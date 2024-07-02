using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquariumController.Helper
{
    public static class Fails
    {
        public static void SaveFail(Exception ex)
        {
            ConsoleEx.WriteLineWithDate($"Save Error: {ex.Message}");

            string allMessages = GetAllExceptionMessages(ex);
            string allStackTraces = GetAllExceptionStackTraces(ex);

            MySqlConnection localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            DB.Helper.SaveFail(localConn, allMessages, allStackTraces);

            localConn.Close();
            localConn.Dispose();

        }

        private static string GetAllExceptionMessages(Exception ex)
        {
            if (ex == null) return string.Empty;

            StringBuilder sb = new StringBuilder();
            CollectExceptionDetails(ex, sb, collectStackTrace: false);

            return sb.ToString().TrimEnd('#');
        }

        private static string GetAllExceptionStackTraces(Exception ex)
        {
            if (ex == null) return string.Empty;

            StringBuilder sb = new StringBuilder();
            CollectExceptionDetails(ex, sb, collectStackTrace: true);

            return sb.ToString().TrimEnd('#');
        }

        private static void CollectExceptionDetails(Exception ex, StringBuilder sb, bool collectStackTrace)
        {
            if (ex == null) return;

            if (collectStackTrace)
            {
                sb.Append(ex.StackTrace);
            }
            else
            {
                sb.Append(ex.Message);
            }

            sb.Append("#");

            if (ex.InnerException != null)
            {
                CollectExceptionDetails(ex.InnerException, sb, collectStackTrace);
            }
        }
    }
}
