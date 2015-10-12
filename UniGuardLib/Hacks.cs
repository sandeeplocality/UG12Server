using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jwm.Device.Protocol;

namespace UniGuardLib
{
    public static class Hacks
    {
        /// <summary>
        /// Set of rules for adjusting data -- these are only hacks, and ought to be removed later...
        /// </summary>
        public static RecordStruct AdjustGPRSTime(RecordStruct record, string databaseName)
        {
            // Quokka security requires their data to be moved backward by 3 hours...
            if (databaseName == "RvQ9b3INZzATdwq0JtrH")
            {
                record.ReadTime = record.ReadTime.AddHours(-3);
            }

            return record;
        }

        /// <summary>
        /// Set rules for adjusting database name
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static string AdjustDatabase(string databaseName)
        {
            switch (databaseName)
            {
                // SNP
                case "kalhr6f2kphxvfgvtnbp":
                case "CDwr3QX4WbdMKEZf69RY":
                    return "1spYDP3fMZckqaGwHj8Q";

                // Challenger Knight
                case "7H84zI9a2CnfBqxDN0wK":
                    return "JjthQOD7n4Ycab51FdHG";

                // Glad Group
                case "nGByXj9Lw0pZHzTQbasY":
                case "YN5hvKc4VZr2QBwP8Opd":
                case "fh74jG5Yu5gE3iGy80dT":
                case "d0cin5h31topmyxpzwrv":
                    return "D8fb0RraxLAFkCM1PNO3";

                // Sydney Convention Centre
                case "tvSbehrFJeW7hA3w2gwa":
                    return "yJZH7NaK1C3xUw5Yjf6X";

                // Country Security Solutions
                case "NmrASKLH6f2kPhXvFgVt":
                case "x4YVM5hnrWC2qGQtsaTD":
                case "E7UA1yPzwGg4k0nr85Kh":
                case "5V4p6Tz8HyBJLvUrMNKP":
                    return "46TMmsvaA7kQRYLbSHEt";

                default:
                    return databaseName;
            }
        }

    }
}
