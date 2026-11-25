using MySql.Data.MySqlClient;

namespace Helpers.DB
{
    public class Helper
    {

        public static string GetSettingFromDb(MySqlConnection conn, string settingName)
        {
            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT title, value FROM settings WHERE(title = '" + settingName + "')",
                Connection = conn
            };

            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();

            string value = rdr["value"].ToString();

            rdr.Close();

            return value;
        }

        /// <summary>
        /// Same as GetSettingFromDb, but returns null if the setting does not exist.
        /// </summary>
        public static string TryGetSettingFromDb(MySqlConnection conn, string settingName)
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT value FROM settings WHERE title = @title", conn))
            {
                cmd.Parameters.AddWithValue("@title", settingName);
                object value = cmd.ExecuteScalar();
                return value == null || value == System.DBNull.Value ? null : value.ToString();
            }
        }

        public static void SaveEvent(MySqlConnection conn, string type, double value)
        {
            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO events (type, value) VALUES (@type, @value)", conn))
            {
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@value", value);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SaveChannelValue(MySqlConnection conn, string channelName, double value)
        {

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "INSERT INTO " + channelName + "(value) VALUES(" + value + "); ",
                Connection = conn
            };

            int rdr = cmd.ExecuteNonQuery();

        }

        public static void SaveFail(MySqlConnection conn, string message, string stackTrace)
        {

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "INSERT INTO fails (message,stacktrace) VALUES('" + message + "','" + stackTrace + "' ); ",
                Connection = conn
            };

            int rdr = cmd.ExecuteNonQuery();

        }

        public static void SaveSettingValue(MySqlConnection conn, string settingName, string value)
        {

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "update settings set value ='" + value + "' where title ='" + settingName + "';",
                Connection = conn
            };

            int rdr = cmd.ExecuteNonQuery();
        }

        public static string GetLastTemperturSavedTime(MySqlConnection conn)
        {

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT * FROM temperature ORDER BY id desc LIMIT 1",
                Connection = conn
            };

            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();

            string value = rdr["created_at"].ToString();

            rdr.Close();

            return value;
        }

    }
}
