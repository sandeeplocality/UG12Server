using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGuardLib;

namespace UniGuard12Server
{
    public class GPRSData
    {
        private bool authenticated = false;
        private string recorderSerial;
        private string agency;
        private string imei;
        private string databaseName;
        private string importDate;
        private string importTime;

        // Containers to hold records
        private List<string[]> normalRecords     = new List<string[]>();
        private List<string[]> alarmRecords      = new List<string[]>();
        private List<string[]> customRecords     = new List<string[]>();
        private List<string[]> lowVoltageRecords = new List<string[]>();

        public GPRSData()
        {
            // Set date & time received
            this.importDate = DateTime.Now.ToString("yyyy-MM-dd");
            this.importTime = DateTime.Now.ToString("HH:mm:ss");
        }

        public string Database
        {
            get { return this.databaseName; }
            set { this.databaseName = value; }
        }

        /// <summary>
        /// Set agency. Set only.
        /// </summary>
        public string Agency
        {
            set
            {
                this.agency = value;
                // Now we dissect it!
                string[] agencyParts = this.agency.Split(';');
                // Make sure there are at least 4 parts to the split
                if (agencyParts.Length >= 4)
                {
                    // Check them
                    this.authenticated = agencyParts[0] == "UG12GPRS" ? true : false;
                    this.databaseName = agencyParts[3];
                }
            }
        }

        /// <summary>
        /// Set / Get recorder serial
        /// </summary>
        public string RecorderSerial
        {
            set { this.recorderSerial = value; }
            get { return this.recorderSerial; }
        }

        /// <summary>
        /// Set / Get recorder Imei
        /// </summary>
        public string Imei
        {
            set { this.imei = value; }
            get { return this.imei; }
        }

        // Read-only attributes
        public bool Authenticated { get { return this.authenticated; } }

        /// <summary>
        /// Adds a normal record to the total normal records List
        /// </summary>
        /// <param name="record">
        /// [0] = tagNumber
        /// [1] = date
        /// [2] = time
        /// [3] = recorderSerial
        /// </param>
        public void AddNormalRecord(string[] record)
        {
            this.normalRecords.Add(record);
        }

        /// <summary>
        /// Adds an alarm record to the total alarm records list
        /// </summary>
        /// <param name="record">
        /// [0] = date
        /// [1] = time
        /// [2] = recorderSerial
        /// </param>
        public void AddAlarmRecord(string[] record)
        {
            this.alarmRecords.Add(record);
        }

        /// <summary>
        /// Adds a custom record to the total custom records list
        /// </summary>
        /// <param name="record">
        /// [0] = date
        /// [1] = time
        /// [2] = recorderSerial
        /// </param>
        public void AddCustomRecord(string[] record)
        {
            this.customRecords.Add(record);
        }

        /// <summary>
        /// Adds a low battery record to the total low battery records list
        /// </summary>
        /// <param name="record">
        /// [0] = date
        /// [1] = time
        /// [2] = recorderSerial
        /// [3] = batteryLevel
        /// </param>
        public void AddLowVoltageRecord(string[] record)
        {
            this.lowVoltageRecords.Add(record);
        }

