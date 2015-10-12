using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MySql.Data.MySqlClient;
using System.Text;
using UniGuardLib;

namespace SiteLoop
{
    class SiteLoop
    {
        private int dayOfWeek;
        private int loopId;
        private int siteId;
        private int branchId;
        private double gmtOffset;
        private double interval;
        private double preWarnInterval;
        private double minimumCompliance;
        private double secondsSinceLastCheck;
        private string databaseName;
        private string database;
        private bool preWarned;
        private Data data;
        private List<string> checkpoints;
        private List<string> emailList;
        private List<MissingCheckpoint> missingCheckpoints;
        private List<int[]> loopIds;
        private DateTime start;
        private DateTime end;
        private DateTime lastCheck;
        private DateTime lastCompletion;

        StringBuilder testCase = new StringBuilder();

        public SiteLoop(string databaseName)
        {
            // Initialize variables
            this.databaseName = databaseName;
            this.database = "ug12db_" + this.databaseName;

            // Access data
            this.data = new Data(this.databaseName);
            this.loopIds = data.GetAllSiteLoopsIds();
        }

        public void Run()
        {
            foreach (var ids in loopIds)
            {
                this.loopId = ids[0];
                this.siteId = ids[1];
                this.branchId = ids[2];
                this.gmtOffset = data.GetZoneOffset(ids[3]);

                // Refresh checkpoints list
                this.checkpoints = new List<string>();
                this.missingCheckpoints = new List<MissingCheckpoint>();
                this.emailList = new List<string>();

                // Process the loop
                ProcessSiteLoop();
            }
        }

