using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using HighRiskCheckpoiont.HelperClass;
using UniGuardLib;

namespace HighRiskCheckpoiont
{
    class HighRiskData
    {
        // Set class variables
        public List<CheckPointHighRisk> HighRiskDataList { get; set; }
        public int HighRiskDataListCount = 0;

        private string databaseName;
        private string database;
        private Data data;
        private double gmtOffset = 0;
       

        public HighRiskData(string databaseName)
        {
            // Initialize variables
            this.databaseName = databaseName;
            this.database = "ug12db_" + this.databaseName;

            // Access data
            data = new Data(this.databaseName);
        }

        /// <summary>
        /// Retrieves Data from HighRiskCheckpoint Table
        /// </summary>
        public void RetrieveData()
        {

            Database db = new Database(this.database);
            HighRiskDataList = new List<CheckPointHighRisk>();

            string query = @"SELECT checkpointhighrisk.id,
                                    checkpointhighrisk.checkpoint_id,
                                    checkpointhighrisk.chrTimeAllowance,
                                    checkpointhighrisk.chrCheckOffset,
                                    checkpointhighrisk.chrStartTime,
                                    checkpointhighrisk.chrLastCheck,
                                    checkpointhighrisk.chrActive,
                                    branch.zone_id

                            FROM    checkpointhighrisk
                                    JOIN checkpoint ON checkpointhighrisk.checkpoint_id = checkpoint.id
                                    JOIN site ON checkpoint.site_id = site.id
                                    JOIN region ON site.region_id = region.id
                                    JOIN branch ON region.branch_id = branch.id
                                  
                            WHERE   checkpointhighrisk.chrActive = 1";

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    try
                    {
                        bool isNull = (reader.GetValue(4) is System.DBNull && reader.GetValue(5) is System.DBNull);
                        this.gmtOffset = data.GetZoneOffset(Convert.ToInt32(reader.GetValue(7)));

                        if (isNull)
                            HighRiskDataList.Add(new CheckPointHighRisk(
                                reader.GetValue(0), // Id
                                reader.GetValue(1), // Checkpoint Id 
                                reader.GetValue(2), // Time Allowance
                                reader.GetValue(3), // Check Offset
                                true,               // Last Check
                                true,               // Start time
                                reader.GetValue(6), // Active
                                this.gmtOffset      // GMT Offset
                            ));

                        else
                            HighRiskDataList.Add(new CheckPointHighRisk(
                                reader.GetValue(0), // Id
                                reader.GetValue(1), // Checkpoint Id 
                                reader.GetValue(2), // Time Allowance
                                reader.GetValue(3), // Check Offset
                                reader.GetValue(4), // Last Check
                                reader.GetValue(5), // Start time
                                reader.GetValue(6), // Active
                                this.gmtOffset      // GMT Offset
                            ));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error: " + ex.Message);
                    }
                }
            }

