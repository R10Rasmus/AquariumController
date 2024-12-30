using AquariumController.Extension;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AquariumController.Helper
{
    public static class SendSMS
    {
        private static readonly HttpClient client = new HttpClient();
        private static DateTime _lastSent = DateTime.MinValue;
        private static bool sendAlarm = false;
        private static readonly TimeSpan _cooldown = TimeSpan.FromHours(2);
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static async Task SendSMSAsync(double tempertur, string SMSapiToken, string PhonNumber, string message, bool alarm=false)
        {
            // Asynchronous locking to ensure thread safety
            await _semaphore.WaitAsync();
            try
            {
                if (alarm && !sendAlarm)
                {
                    sendAlarm = true;
                    _lastSent = DateTime.MinValue;
                }

                var timeSinceLastSend = DateTime.UtcNow - _lastSent;
                if (timeSinceLastSend < _cooldown)
                {
                    return;
                }

                // Start cooldown
                _lastSent = DateTime.UtcNow;
                sendAlarm = false;
            }
            finally
            {
                _semaphore.Release();
            }

            // Set up the authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Token",
                SMSapiToken
            );

            string formatted = FormatPhoneNumberRegex(PhonNumber);

            ConsoleEx.WriteLineWithDate($"Sending SMS to {formatted}...");

            var messages = new
            {
                sender = "Akv. ALARM",
                 message,
                recipients = new[] { new { msisdn = formatted } }, // Ensure msisdn is correctly formatted 0045_1234_5678
            };

            try
            {
                // Serialize the messages object to JSON
                var request = new StringContent(JsonConvert.SerializeObject(messages), Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(
                    "https://gatewayapi.com/rest/mtsms",
                    request
                );

                if (resp.IsSuccessStatusCode && resp.Content != null)
                {
                    ConsoleEx.WriteLineWithDate("SMS Sent Successfully.");
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    if (responseContent != null)
                    {
                        ConsoleEx.WriteLineWithDate($"SMS ID: {responseContent}");
                    }
                }
                else if (resp.Content != null)
                {
                    ConsoleEx.WriteLineWithDate("Failed to send SMS. Response content:");
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    ConsoleEx.WriteLineWithDate(responseBody);

                    // Reset the cooldown to allow retrying after a failure
                    await _semaphore.WaitAsync();
                    try
                    {
                        _lastSent = DateTime.MinValue;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLineWithDate($"Exception occurred while sending SMS: {ex.Message}");

                // Reset the cooldown to allow retrying after an exception
                await _semaphore.WaitAsync();
                try
                {
                    _lastSent = DateTime.MinValue;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Formats an 8-digit phone number using regular expressions.
        /// Example: "31419498" -> "0045_3141_9498"
        /// </summary>
        /// <param name="input">The original 8-digit phone number as a string.</param>
        /// <returns>The formatted phone number.</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null, empty, not 8 digits, or contains non-digit characters.</exception>
        static string FormatPhoneNumberRegex(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty.");

            // Regular expression to match exactly 8 digits
            Regex regex = new Regex(@"^(\d{4})(\d{4})$");
            Match match = regex.Match(input);

            if (!match.Success)
                throw new ArgumentException("Input must be exactly 8 digits.");

            string firstPart = match.Groups[1].Value;
            string secondPart = match.Groups[2].Value;

            return $"0045_{firstPart}_{secondPart}";
        }
    }
}
