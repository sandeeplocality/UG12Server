using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MySql.Data.MySqlClient;
using System.Text;
using UniGuardLib;

namespace SiteWelfareChecking
{
    public class SiteWelfareData
    {
        private const string LOGOPATH = @"C:\HostingSpaces\Portal\logo\";

        // Set class variables
        private List<string> emailList = new List<string>();
        private Data data;
        private List<int[]> welfareIds;
        private string databaseName;
        private string database;
        private string lastCheck;
        private string logoPath;
        private int dayOfWeek;
        private int welfareId;
        private int branchId;
        private int siteId;
        private int recorderSerial;
        private double timeAllowance;
        private double timeOffset;
        private double gmtOffset;
        private DateTime start;

        public SiteWelfareData(string databaseName)
        {
            // Initialize variables
            this.databaseName = databaseName;
            this.database     = "ug12db_" + this.databaseName;

            // Access data
            data       = new Data(this.databaseName);
            welfareIds = data.GetAllWelfareIds();
        }

        public void Run()
        {
            foreach (var ids in welfareIds)
            {
                this.welfareId = ids[0];
                this.siteId    = ids[1];
                this.branchId  = ids[2];
                this.gmtOffset = data.GetZoneOffset(ids[3]);

                ProcessSiteWelfare();
            }
        }