            //set Count for checking in HighRisk Class
            HighRiskDataListCount = HighRiskDataList.Count;
        }

        /// <summary>
        /// Data Processing engine
        /// 1. Checks for null chrStartDate, chrStartTime => intialises to UtcNow in Checkand InitialiseNull
        ///    Records if true.
        /// 
        /// 2. Checks for validity of time elapsed since start against chrLastCheck
        /// 
        /// 3. If it fails, an email will be sent, exception created and update chrLastChecked
        /// </summary>
        public void ProcessData()
        {
            foreach (CheckPointHighRisk HighRiskRecord in HighRiskDataList)
            {
                if (HighRiskRecord.startCheckNull == true && HighRiskRecord.lastCheckNull == true)
                {
                    CheckandInitialiseNullRecords(HighRiskRecord);
                }
                else
                {
                    //if check comes back false, time has been exceeded
                    if (IsCheckLastCheckedTime(HighRiskRecord) == false)
                    {
                        try
                        {

                            ProcessEmailAlert(HighRiskRecord);
                            CreateException(HighRiskRecord.id);
                            UpdateLastCheck(HighRiskRecord.id);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Database update error:\r\n" + ex.Message);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process emails to send
        /// </summary>
        /// <param name="HighRiskRecord_Id"></param>
        private void ProcessEmailAlert(CheckPointHighRisk HighRiskRecord)
        {
            List<string> emailList = new List<string>();

            GetEmailList(HighRiskRecord.id, emailList);

            if (emailList.Count > 0)
            {
                try
                {
                    Mailer mail = new Mailer();
                    mail.Recipients = emailList;
                    mail.Visit = GetMissedVisit(HighRiskRecord);

                    //there are circumstances where doing a search for GetMissedVisit, there is no data returned
                    //this should only mail out if there is data returned, else empty alert is useless.
                    if (HighRiskRecord.anyMissedVisitData != true)
                    {
                        mail.sendMail();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Mailer setup error:\r\n" + ex.Message);
                }
            }


        }
        /// <summary>
        /// Writes data to the database for the records and sets the chrStartDate and chrLastCheck
        /// </summary>
        /// <param name="HighRiskData"></param>
        private void CheckandInitialiseNullRecords(CheckPointHighRisk HighRiskRecord)
        {
            Database db = new Database(this.database);

            UpdateQuery update = new UpdateQuery();
            update.SetTable("checkpointhighrisk");
            update.SetFields(new string[] { "chrLastCheck", "chrStartTime" });

            string _nullDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            List<string> _nullDates = new List<string>();
            _nullDates.Add(_nullDateTime);
            _nullDates.Add(_nullDateTime);

            update.SetRowValues(_nullDates);
            update.SetId(HighRiskRecord.id.ToString());

            db.Update(update);
        }

        /// <summary>
        /// Get Email Addresses from checkpointhighriskemail table
        /// </summary>
        private void GetEmailList(int HighRiskRecord_Id, List<string> emailList)
        {
            Database db = new Database(this.database);
            string query = String.Format(


                //"SELECT cheEmail FROM checkpointhighriskemail WHERE checkpointhighrisk_id = {0}", 


                //modification to allow for only active checkpoints to be emailed
                @"SELECT cheEmail
                FROM checkpointhighriskemail
                INNER JOIN checkpointhighrisk on checkpointhighrisk.id=checkpointhighriskemail.checkpointhighrisk_id

                WHERE checkpointhighrisk_id = {0} and checkpointhighrisk.chrActive=1",
                HighRiskRecord_Id);


            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    emailList.Add(reader.GetString(0));
                }
            }
        }

        /// <summary>
        /// Gets the missed visit details
        /// </summary>
        /// <param name="HighRiskRecord_Id">Checkpoint ID</param>
        /// <returns>
        ///     object[] { checkpoint, tagNo, site, region, exception date, exception time }
        /// </returns>
        private object[] GetMissedVisit(CheckPointHighRisk HighRiskRecord)
        {
            object[] result = new object[7];
            string query;

            Database db = new Database(this.database);

            // Get exception details
            query = String.Format(@"
                SELECT  checkpointhighriskexception.hreDate,
                        checkpointhighriskexception.hreTime,
                        checkpoint.chpDescription,
                        tag.tagTSN,
                        site.sitName,
                        region.regName
                  FROM  checkpointhighriskexception
                        JOIN checkpointhighrisk ON checkpointhighriskexception.checkpointhighrisk_id = checkpointhighrisk.id
                        JOIN checkpoint ON checkpointhighrisk.checkpoint_id = checkpoint.id
                        JOIN tag on checkpoint.tag_id = tag.id
                        JOIN site on checkpoint.site_id = site.id
                        JOIN region on site.region_id = region.id
                 WHERE  checkpointhighrisk_id = {0} 
              ORDER BY  checkpointhighriskexception.hreDate DESC, 
                        checkpointhighriskexception.hreTime DESC 
                 LIMIT  1
            ", HighRiskRecord.id);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    result[0] = reader.GetDateTime(0).ToString("dd/MM/yyyy");
                    result[1] = reader.GetValue(1).ToString();
                    result[2] = reader.GetString(2);
                    result[3] = reader.GetString(3);
                    result[4] = reader.GetString(4);
                    result[5] = reader.GetString(5);

                    if (result == null)
                    {
                        HighRiskRecord.anyMissedVisitData = true;
                    }
                    else
                    {
                        HighRiskRecord.anyMissedVisitData = false;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// class to check for valid or invalid time interval since Lastcheck
        /// </summary>
        /// <param name="HighRiskRecord"></param>
        /// <returns></returns>
        private bool IsCheckLastCheckedTime(CheckPointHighRisk HighRiskRecord)
        {
            int timeAllowance = HighRiskRecord.chrTimeAllowance;
            int timeOffset = HighRiskRecord.chrCheckOffset;
            DateTime start = HighRiskRecord.chrStartTime;

            double secondsSinceLastHit   = this.GetSecondsSinceLastHit(HighRiskRecord);
            double secondsSinceLastCheck = this.GetSecondsSinceLastCheck(HighRiskRecord.id);
            double secondsSinceStart     = this.GetSecondsSinceStart(HighRiskRecord.chrStartTime);

            // Check that the seconds since the start of the shift are greater than the time allowance
            // And the number of seconds since the last hit is also greater than the time allowance
            // And the number of seconds since the last check are greater than the time offset
            return !((secondsSinceStart > timeAllowance) && (secondsSinceLastHit > timeAllowance) && (secondsSinceLastCheck > timeOffset));
        }

        /// <summary>
        /// Get total seconds since start of welfare check period
        /// </summary>
        /// <returns></returns>
        private double GetSecondsSinceStart(DateTime HighRiskStartTime)
        {
            return Math.Floor((Utility.Now(this.gmtOffset) - HighRiskStartTime).TotalSeconds);
        }

        private double GetSecondsSinceLastHit(CheckPointHighRisk HighRiskRecord)
        {
            Database db = new Database(this.database);
            double result = 0;
            string query = String.Format(@"
                SELECT  TIMESTAMP(patrol.patDate, patrol.patTime) as 'timestamp'
                FROM    patrol
                        LEFT JOIN tag ON patrol.patTSN = tag.tagTSN
                        LEFT JOIN checkpoint ON checkpoint.tag_id = tag.id
                     

                WHERE   checkpoint.id = {0}

                ORDER   BY patrol.patDate DESC, patrol.patTime DESC LIMIT 1
            ", HighRiskRecord.checkpoint_id);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    DateTime last = reader.GetDateTime(0);
                    DateTime now = Utility.Now(this.gmtOffset);
                    TimeSpan ts = now - last;
                    result = Math.Floor(ts.TotalSeconds);
                }
            }

            return result;
        }
        private double GetSecondsSinceLastCheck(int HighRiskRecord_Id)
        {
            Database db = new Database(this.database);
            double result = 0;
            string query = String.Format(
                "SELECT chrLastCheck FROM checkpointhighrisk WHERE id = {0}", HighRiskRecord_Id);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    DateTime last = reader.GetDateTime(0);
                    DateTime now = Utility.Now();
                    TimeSpan ts = now - last;
                    result = Math.Floor(ts.TotalSeconds);
                }
            }

            return result;
        }

        private void CreateException(int HighRiskRecord_Id)
        {
            // Create insert query
            InsertQuery insert = new InsertQuery();
            insert.SetTable("checkpointhighriskexception");
            insert.SetFields(new string[] { "checkpointhighrisk_id", "hreDate", "hreTime" });
            insert.AddRowValues(new string[] {
                HighRiskRecord_Id.ToString(),
                Utility.Now(this.gmtOffset).ToString("yyyy-MM-dd"),
                Utility.Now(this.gmtOffset).ToString("HH:mm:ss")    
            });

            // Database
            Database db = new Database(this.database);
            db.Insert(insert);
        }

        private void UpdateLastCheck(int HighRiskRecord_Id)
        {
            Database db = new Database(this.database);

            UpdateQuery update = new UpdateQuery();
            update.SetTable("checkpointhighrisk");
            update.SetFields(new string[] { "chrLastCheck" });
            update.AddRowValue(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            update.SetId(HighRiskRecord_Id.ToString());

            db.Update(update);
        }
    }
}

