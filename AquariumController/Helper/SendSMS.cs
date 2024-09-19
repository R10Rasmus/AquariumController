using AquariumController.Extension;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AquariumController.Helper
{
    public static class SendSMS
    {
        private static readonly HttpClient client = new HttpClient();
        private static DateTime _lastSent = DateTime.MinValue;
        private static readonly TimeSpan _cooldown = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static async Task SendSMSAsync(double tempertur)
        {
            // Asynchronous locking to ensure thread safety
            await _semaphore.WaitAsync();
            try
            {
                var timeSinceLastSend = DateTime.UtcNow - _lastSent;
                if (timeSinceLastSend < _cooldown)
                {
                    var remainingTime = _cooldown - timeSinceLastSend;
                    ConsoleEx.WriteLineWithDate($"Cannot send SMS yet. Please wait {remainingTime.Minutes} minutes and {remainingTime.Seconds} seconds.");
                    return;
                }

                // Update the last sent time to start the cooldown
                _lastSent = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }

            // Retrieve configuration settings
            var apiToken = "7jalEAQFSM-YAtAte6eMDFMPQPZkpAYbW0H-E-1Bez7UEhK_fDUJccup_pwZ14bj";

            // Set up the authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Token",
                apiToken
            );

            var messages = new
            {
                sender = "Akvarium alarm",
                message = $"ALARM Tempertur {tempertur}",
                recipients = new[] { new { msisdn = 45_3141_9498 } }, // Ensure msisdn is correctly formatted
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
    }
}
