using AquariumController.Extension;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Q42.HueApi;
using Q42.HueApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace AquariumController.Helper
{
    public static class Reboot
    {
        public async static void RebootCommand(MySqlConnection conn)
        {
            var client = new LocalHueClient(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            var HueKey = Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey");
            
            client.Initialize(HueKey);
            var schedules = await client.GetSchedulesAsync();

            //Clean up
            foreach (var schedule in schedules)
            {
                ConsoleEx.WriteLineWithDate($"Deleting schedule {schedule.Name}...");
                await client.DeleteScheduleAsync(schedule.Id);
            }
            var PiLightName = Helpers.DB.Helper.GetSettingFromDb(conn, "PiLightName");

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            var PiLight = lights.FirstOrDefault(s => s.Name == PiLightName);

            if (PiLight == null)
            {
                ConsoleEx.WriteLineWithDate($"PiLight by name {PiLightName} NOT found!");
            }
            else
            {
                ConsoleEx.WriteLineWithDate($"Found PiLight by name {PiLightName}");
            }


            //Create new schedule
            Schedule newSchedule = new Schedule
            {
                Name = "Pi restart",
                Description = "Schedule to start pi",
                LocalTime = new HueDateTime()
                {
                    TimerTime = new TimeSpan(0,1,0)
                },
                AutoDelete = true,
                Command = new InternalBridgeCommand()
            };

            var commandBody = new LightCommand
            {
                On = true
            };
            newSchedule.Command.Body = commandBody;
            newSchedule.Command.Address = $"/api/{HueKey}/lights/{PiLight.Id}/state";
            newSchedule.Command.Method = HttpMethod.Put;

            var result = await client.CreateScheduleAsync(newSchedule);

            ConsoleEx.WriteLineWithDate($"Result of creating schedule: {result}");

            LightCommand lightCommand = new LightCommand() { On = false };
            await client.SendCommandAsync(lightCommand, new List<string> { PiLight.Id });

        }




    }
}
