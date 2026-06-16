using AquariumController.Extension;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AquariumController.Helper
{
    public class Pumper
    {
        //do not hammer the Hue bridge, only poll the state every few seconds
        private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
        private DateTime _lastPoll = DateTime.MinValue;

        //warn by SMS if the pump stays off for longer than this (no circulation is critical)
        private static readonly TimeSpan _offWarningThreshold = TimeSpan.FromMinutes(6);
        private DateTime? _offSince = null;
        private bool _offWarningSent = false;

        private readonly ILocalHueClient client;
        private readonly string pumperId;
        private bool? _lastState = null;

        public Pumper(MySqlConnection conn)
        {
            ConsoleEx.WriteLineWithDate("Setting up Pumper (Hue smart plug)...");

            //Pumper is non-critical: never let a missing setting/table or Hue error take down
            //the whole controller (temperature, cooling, SMS alarms). On failure, just disable it.
            try
            {
                client = new LocalHueClient(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
                client.Initialize(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

                string pumperName = Helpers.DB.Helper.GetSettingFromDb(conn, "PumperName");

                IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();
                Light pumper = lights.FirstOrDefault(t => t.Name == pumperName);

                if (pumper != null)
                {
                    pumperId = pumper.Id;
                    _lastState = pumper.State.On;
                    ConsoleEx.WriteLineWithDate($"Pumper '{pumperName}' found (id:{pumperId}), state is {(_lastState.Value ? "on" : "off")}");
                }
                else
                {
                    ConsoleEx.WriteLineWithDate($"Pumper '{pumperName}' was not found in Hue. (Set the 'PumperName' setting and restart.)");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                ConsoleEx.WriteLineWithDate($"Pumper setup failed, monitoring disabled: {ex.Message} (Did you run db/pumper_setup.sql to add the 'PumperName' setting and the pumper_state_changes table?)");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        //Poll the smart plug. When it changes on/off state, log the change (from -> to) to the db.
        public void CheckPumperState(MySqlConnection conn)
        {
            if (pumperId == null || _lastPoll.Add(_pollInterval) > DateTime.Now)
            {
                return;
            }

            _lastPoll = DateTime.Now;

            Light pumper = client.GetLightAsync(pumperId).GetAwaiter().GetResult();
            if (pumper == null)
            {
                return;
            }

            bool currentState = pumper.State.On;

            if (_lastState == null)
            {
                _lastState = currentState;
            }
            else if (_lastState.Value != currentState)
            {
                Helpers.DB.Helper.SavePumperStateChange(conn, _lastState.Value, currentState);

                ConsoleEx.WriteLineWithDate($"Pumper changed from {(_lastState.Value ? "on" : "off")} to {(currentState ? "on" : "off")}");

                _lastState = currentState;
            }

            CheckOffDurationWarning(conn, currentState);
        }

        //Send a single SMS warning if the pump stays off longer than the threshold. Reset when it turns back on.
        private void CheckOffDurationWarning(MySqlConnection conn, bool currentState)
        {
            if (currentState)
            {
                _offSince = null;
                _offWarningSent = false;
                return;
            }

            if (_offSince == null)
            {
                _offSince = DateTime.Now;
            }

            if (!_offWarningSent && DateTime.Now - _offSince.Value > _offWarningThreshold)
            {
                _offWarningSent = true;

                int minutes = (int)Math.Round((DateTime.Now - _offSince.Value).TotalMinutes);
                ConsoleEx.WriteLineWithDate($"Pumper has been off for {minutes} min, sending SMS warning!");

                //fire-and-forget like the temperature alarms; alarm=true bypasses the shared 2h SMS
                //cooldown so the warning is actually delivered. Only called once per off-episode.
                SendSMS.SendSMSAsync(0, conn, $"WARNING: Pumper has been off for more than {minutes} min!", true);
            }
        }
    }
}
