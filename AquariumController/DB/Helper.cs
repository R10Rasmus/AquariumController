using MySql.Data.MySqlClient;

namespace AquariumController.DB
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

            var rdr = cmd.ExecuteReader();
            rdr.Read();

            string value = rdr["value"].ToString();

            rdr.Close();

            return value;
        }

        public static void SaveChannelValue(MySqlConnection conn, string channelName, double value)
        {

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "INSERT INTO " + channelName + "(value) VALUES(" + value + "); ",
                Connection = conn
            };

            var rdr = cmd.ExecuteNonQuery();



        }

    }
}