        /// <summary>
        /// Stores GPRS Data to database
        /// </summary>
        public void StoreData()
        {
            // Count everything
            string[][] nr = normalRecords.ToArray();
            string[][] ar = alarmRecords.ToArray();
            string[][] cr = customRecords.ToArray();
            string[][] lr = lowVoltageRecords.ToArray();
            int nrc = nr.Length;
            int arc = ar.Length;
            int crc = cr.Length;
            int lrc = lr.Length;
            int totalRecords = nrc + arc + crc + lrc;
            bool storedData = false;

            // Add data
            Data data = new Data(this.databaseName);

            /*********************************************
             * INSERT IMPORT DATA
             *********************************************/

            InsertQuery insertImport = new InsertQuery();
            insertImport.SetTable("import");
            insertImport.SetFields(new string[4] { "impDate", "impTime", "impRecordCount", "impLiveData" });

            // Adjust time if necessary
            string importDateTime = this.importDate + " " + this.importTime;
            string newImportDate = data.AdjustTimezone(importDateTime, "yyyy-MM-dd", this.recorderSerial);
            string newImportTime = data.AdjustTimezone(importDateTime, "HH:mm:ss", this.recorderSerial);

            // Add rows
            insertImport.AddRowValues(new string[4] { newImportDate, newImportTime, totalRecords.ToString(), "1" });

            // Insert the import and retrieve the import Id
            int importId = data.InsertImport(insertImport);

            /*********************************************
             * INSERT PATROL RECORDS
             *********************************************/
            if (nrc > 0)
            {
                // Prepare new InserQuery for patrol data
                InsertQuery insertPatrol = new InsertQuery();
                insertPatrol.SetTable("patrol");
                insertPatrol.SetFields(new string[5] { "import_id", "patTSN", "patDate", "patTime", "patRSN" });

                // Queue normal records for insertion
                for (int i = 0; i < nrc; ++i)
                {
                    string date = data.AdjustTimezone(nr[i][1], "yyyy-MM-dd", nr[i][2]);
                    string time = data.AdjustTimezone(nr[i][1], "HH:mm:ss", nr[i][2]);

                    if (data.CheckPatrol(nr[i][0], date, time, nr[i][2]))
                        insertPatrol.AddRowValues(new string[5] { importId.ToString(), nr[i][0], date, time, nr[i][2] });
                }

                // Insert it
                if (insertPatrol.Count > 0)
                {
                    data.InsertPatrol(insertPatrol);
                    storedData = true;
                }

            }

            /*********************************************
             * INSERT EVENT RECORDS
             *********************************************/

            if ((arc + crc) > 0)
            {
                // Prepare new InsertQuery for event data
                InsertQuery insertEvent = new InsertQuery();
                insertEvent.SetTable("event");
                insertEvent.SetFields(new string[5] { "import_id", "eventtype_id", "evnDate", "evnTime", "evnRSN" });

                // Queue alarm records for insertion
                if (arc > 0)
                {
                    for (int i = 0; i < arc; ++i)
                    {
                        string date = data.AdjustTimezone(ar[i][0], "yyyy-MM-dd", ar[i][1]);
                        string time = data.AdjustTimezone(ar[i][0], "HH:mm:ss", ar[i][1]);
                        insertEvent.AddRowValues(new string[5] { importId.ToString(), 1.ToString(), date, time, ar[i][1] });
                    }
                }

                // Queue custom records for insertion
                if (crc > 0)
                {
                    for (int i = 0; i < crc; ++i)
                    {
                        string date = data.AdjustTimezone(cr[i][0], "yyyy-MM-dd", cr[i][1]);
                        string time = data.AdjustTimezone(cr[i][0], "HH:mm:ss", cr[i][1]);
                        insertEvent.AddRowValues(new string[5] { importId.ToString(), 2.ToString(), date, time, cr[i][1] });
                    }
                }

                // Insert it
                if (insertEvent.Count > 0)
                {
                    data.InsertEvent(insertEvent);
                    storedData = true;
                }
            }


            /*********************************************
             * INSERT LOW VOLTAGE RECORDS
             *********************************************/

            if (lrc > 0)
            {
                // Prepare new InsertQuery
                InsertQuery insertVoltage = new InsertQuery();
                insertVoltage.SetTable("lowvoltage");
                insertVoltage.SetFields(new string[5] { "import_id", "lowReading", "lowDate", "lowTime", "lowRSN" });

                // Queue low voltage records for insertion
                for (int i = 0; i < lrc; ++i)
                {
                    string date = data.AdjustTimezone(lr[i][0], "yyyy-MM-dd", lr[i][1]);
                    string time = data.AdjustTimezone(lr[i][0], "HH:mm:ss", lr[i][1]);
                    insertVoltage.AddRowValues(new string[5] { importId.ToString(), lr[i][2], date, time, lr[i][1] });
                }

                // Insert it
                if (insertVoltage.Count > 0)
                {
                    data.InsertLowVoltage(insertVoltage);
                    storedData = true;
                }
            }

            /*********************************************
             * INSERT UPLOAD ACTIVITY
             *********************************************/

            // Only required for new legacy databases
            InsertQuery insertUploadActivity = new InsertQuery();
            insertUploadActivity.SetTable("uploadactivity");
            insertUploadActivity.SetFields(new string[5] { "account_id", "uplDate", "uplTime", "uplRecords", "uplLiveData" });

            // Add rows
            string accountId = data.GetAccountIdForDatabase().ToString();
            insertUploadActivity.AddRowValues(new string[5] { accountId, importDate, importTime, totalRecords.ToString(), "1" });

            // Insert it
            if (storedData)
                data.InsertUploadActivity(insertUploadActivity);
            else
                // Delete the import if not used
                data.DeleteImport(importId);
        }

    }
}