        /// <summary>
        /// Main processing of site loop logic
        /// </summary>
        private void ProcessSiteLoop()
        {
            // Get current day of the week
            this.dayOfWeek = (Int32)Utility.Now(this.gmtOffset).DayOfWeek;

            // Check that loop is currently active and meets all criteria
            if (this.IsActive())
            {
                try
                {
                    // Assign some site loop specific variables to the scope
                    this.GetSiteLoop();

                    // If the number of seconds since the last check is greater
                    // than the set pre warning interval, run the loop checking logic
                    if (this.secondsSinceLastCheck > this.preWarnInterval)
                    {
                        if (!this.preWarned)
                        {
                            if (this.LoopException())
                            {
                                this.ProcessException(true);

                                // Update pre-warning
                                this.UpdatePreWarning();
                            }
                        }
                    }

                    // If the number of seconds since the last check is greater
                    // than the set interval, run the loop checking logic
                    if (this.secondsSinceLastCheck > this.interval)
                    {
                        // Run query
                        if (this.LoopException())
                        {
                            this.ProcessException(false);

                            // Update the last check
                            this.UpdateLastCheck();
                        }

                        // If no loop exception found, mark the run as completed
                        else
                        {
                            this.UpdateLastCompletion(DateTime.Now);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Site Loops error:\r\n" + ex.ToString());
                }
            }

        }

        private void ProcessException(bool isPreWarning)
        {
            // Find out which checkpoints are missing
            this.GetMissingCheckpoints();

            // If there are more than 0 missing checkpoints
            if (this.missingCheckpoints.Count > 0)
            {
                // Create exception
                int loopExceptionID = this.CreateException();

                // Add data to exception
                foreach (var mchps in this.missingCheckpoints)
                {
                    this.InsertIntoException(loopExceptionID, mchps.CheckpointID);
                }

                // Get list of emails
                this.GetEmailList(isPreWarning);

                // Setup the email
                Mailer mailer = new Mailer(isPreWarning);
                mailer.Recipients = this.emailList;
                mailer.MissingCheckpoints = this.missingCheckpoints;

                // Send the email
                mailer.sendMail();
            }
        }

        /// <summary>
        /// Gets the email list to contact in case of an exception
        /// </summary>
        private void GetEmailList(bool isPreWarning)
        {
            // Connect to database
            Database db = new Database(this.database);

            string preWarning = isPreWarning ? "1" : "0";

            // Query
            string q1 = String.Format("SELECT loeEmail FROM loopemail WHERE loop_id = {0} AND loePreWarn = {1}", this.loopId, preWarning);

            try
            {
                // Insert results into list
                using (var reader = db.Query(q1))
                    while (reader.Read())
                        this.emailList.Add(reader.GetString(0));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// Creates a loop exception and returns its ID
        /// </summary>
        private int CreateException()
        {
            // Connect to the database
            Database db = new Database(this.database);

            // Create insert query
            InsertQuery insert = new InsertQuery();
            insert.SetTable("loopexception");
            insert.SetFields(new string[3] { "loop_id", "lexDate", "lexTime" });
            insert.AddRowValues(new string[3] {
                this.loopId.ToString(),
                Utility.Now(this.gmtOffset).ToString("yyyy-MM-dd"),
                Utility.Now(this.gmtOffset).ToString("HH:mm:ss")
            });

            // Insert
            db.Insert(insert);

            // Return last insert ID
            return db.GetLastColumn("loopexception", "id");
        }

        /// <summary>
        /// Insert data into exception
        /// </summary>
        /// <param name="loopExceptionId"></param>
        /// <param name="checkpointId"></param>
        private void InsertIntoException(int loopExceptionId, int checkpointId)
        {
            // Connect to the database
            Database db = new Database(this.database);

            // Create insert query
            InsertQuery insert = new InsertQuery();
            insert.SetTable("loopexceptioncheckpoint");
            insert.SetFields(new string[2] { "loopexception_id", "checkpoint_id" });
            insert.AddRowValues(new string[2] {
                loopExceptionId.ToString(),
                checkpointId.ToString()
            });

            // Insert
            db.Insert(insert);
        }

        /// <summary>
        /// Gets the missing checkpoints from the breaching loop
        /// </summary>
        private void GetMissingCheckpoints()
        {
            // Connect to the database
            Database db = new Database(this.database);

            // Create the query string for the existing checkpoints
            string chpString = this.checkpoints.Count > 0 ?
                String.Format(" AND tag.tagTSN NOT IN ({0})", string.Join(", ", this.checkpoints.ToArray())) :
                null;

            // Create query to get missing checkpoints
            string query = String.Format(@"
                SELECT  checkpoint.id,
                        checkpoint.chpDescription,
                        site.sitName,
                        tag.tagTSN
                FROM    tag
                        JOIN checkpoint ON checkpoint.tag_id = tag.id
                        JOIN site ON checkpoint.site_id = site.id
                WHERE   site.id = {0}
                  AND   checkpoint.id NOT IN (SELECT checkpoint_id FROM loopexclusion WHERE loop_id = {1})
                        {2}
            ", this.siteId, this.loopId, chpString);

            // Get response
            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    // Create new missing checkpoint
                    MissingCheckpoint mc = new MissingCheckpoint();
                    mc.CheckpointID = reader.GetInt32(0);
                    mc.Description = reader.GetString(1);
                    mc.SiteName = reader.GetString(2);
                    mc.TagSerial = reader.GetString(3);

                    // Add it to the list
                    this.missingCheckpoints.Add(mc);
                }
            }
        }

        /// <summary>
        /// Gets number of checkpoints in site
        /// </summary>
        /// <returns></returns>
        private double NumberOfCheckpoints()
        {
            // Connect to the database
            Database db = new Database(this.database);

            double numCheckpoints = 0;

            // Get number of checkpoints in site
            string q1 = String.Format(@"
                SELECT  COUNT(*) as 'num' 
                  FROM  checkpoint 
                 WHERE  site_id = {0} 
                   AND  checkpoint.id NOT IN (SELECT checkpoint_id FROM loopexclusion WHERE loop_id = {1})
            ", this.siteId, this.loopId);

            // Get response
            using (var reader = db.Query(q1))
                while (reader.Read())
                    numCheckpoints = Convert.ToDouble(reader.GetValue(0));

            return numCheckpoints;
        }

        /// <summary>
        /// Determines wether or not the loop raises an exception
        /// </summary>
        /// <returns></returns>
        private bool LoopException()
        {
            // Connect to the database
            Database db = new Database(this.database);

            double numCheckpoints = this.NumberOfCheckpoints();

            // Ensure the number of checkpoints is more than 0
            if (numCheckpoints > 0)
            {
                // Determine the parameters for the query:
                // Query: select checkpoint from site -- start time: this.lastCompletion, end time: now,
                //        then make sure compliance percentage is met.
                //        also, exclude checkpoints which are in exclusion list
                string query = String.Format(@"
                    SELECT DISTINCT patrol.patTSN
                    FROM    patrol
                            LEFT JOIN tag ON tag.tagTSN = patrol.patTSN
                            LEFT JOIN checkpoint ON checkpoint.tag_id = tag.id
                    WHERE   checkpoint.site_id = {0}
                      AND   checkpoint.id NOT IN (SELECT checkpoint_id FROM loopexclusion WHERE loop_id = {1})
                      AND   TIMESTAMP(patrol.patDate, patrol.patTime) BETWEEN '{2}' AND '{3}'
                ", this.siteId, this.loopId, this.start.ToString("yyyy-MM-dd HH:mm:ss"), this.end.ToString("yyyy-MM-dd HH:mm:ss"));

                // Add response to checkpoints list
                using (var reader = db.Query(query))
                    while (reader.Read())
                        this.checkpoints.Add(reader.GetValue(0).ToString());

                // If the count of returned checkpoints is lower than the count of checkpoints, look closer
                if (checkpoints.Count < numCheckpoints)
                {
                    // Check percentage
                    double percentage = Math.Abs((checkpoints.Count / numCheckpoints) * 100);

                    // If the percentage visited is lower than the minimum compliance percentage
                    if (percentage < this.minimumCompliance)
                    {
                        // Return exception alert
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check that the loop is currently active and continue the check
        /// </summary>
        /// <returns></returns>
        private bool IsActive()
        {
            // We will store all the start and end times in a dates variable as a list
            List<DateTime[]> dateList = new List<DateTime[]>();

            // Connect to database
            Database db = new Database(this.database);

            // Get an array of all the dates for the current week in loop
            string query = String.Format(@"
                SELECT  @diff:=( CAST(loopshift.day_id AS SIGNED) - (WEEKDAY(CURRENT_DATE) + 1) ) as 'diff',
    		            @date:=DATE_ADD(CURRENT_DATE, INTERVAL @diff DAY) as 'start_date',
    		            CONVERT(TIMESTAMP(@date, loopshift.losStart) USING latin1) AS 'start',
    		            CONVERT(TIMESTAMP(IF (loopshift.losFinish <= loopshift.losStart,
    					                     DATE_ADD(@date, INTERVAL 1 DAY),
    					                     @date),
    				                      loopshift.losFinish) USING latin1) AS 'end'
                  FROM  loopshift
                        LEFT JOIN `loop` ON loopshift.loop_id = `loop`.id
                 WHERE  `loop`.id = {0}
                        AND loopshift.losActive = 1
                        AND `loop`.looActive = 1
                        AND loopshift.day_id < 8
            ", this.loopId);

            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
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
                    // First assign the start and end times
                    this.start = dates[0];
                    this.end = dates[1];

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the site loop details and assign them to scope variables.
        /// Will also adjust the last check property if it is null.
        /// </summary>
        private void GetSiteLoop()
        {
            // Connect to database
            Database db = new Database(this.database);
            string query = String.Format(
                "SELECT looPreWarnInterval, looInterval, looMinimumCompliance, looLastCheck, looLastCompletion, looPreWarned FROM `loop` WHERE id = {0}", this.loopId
            );

            // Perform query
            using (var reader = db.Query(query))
            {
                while (reader.Read())
                {
                    // Assign variables
                    this.preWarnInterval = Convert.ToDouble(reader.GetValue(0));
                    this.interval = Convert.ToDouble(reader.GetValue(1));
                    this.minimumCompliance = Convert.ToDouble(reader.GetValue(2));

                    // If the last check is null, mark it as now
                    if (DBNull.Value.Equals(reader.GetValue(3)))
                        this.UpdateLastCheck();

                    // Otherwise assign the last check to the scope
                    else
                        this.lastCheck = reader.GetDateTime(3);

                    // Get the number of seconds since last check
                    this.secondsSinceLastCheck = (DateTime.Now - this.lastCheck).TotalSeconds;

                    // Check if the last completion is not null, and less than the start time
                    if ((!DBNull.Value.Equals(reader.GetValue(4)) && reader.GetDateTime(4) < this.start) || (DBNull.Value.Equals(reader.GetValue(4))))
                    {
                        this.UpdateLastCompletion(this.start.AddMinutes(-10));
                    }

                    // Get pre-warned status
                    this.preWarned = Convert.ToInt32(reader.GetValue(5)) == 0 ? false : true;

                }
            }
        }

        /// <summary>
        /// Updates the last check row in the table
        /// </summary>
        private void UpdateLastCheck()
        {
            Database db = new Database(this.database);
            DateTime dt = DateTime.Now;

            UpdateQuery update = new UpdateQuery();
            update.SetTable("loop");
            update.SetFields(new string[] { "looLastCheck", "looPreWarned" });
            update.AddRowValue(dt.ToString("yyyy-MM-dd HH:mm:ss"));
            update.AddRowValue(0.ToString());
            update.SetId(this.loopId.ToString());

            db.Update(update);

            this.lastCheck = dt;
        }

        /// <summary>
        /// Updates the last completion row in the table
        /// </summary>
        /// <param name="dt"></param>
        private void UpdateLastCompletion(DateTime dt)
        {
            Database db = new Database(this.database);

            UpdateQuery update = new UpdateQuery();
            update.SetTable("loop");
            update.SetFields(new string[] { "looLastCompletion" });
            update.AddRowValue(dt.ToString("yyyy-MM-dd HH:mm:ss"));
            update.SetId(this.loopId.ToString());

            db.Update(update);

            this.lastCompletion = dt;
        }

        /// <summary>
        /// Set the loop pre-warned status to true
        /// </summary>
        private void UpdatePreWarning()
        {
            Database db = new Database(this.database);

            UpdateQuery update = new UpdateQuery();
            update.SetTable("loop");
            update.SetFields(new string[] { "looPreWarned" });
            update.AddRowValue(1.ToString());
            update.SetId(this.loopId.ToString());

            db.Update(update);

            this.preWarned = true;
        }

    }
}
