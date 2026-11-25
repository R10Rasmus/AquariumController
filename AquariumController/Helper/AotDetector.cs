using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AquariumController.Helper
{
    /// <summary>
    /// Detects a sudden temperature change, which happens when the AOT system adds new water.
    /// A change is saved as an event in the db, so it can be shown on the graphs.
    /// </summary>
    public class AotDetector
    {
        public const string EventType = "AOT";

        private const double DefaultTempDelta = 0.2;
        private const int DefaultWindowMinutes = 2;

        // Ignore changes if a cooler was turned on/off within this time, since the cooler changes the temperature
        private static readonly TimeSpan CoolerIgnoreTime = TimeSpan.FromMinutes(5);

        // Only one event per water fill
        private static readonly TimeSpan EventCooldown = TimeSpan.FromMinutes(15);

        private static readonly TimeSpan ReadSettingsInterval = TimeSpan.FromMinutes(5);

        private readonly Queue<KeyValuePair<DateTime, double>> _readings = new Queue<KeyValuePair<DateTime, double>>();

        private double _tempDelta = DefaultTempDelta;
        private TimeSpan _window = TimeSpan.FromMinutes(DefaultWindowMinutes);
        private DateTime _lastEventUtc = DateTime.MinValue;
        private DateTime _lastSettingsReadUtc = DateTime.MinValue;

        public AotDetector(MySqlConnection conn)
        {
            ReadSettings(conn);
        }

        public void AddReading(MySqlConnection conn, double temperature)
        {
            if (temperature <= 0)
                return;

            DateTime now = DateTime.UtcNow;

            if (now - _lastSettingsReadUtc > ReadSettingsInterval)
                ReadSettings(conn);

            _readings.Enqueue(new KeyValuePair<DateTime, double>(now, temperature));
            while (_readings.Count > 0 && now - _readings.Peek().Key > _window)
                _readings.Dequeue();

            if (now - _lastEventUtc < EventCooldown)
                return;

            double min = _readings.Min(r => r.Value);
            double max = _readings.Max(r => r.Value);

            double change = 0;
            if (temperature - min >= _tempDelta)
                change = temperature - min;
            else if (max - temperature >= _tempDelta)
                change = temperature - max;

            if (change == 0)
                return;

            if (now - Cooler.LastStateChangeUtc < CoolerIgnoreTime)
                return;

            _lastEventUtc = now;
            _readings.Clear();

            ConsoleEx.WriteLineWithDate($"Sudden temperature change of {change:0.00} within {_window.TotalMinutes} min, saving AOT event");

            try
            {
                Helpers.DB.Helper.SaveEvent(conn, EventType, Math.Round(change, 2));
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLineWithDate($"Could not save AOT event: {ex.Message}");
            }
        }

        private void ReadSettings(MySqlConnection conn)
        {
            _lastSettingsReadUtc = DateTime.UtcNow;
            try
            {
                string delta = Helpers.DB.Helper.TryGetSettingFromDb(conn, "AotTempDelta");
                if (double.TryParse(delta?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d > 0)
                    _tempDelta = d;

                string window = Helpers.DB.Helper.TryGetSettingFromDb(conn, "AotWindowMinutes");
                if (int.TryParse(window, out int w) && w > 0)
                    _window = TimeSpan.FromMinutes(w);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLineWithDate($"Could not read AOT settings, using defaults: {ex.Message}");
            }
        }
    }
}
