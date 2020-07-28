using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquariumController.Helper
{
    public static class Ph
    {
        public static double PH = 0;
        public static void SavePh(Object stateInfo)
        {
            if (PH > 0)
            {
                Console.WriteLine($"Save ph with value {PH}");

                var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                DB.Helper.SaveChannelValue(localConn, "ph", PH);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                Console.WriteLine($"Do not save pH if it is 0");
            }

        }

    }
}
