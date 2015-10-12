using System;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace UniGuardLib
{
    public class Data
    {
        // Params
        private string databaseName;
        private string newDatabase;

        public Data(string database)
        {
            // Set params
            this.databaseName = database;
            this.newDatabase  = "ug12db_" + database;
        }

        /*********************************************************
         * GET STATEMENTS 
         *********************************************************/

        /// <summary>
        /// Returns the account Id for the database passed through
        /// </summary>
        /// <returns>Returns an integer value for the id column of the account.</returns>
        public int GetAccountIdForDatabase()
        {
            int output = 0;

            Database db = new Database();
            output = db.GetAccountFromDatabase(this.databaseName);

            return output;
        }

        /// <summary>
        /// Gets the zone id for current database
        /// </summary>
        /// <returns>Returns zone.id</returns>
        public int GetZoneId(int branchId = 0)
        {
            int output            = 0;
            string query          = branchId != 0 ?
                String.Format("SELECT zone_id FROM branch WHERE id = {0}", branchId) :
                String.Format("SELECT zone_id FROM branch ORDER BY id ASC LIMIT 1;");

            Database db = new Database(this.newDatabase);
            using (var reader = db.Query(query))
            while (reader.Read())
            {
                output = Convert.ToInt32(reader.GetValue(0));
            }

            return output;
        }

        /// <summary>
        /// Gets the branch id for a particular recorder
        /// </summary>
        /// <param name="recorderSerial">Gets the recorder serial</param>
        /// <returns>Branch Id for a recorder</returns>
        private int GetBranchIdForRecorderSerial(string recorderSerial)
        {
            int output = 1;

            Database db = new Database(this.newDatabase);
            string query = String.Format("SELECT branch_id FROM recorder WHERE recRSN = {0}", recorderSerial);

            // Initialize reader
            using (var reader = db.Query(query))
            while (reader.Read())
            {
                output = Convert.ToInt32(reader.GetValue(0));
            }

            return output;
        }

        /// <summary>
        /// Gets all the id numbers for all welfare checks in database
        /// </summary>
        /// <returns>Returns an array of all welfare check Ids</returns>
        public List<int[]> GetAllWelfareIds()
        {
            List<int[]> list = new List<int[]>();
            Database db = new Database(this.newDatabase);

            string query = @"SELECT welfarecheck.id,
                                    welfarecheck.site_id,
                                    region.branch_id,
                                    branch.zone_id
                            FROM    welfarecheck
                                    LEFT JOIN site ON welfarecheck.site_id = site.id
                                    LEFT JOIN region ON site.region_id = region.id
                                    LEFT JOIN branch ON region.branch_id = branch.id
                            WHERE   site.sitWelfareActive = 1";

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                int[] array = new int[4] {
                    Convert.ToInt32(reader.GetValue(0)),
                    Convert.ToInt32(reader.GetValue(1)),
                    Convert.ToInt32(reader.GetValue(2)),
                    Convert.ToInt32(reader.GetValue(3))
                };

                list.Add(array);
            }

            return list;
        }

        /// <summary>
        /// Gets all the id numbers for all site loops in database
        /// </summary>
        /// <returns>Returns an array of all welfare check Ids</returns>
        public List<int[]> GetAllSiteLoopsIds()
        {
            List<int[]> list = new List<int[]>();
            Database db = new Database(this.newDatabase);

            string query = @"SELECT loop.id,
                                    loop.site_id,
                                    region.branch_id,
                                    branch.zone_id
                            FROM    `loop`
                                    LEFT JOIN site ON loop.site_id = site.id
                                    LEFT JOIN region ON site.region_id = region.id
                                    LEFT JOIN branch ON region.branch_id = branch.id
                            WHERE   loop.looActive = 1";

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                int[] array = new int[4] {
                    Convert.ToInt32(reader.GetValue(0)),
                    Convert.ToInt32(reader.GetValue(1)),
                    Convert.ToInt32(reader.GetValue(2)),
                    Convert.ToInt32(reader.GetValue(3))
                };

                list.Add(array);
            }

            return list;
        }

        /// <summary>
        /// Returns the Id number of the scheduled task to be run
        /// </summary>
        /// <returns>Returns an array of integer with the respective task ids</returns>
        public int[] GetPendingScheduledTasks()
        {
            List<int> list = new List<int>();
            Database db = new Database(this.newDatabase);

            string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string query = String.Format("SELECT id FROM scheduledtask WHERE sctNextRun < '{0}' AND sctActive = '1'", datetime);

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                list.Add(Convert.ToInt32(reader.GetValue(0)));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Returns a string array with all the values from the scheduled task
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public string[] GetScheduledTask(int taskId)
        {
            List<string> list = new List<string>();
            Database db = new Database(this.newDatabase);

            string query = String.Format(@"SELECT timecycle_id, content_id, sctNextRun, sctDescription, sctEmailSubject
                    FROM scheduledtask WHERE scheduledtask.id = {0}", taskId);

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                list.Add(reader.GetValue(0).ToString()); // timecycle_id
                list.Add(reader.GetValue(1).ToString()); // content_id
                list.Add(reader.GetValue(2).ToString()); // sctNextRun
                list.Add(reader.GetString(3));           // sctDescription
                list.Add(reader.GetString(4));           // sctEmailSubject
            }
            

            return list.ToArray();
        }

        public string GetScheduledTaskName(int taskId)
        {
            string output = null;
            Database db   = new Database(this.newDatabase);
            string query  = String.Format("SELECT sctName FROM scheduledtask WHERE id = {0}", taskId);

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                output = reader.GetValue(0).ToString();
            }


            return output;
        }

        /// <summary>
        /// Gets the report name by the content Id
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public string GetReportNameByContentId(int contentId)
        {
            string result = null;
            Database db   = new Database("ug12_maindb");
            string query  = String.Format(@"SELECT conTitle FROM content WHERE id = {0}", contentId.ToString());

            using (var reader = db.Query(query))
            while (reader.Read())
            {
                result = reader.GetString(0);
            }

            return result;
        }

        /// <summary>
        /// Returns list of emails for the selected task
        /// </summary>
        /// <param name="taskId">Integer task Id</param>
        /// <returns></returns>
        public string[] GetScheduledTaskEmailList(int taskId)
        {
            List<string> list = new List<string>();
            Database db       = new Database(this.newDatabase);
            string query      = String.Format(@"SELECT sceEmail FROM scheduledtaskemail WHERE scheduledtask_id = {0}", taskId);

            using (var reader = db.Query(query))
                while (reader.Read())
                {
                    list.Add(reader.GetString(0));
                }

            return list.ToArray();
        }

        /// <summary>
        /// Gets the main administrator's email
        /// </summary>
        /// <returns></returns>
        public string GetAdministratorEmail()
        {
            string email = null;
            Database db  = new Database(this.newDatabase);
            string query = @"SELECT usrEmail FROM user WHERE userrole_id = 1 AND usrActive = 1 ORDER BY id ASC LIMIT 1";

            using (var reader = db.Query(query))
                while (reader.Read())
                {
                    email = reader.GetString(0);
                }

            return email;
        }

        /// <summary>
        /// Gets all administrators emails
        /// </summary>
        /// <returns></returns>
        public string[] GetAdministratorEmails()
        {
            List<string> emailList = new List<string>();
            Database db = new Database(this.newDatabase);
            string query = "SELECT usrEmail FROM user WHERE userrole_id = 1";

            using (var reader = db.Query(query))
                while (reader.Read())
                {
                    emailList.Add(reader.GetValue(0).ToString());
                }

            return emailList.ToArray();
        }

        /*********************************************************
         * INSERT STATEMENTS 
         *********************************************************/

        /// <summary>
        /// Inserts an uploadactivity record into database
        /// </summary>
        /// <param name="insert">InsertQuery</param>
        public void InsertUploadActivity(InsertQuery insert)
        {
            Database db = new Database();
            db.Insert(insert);
        }

        public bool CheckPatrol(string tsn, string date, string time, string rsn)
        {
            Database db = new Database(this.newDatabase);
            string query = String.Format(
                "SELECT id FROM patrol WHERE patTSN = '{0}' AND patDate = '{1}' AND patTime = '{2}' AND patRSN = '{3}'",
                tsn,
                date,
                time,
                rsn
            );

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inserts an import and handles storage into old databasing if necessary
        /// </summary>
        /// <param name="insert">InserQuery object must be provided as a param</param>
        /// <returns>Returns last insert id</returns>
        public int InsertImport(InsertQuery insert)
        {
            int id = 0;

            // Insert the Import into the database
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
            id = db.GetLastColumn("import", "id");

            return id;
        }

        /// <summary>
        /// Inserts event data into database
        /// </summary>
        /// <param name="insert"></param>
        public void InsertEvent(InsertQuery insert)
        {
            // Insert the Event into the database
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
        }

        /// <summary>
        /// Inserts low voltage data into database
        /// </summary>
        /// <param name="insert"></param>
        public void InsertLowVoltage(InsertQuery insert)
        {
            // Insert the Event into the database
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
        }

        /// <summary>
        /// Specialised method to insert patrol data to the database. Optionally
        /// it will also insert to a secondary database.
        /// </summary>
        /// <param name="insert">InsertQuery previously created</param>
        public void InsertPatrol(InsertQuery insert)
        {
            // Insert the Patrol into the database
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
        }

        /// <summary>
        /// Specialised method to store shock data into the database, optionally
        /// it will store data to the old legacy databases also
        /// </summary>
        /// <param name="insert">InsertQuery previously inserted</param>
        public void InsertShock(InsertQuery insert)
        {
            // Insert the shock data
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
        }

        public void InsertVoltage(InsertQuery insert)
        {
            // Insert the Patrol into the database
            Database db = new Database(this.newDatabase);
            db.Insert(insert);
        }

        /*********************************************************
         * UPDATE STATEMENTS 
         *********************************************************/

        public void UpdateTaskScheduler(UpdateQuery update)
        {
            // Insert the Import into the database
            Database db = new Database(this.newDatabase);
            db.Update(update);
        }

        /*********************************************************
         * DELETE STATEMENTS 
         *********************************************************/

        public void DeleteImport(int id)
        {
            Database db = new Database(this.newDatabase);
            db.Delete("import", id);
        }

        /*********************************************************
         * MISC STATEMENTS 
         *********************************************************/

        /// <summary>
        /// Adjusts date to its current timezone
        /// </summary>
        /// <param name="dateTime">String datetime stamp</param>
        /// <returns>String datetime stamp</returns>
        public string AdjustTimezone(string dateTime, string format = "yyyy-MM-dd HH:mm:ss", string recorderSerial = null)
        {
            DateTime dt             = Convert.ToDateTime(dateTime);
            DateTime now            = DateTime.Now;
            TimeSpan timeDifference = now - dt;
            double offset;

            // If recorderSerial is not null, re-calculate the zone id
            int branchId = recorderSerial != null ? this.GetBranchIdForRecorderSerial(recorderSerial) : 0;
                
            int zoneId = this.GetZoneId(branchId);
            offset     = this.GetZoneOffset(zoneId);

            // Check date >= 7 hrs
            if (timeDifference.TotalHours <= -8)
                dt = dt.AddSeconds(offset * -1);

            // Time is now UTC: Now we turn it back to the appropriate timezone...
            return dt.AddSeconds(offset).ToString(format);
        }

        public double GetZoneOffset(int zoneId = 38)
        {
            double offset = 0;
            string query;

            query = String.Format(
                @"SELECT	timezone.gmt_offset
                    FROM 	timezone
	                        JOIN zone ON timezone.zone_id = zone.id
                    WHERE 	timezone.time_start < UNIX_TIMESTAMP(UTC_TIMESTAMP())
	                        AND zone.id = {0}
                    ORDER BY timezone.time_start DESC LIMIT 1", zoneId);

            Database db = new Database();
            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    offset = Convert.ToInt32(reader.GetValue(0));
                }
            }

            return offset;
        }

    }

    /// <summary>
    /// Retrieves data from the main database
    /// </summary>
    public static class LocalData
    {

        /// <summary>
        /// Returns a list of all databases
        /// </summary>
        /// <returns></returns>
        public static string[] GetAllDatabases()
        {
            List<string> list = new List<string>();
            Database db = new Database();

            try
            {
                using (var reader = db.Query("SELECT schCode FROM schemalocation"))
                    while (reader.Read())
                    {
                        list.Add(reader.GetString(0));
                    }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

            return list.Count > 0 ? list.ToArray() : null;
        }

        /// <summary>
        /// Returns the company name by database name
        /// </summary>
        /// <param name="databaseName">Database name</param>
        /// <returns></returns>
        public static string GetCompanyNameByDatabase(string databaseName)
        {
            string result  = null;
            int databaseId = 0;

            Database db   = new Database();
            string query  = String.Format("SELECT id FROM schemalocation WHERE schCode = '{0}'", databaseName);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    databaseId = Convert.ToInt32(reader.GetValue(0));
                }
            }

            // Reassign query
            query = String.Format("SELECT accCompanyName FROM account WHERE id = {0}", databaseId);
            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    result = reader.GetString(0);
                }
            }

            return result;
        }

    }

}