        private void ProcessSiteWelfare()
        {
            // Get current day of the week
            this.dayOfWeek = (Int32)Utility.Now(this.gmtOffset).DayOfWeek;

            // If welfare is active and meets all criteria, continue, otherwise just do nothing
            if (this.IsActive())
            {
                try
                {
                    this.GetWelfareCheck();
                }
                catch (Exception ex)
                {
                    Log.Error("Site welfare error:\r\n" + ex.ToString());
                }

                double secondsSinceLastHit   = this.GetSecondsSinceLastHit();
                double secondsSinceLastCheck = this.GetSecondsSinceLastCheck();
                double secondsSinceStart     = this.GetSecondsSinceStart();

                // Check that the seconds since the start of the shift are greater than the time allowance
                if ((secondsSinceStart > timeAllowance) &&
                    
                    // And the number of seconds since the last hit is also greater than the time allowance
                    (secondsSinceLastHit > timeAllowance) &&
                    
                    // And the number of seconds since the last check are greater than the time offset
                    (secondsSinceLastCheck > timeOffset))
                {

                    // Get list of emails
                    this.GetEmailList();

                    if (this.emailList.Count > 0)
                    {
                        try
                        {
                            Mailer mail      = new Mailer();
                            mail.Recipients = this.emailList;
                            mail.Visit      = this.GetLastWelfareVisit();
                            mail.sendMail();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Mailer setup error:\r\n" + ex.ToString());
                        }
                    }

                    try
                    {
                        this.CreateException();
                        this.UpdateLastCheck();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Database update error:\r\n" + ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Gets details for last welfare visit
        /// </summary>
        /// <returns>Object array, single row</returns>
        private object[] GetLastWelfareVisit()
        {
            object[] result = new object[7];
            Database db = new Database(this.database);

            // Build query
            string query = String.Format(@"
                SELECT
                    patrol.patDate,
                    patrol.patTime,
                    patrol.patRSN,
                    checkpoint.chpDescription,
                    site.sitName,
                    region.regName,
                    IF (recorder.recName IS NULL, patrol.patRSN, recorder.recName) AS 'recName'
                FROM patrol
                    LEFT JOIN tag ON patrol.patTSN = tag.tagTSN
                    LEFT JOIN recorder ON patrol.patRSN = recorder.recRSN
                    LEFT JOIN checkpoint ON checkpoint.tag_id = tag.id
                    LEFT JOIN site ON checkpoint.site_id = site.id
                    LEFT JOIN region ON site.region_id = region.id
                WHERE site.id = {0}
                ORDER BY patrol.patDate DESC, patrol.patTime DESC LIMIT 1
            ", this.siteId);

            // Get data
            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    result[0] = reader.GetDateTime(0).ToString("dd/MM/yyyy");
                    result[1] = reader.GetValue(1).ToString();
                    result[2] = Convert.ToInt32(reader.GetValue(2));
                    result[3] = reader.GetString(3) == String.Empty ? "Unkonwn Checkpoint" : reader.GetString(3);
                    result[4] = reader.GetString(4);
                    result[5] = reader.GetString(5);
                    result[6] = reader.GetString(6);

                    // Let's add the recorder serial here for convenience too
                    this.recorderSerial = Convert.ToInt32(reader.GetValue(2));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates an exception and inserts it to the database.
        /// </summary>
        private void CreateException()
        {
            // Create insert query
            InsertQuery insert = new InsertQuery();
            insert.SetTable("welfarecheckexception");
            insert.SetFields(new string[4] { "welfarecheck_id", "excDate", "excTime", "excRSN" });
            insert.AddRowValues(new string[4] {
                this.welfareId.ToString(),
                Utility.Now(this.gmtOffset).ToString("yyyy-MM-dd"),
                Utility.Now(this.gmtOffset).ToString("HH:mm:ss"),
                this.recorderSerial.ToString()
            });

            // Database
            Database db = new Database(this.database);
            db.Insert(insert);
        }

        private List<string[]> GetVisitHistory()
        {
            List<string[]> list = new List<string[]>();
            Database db = new Database(this.database);

            // Get DateTime and substract 24 hours
            string startTime = Utility.Now(this.gmtOffset).AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");

            string query = String.Format(@"
                SELECT
                    patrol.patDate,
                    patrol.patTime,
                    patrol.patRSN,
                    checkpoint.chpDescription,
                    site.sitName,
                    region.regName,
                    IF (recorder.recName IS NULL, patrol.patRSN, recorder.recName) AS 'recName'
                FROM patrol
                    LEFT JOIN tag ON patrol.patTSN = tag.tagTSN
                    LEFT JOIN recorder ON patrol.patRSN = recorder.recRSN
                    LEFT JOIN checkpoint ON checkpoint.tag_id = tag.id
                    LEFT JOIN site ON checkpoint.site_id = site.id
                    LEFT JOIN region ON site.region_id = region.id
                WHERE patrol.patRSN = {0}
                    AND TIMESTAMP(patrol.patDate, patrol.patTime) > '{1}'
                ORDER BY patrol.patDate ASC, patrol.patTime ASC LIMIT 10
            ", this.recorderSerial, startTime);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    list.Add(new string[7] {
                        reader.GetDateTime(0).ToString("dd/MM/yyyy"),
                        reader.GetValue(1).ToString(),
                        reader.GetString(2),
                        reader.GetString(3) == String.Empty ? "Unkonwn Checkpoint" : reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetString(6)
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// Assigns all details about welfare check to the variables
        /// </summary>
        private void GetWelfareCheck()
        {
            Database db = new Database(this.database);
            string query = String.Format(
                    "SELECT welTimeAllowance, welCheckOffset, welLastCheck FROM welfarecheck WHERE id = {0}", this.welfareId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    this.timeAllowance = Convert.ToDouble(reader.GetValue(0));
                    this.timeOffset    = Convert.ToDouble(reader.GetValue(1));
                    this.lastCheck     = reader.GetValue(2).ToString();
                }
            }
        }

        /// <summary>
        /// Gets the email list to contact in case of an exception
        /// </summary>
        private void GetEmailList()
        {
            Database db = new Database(this.database);
            string query = String.Format(
                    "SELECT weeEmail FROM welfarecheckemail WHERE welfarecheck_id = {0}", this.welfareId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    this.emailList.Add(reader.GetString(0));
                }
            }
        }

        /// <summary>
        /// Returns the time (in minutes) since the last registered hit in the site
        /// </summary>
        /// <returns>Number of minutes since last hit on site</returns>
        private double GetSecondsSinceLastHit()
        {
            Database db = new Database(this.database);
            double result = 0;
            string query = String.Format(@"
                SELECT  TIMESTAMP(patrol.patDate, patrol.patTime) as 'timestamp'
                FROM    patrol
                        LEFT JOIN tag ON patrol.patTSN = tag.tagTSN
                        LEFT JOIN checkpoint ON checkpoint.tag_id = tag.id
                        LEFT JOIN site ON checkpoint.site_id = site.id
                WHERE   site.id = {0}
                ORDER   BY patrol.patDate DESC, patrol.patTime DESC LIMIT 1
            ", this.siteId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    DateTime last = reader.GetDateTime(0);
                    DateTime now  = Utility.Now(this.gmtOffset);
                    TimeSpan ts = now - last;
                    result = Math.Floor(ts.TotalSeconds);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets total seconds since last welfare check
        /// </summary>
        /// <returns>Returns seconds since last welfare check</returns>
        private double GetSecondsSinceLastCheck()
        {
            Database db = new Database(this.database);
            double result = 0;
            string query = String.Format(
                "SELECT welLastCheck FROM welfarecheck WHERE id = {0}", this.welfareId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    DateTime last = reader.GetDateTime(0);
                    DateTime now  = Utility.Now();
                    TimeSpan ts = now - last;
                    result = Math.Floor(ts.TotalSeconds);
                }
            }

            return result;
        }

        /// <summary>
        /// Get total seconds since start of welfare check period
        /// </summary>
        /// <returns></returns>
        private double GetSecondsSinceStart()
        {
            return Math.Floor( (Utility.Now(this.gmtOffset) - this.start).TotalSeconds );
        }

        /// <summary>
        /// Checks if the welfare check falls within the parameters required to check it or not
        /// </summary>
        /// <returns></returns>
        private bool IsActive()
        {
            // We will store all the start and end times in a dates variable as a list
            List<DateTime[]> dateList = new List<DateTime[]>();

            // Connect to database
            Database db = new Database(this.database);

            // Build query
            string query = String.Format(@"
                SELECT  @diff:=( CAST(welfarecheckday.day_id AS SIGNED) - (WEEKDAY(CURRENT_DATE) + 1) ) as 'diff',
                        @date:=DATE_ADD(CURRENT_DATE, INTERVAL @diff DAY) as 'start_date',
                        CONVERT(TIMESTAMP(@date, welfarecheckday.wedStart) USING latin1) AS 'start',
                        CONVERT(TIMESTAMP(IF (welfarecheckday.wedFinish <= welfarecheckday.wedStart,
                                              DATE_ADD(@date, INTERVAL 1 DAY),
                                              @date),
                                          welfarecheckday.wedFinish) USING latin1) AS 'end',
                        welfarecheck.welAlwaysCheck,
                        welfarecheckday.day_id
                FROM    welfarecheckday
                        LEFT JOIN welfarecheck ON welfarecheckday.welfarecheck_id = welfarecheck.id
                        LEFT JOIN site ON welfarecheck.site_id = site.id
                WHERE   welfarecheck.id = {0}
                        AND welfarecheckday.wedActive = 1
                        AND welfarecheckday.day_id < 8
                        AND site.sitWelfareActive = 1
            ", this.welfareId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    // Set to true if 'always check' is selected and matches the current day of the week
                    if (Convert.ToInt32(reader.GetValue(4)) == 1 && Convert.ToInt32(reader.GetValue(5)) == this.dayOfWeek)
                        return true;

                    // Assign TIMESTAMP data to DateTime array List
                    dateList.Add(new DateTime[2] { Convert.ToDateTime(reader.GetString(2)), Convert.ToDateTime(reader.GetString(3)) });
                }
            }

            // Establish current time
            DateTime target = Utility.Now(this.gmtOffset);

            // Iterate over dates stored
            foreach (var dates in dateList)
            {
                // If right now lands in between the start and end time of any of the following, return true
                if (target > dates[0] && target < dates[1])
                {
                    // Assign the start date/time
                    this.start = dates[0];

                    // Return true
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the last check row in the table
        /// </summary>
        private void UpdateLastCheck()
        {
            Database db = new Database(this.database);

            UpdateQuery update = new UpdateQuery();
            update.SetTable("welfarecheck");
            update.SetFields(new string[] { "welLastCheck" });
            update.AddRowValue(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            update.SetId(this.welfareId.ToString());

            db.Update(update);
        }

        /// <summary>
        /// Checks required files for logo in reports and if not present, creates them
        /// </summary>
        private void CheckFileRequirements()
        {
            // First ensure that the logo directory exists
            DirectoryInfo dir = new DirectoryInfo(LOGOPATH);
            if (!dir.Exists) dir.Create();

            // Now ensure the default logo exists, otherwise put it there
            this.logoPath = LOGOPATH + "uniguard_oms.jpg";
            FileInfo logo = new FileInfo(this.logoPath);

            if (!logo.Exists)
            {
                try
                {
                    Bitmap lg = new Bitmap(Properties.Resources.uniguard_oms);
                    lg.Save(this.logoPath);
                    lg.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error("Image saving failed:\r\n" + ex.ToString());
                }
            }

        }
    }
}
