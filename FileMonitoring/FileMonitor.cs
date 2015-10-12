using System;
using System.IO;
using System.ServiceProcess;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Text.RegularExpressions;
using UniGuardLib;

namespace FileMonitoring
{
    public class FileMonitor
    {
        const string IMPORTPATH    = @"C:\HostingSpaces\Portal\imports\";
        const string APSPATH       = @"C:\HostingSpaces\APS\ftpRoot\";
        public static bool running;
        private static System.Timers.Timer timer;

        public FileMonitor()
        {
            running = false;
            // Set up the interval timer and start it
            timer = new System.Timers.Timer();
            timer.Interval  = 1000;
            timer.Elapsed  += new ElapsedEventHandler(this.OnTimedEvent);
            timer.AutoReset = false;
            timer.Enabled   = true;
        }

        /// <summary>
        /// This is the timer tick event for the timer loop
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                // Stop the timer
                timer.Stop();
                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12FileMonitoring"))
                {
                    ServiceController sc = new ServiceController("UniGuard12FileMonitoring");
                    sc.Stop();
                }
                else
                {
                    if (running) return;
                    running = true;
                    // Run the method
                    this.MonitorFiles();
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions in the stack
                Log.Error(ex.ToString());
            }
            finally
            {
                running = false;
                // Restart the timer
                timer.Start();
            }
        }

        /// <summary>
        /// This is the main method of this class, it monitors the file structure from
        /// IMPORTPATH and its subdirectories. The Uploader software on client machines
        /// will SFTP files into this directory structure and it is this class' job to
        /// get the files, decrypt them, enter them into the database and move them away.
        /// </summary>
        private void MonitorFiles()
        {
            // FIRST PASS:
            // First pass will check the APS' ftpRoot folder and check for any data
            // incoming from the MK1 GPRS recorders. It will then move this data to the
            // import folders where the iteration will finalize and input the records

            string[] apsPaths = Utility.GetAllDirectories(APSPATH);
            foreach (string path in apsPaths)
            {
                // Skip the 'bad' directory
                if (path != APSPATH + "bad")
                {
                    // Get the out path
                    string outPath = path + @"\out\";

                    // Get the databasename
                    DirectoryInfo dbDir = new DirectoryInfo(path);
                    string dirName      = dbDir.Name;
                    string dbName       = Database.GetSchemaName(dirName);

                    // Let's also make sure that the History and Exceptions directories are present
                    DirectoryInfo hisDir = new DirectoryInfo(outPath + @"\History");
                    DirectoryInfo excDir = new DirectoryInfo(outPath + @"\Exceptions");
                    if (!hisDir.Exists) hisDir.Create();
                    if (!excDir.Exists) excDir.Create();

                    // Store the files in an array
                    FileInfo[] files = Utility.GetallFilesFromDirectory(outPath, new string[1] { "*.import" });

                    // Move them
                    foreach (FileInfo file in files)
                    {
                        WaitForFile(file);
                        
                        // Get filename without extension
                        string fileName = Path.GetFileNameWithoutExtension(file.FullName);

                        // Copy to import directory
                        try
                        {
                            File.Copy(file.FullName, IMPORTPATH + dbName + @"\" + fileName + ".uif");
                            File.Move(file.FullName, hisDir.FullName + @"\" + file.Name);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Could not copy import file to imports directory" + Environment.NewLine + ex.ToString());
                            File.Move(file.FullName, excDir.FullName + file.Name);
                        }
                    }
                }
            }

            // Check that the imports directory exists, if not; create it
            DirectoryInfo dir = new DirectoryInfo(IMPORTPATH);
            if (!dir.Exists) dir.Create();

            // Get paths from import directory
            string[] paths = Utility.GetAllDirectories(IMPORTPATH);

            // Loop over string array and print each string to the log
            foreach (string path in paths)
            {
                // Let's make sure that the History and Exceptions directories are present
                DirectoryInfo hisDir = new DirectoryInfo(path + @"\History");
                DirectoryInfo excDir = new DirectoryInfo(path + @"\Exceptions");
                if (!hisDir.Exists) hisDir.Create();
                if (!excDir.Exists) excDir.Create();

                // Store the files in an array
                FileInfo[] files = Utility.GetallFilesFromDirectory(path, new string[4] { "*.export", "*.uef", "*.uif", "*.plain" });

                // Display them
                foreach (FileInfo file in files)
                {
                    WaitForFile(file);
                    new FileDecrypt(file);
                }
            }
        }

        private void WaitForFile(FileInfo file)
        {
            long fileSizeOld = 0;
            long fileSizeNew = 0;
            // Let's compare file sizes to ensure the download is done
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    // File name
                    string fileName = file.FullName;

                    // Check once
                    FileInfo f1 = new FileInfo(fileName);
                    fileSizeOld = f1.Length;
                    Thread.Sleep(1000);

                    // Check again
                    FileInfo f2 = new FileInfo(fileName);
                    fileSizeNew = f2.Length;

                    // If the old filesize is the same as the new file size and is also more than 0, exit loop
                    if ((fileSizeOld == fileSizeNew) && (fileSizeOld + fileSizeNew > 0))
                        break;
                }
                catch (Exception ex)
                {
                    Log.Error("File read error:\r\n" + ex.ToString());
                }
            }
        }

    }

    public class FileDecrypt
    {
        FileInfo file;
        private string databaseName;
        private const string TEMPPATH = @"C:\HostingSpaces\Portal\temp\";

        /// <summary>
        /// This method checks for copies of the file passed to it in the history and
        /// exception directories inside the database host directory. If it does not
        /// find copies of those files it will determine which decrypt process to run
        /// the file through and call the appropriate method.
        /// </summary>
        /// <param name="file">The FileInfo object of the file to be decrypted</param>
        public FileDecrypt(FileInfo file)
        {
            // Set the file locally
            this.file = file;

            // History and Exception paths
            string histPath = this.file.DirectoryName + @"\History\";
            string excpPath = this.file.DirectoryName + @"\Exceptions\";

            // Set default response
            int decryptResponse = 4;

            // Set database name: Ensure to use hacks utility to rename databases which require it
            this.databaseName = Hacks.AdjustDatabase(this.file.Directory.Name);

            // Before we attempt to decrypt the file, check for identical files in History and Exceptions
            FileInfo histFile = new FileInfo(histPath + this.file.Name);
            FileInfo excpFile = new FileInfo(excpPath + this.file.Name);

            // If a copy of the file exists in history, put it into Exceptions, unless it exists there too
            if (histFile.Exists)
            {
                if (excpFile.Exists)
                {
                    // If it exists in both, just delete it.
                    try
                    {
                        this.file.Delete();
                        Log.Warning("Deleted " + this.file.FullName + ", it was already present in History and Exceptions.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not delete the file " + this.file.FullName + "\r\n" + ex.Message);
                    }
                    return;
                }
                else
                {
                    this.file.MoveTo(excpPath + this.file.Name);
                    Log.Warning("Moved " + this.file.FullName + " to Exceptions as it was already present in History.");
                    return;
                }
            }

            // Decrypt the file depending on its extension
            try
            {
                // If the file is an export file, run DecryptExport()
                if (file.Extension == ".export")
                    decryptResponse = this.DecryptExport();

                // If it is a uef DecryptUEF()
                if (file.Extension == ".uef")
                    decryptResponse = this.DecryptUEF();

                // If its an import file, it's already decrypted
                if (file.Extension == ".uif" || file.Extension == ".plain")
                    decryptResponse = this.DecryptUIF();

            }
            catch (Exception ex)
            {
                Log.Error("Decrypt error." + ex.Message);
            }

            // Let's force a wait here...
            Thread.Sleep(500);

            // Determine the appropriate action to take once the file is decrypted
            switch (decryptResponse)
            {
                case 0:
                    // Success, therefore we move the file to it's History directory
                    try
                    {
                        this.file.MoveTo(histPath + this.file.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(String.Format("Could not move file during decryption. {0}, {1}", this.file.FullName, ex.Message));
                    }
                    break;

                case 1:
                    Log.Warning("Insufficient arguments when decrypting " + file.Name);
                    break;

                case 2:
                    Log.Warning("Source file does not exist: " + file.Name);
                    break;

                case 3:
                    Log.Warning("Problem saving " + file.Name + " to target destination.");
                    break;

                case 4:
                    Log.Warning("Error decrypting file: " + file.Name);
                    break;

                case 5:
                    Thread.Sleep(1000);
                    break;

            }
        }

        /// <summary>
        /// Decrypts a UEF file to readable text format
        /// </summary>
        /// <returns></returns>
        private int DecryptUEF()
        {
            int response = 4;
            string line;
            string fullString = "";
            string uncompString = "";

            // Create the directory if it doesn't exist
            DirectoryInfo dir = new DirectoryInfo(TEMPPATH);
            if (!dir.Exists) dir.Create();

            try
            {
                StreamReader reader = new StreamReader(this.file.FullName);
                while ((line = reader.ReadLine()) != null)
                {
                    fullString += line;
                }
                // Close the reader
                reader.Close();

                // Decompress and decrypt text
                CompressString compString = new CompressString(System.Text.Encoding.UTF8);
                compString.Compressed = fullString;
                uncompString = compString.UnCompressed;
            }
            catch (IOException)
            {
                // If it is an IOException, let's see if we can wait for the file to be ready
                for (int i = 0; i < 20; i++)
                {
                    Thread.Sleep(500);
                    if (Utility.IsFileReady(this.file.FullName))
                    {
                        return 5;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error reading file " + this.file.FullName + "\r\n" + ex.ToString());
            }

            try
            {
                // Write the string to the file
                StreamWriter writer = new StreamWriter(TEMPPATH + this.file.Name);
                writer.Write(uncompString);
                writer.Close();
                response = 0;
            }
            catch (IOException)
            {
                response = 3;
            }

            // If we got the best response, store the data
            if (response == 0)
            {
                // Store data from file
                try
                {
                    if (!this.storeDataFromFile()) response = 5;
                }
                catch (Exception ex)
                {
                    Log.Error("Could not store data to database:\r\n" + ex.ToString());
                }
            }
            else
            {
                Log.Warning("Bad UEF file decryption " + TEMPPATH + this.file.Name);
            }

            return response;
        }

        /// <summary>
        /// This method retrieves the ExpUp.exe embedded resource and transfers it to the
        /// local disk in a temp folder where it runs it, sends the order to store the data,
        /// then moves the file away to the History directory whithin the subdirectory
        /// </summary>
        /// <returns>
        ///     int 0 = Success
        ///     int 1 = Insufficient arguments
        ///     int 2 = Source file does not exist
        ///     int 3 = Problem saving destination file
        ///     int 4 = Method failed to finish
        /// </returns>
        private int DecryptExport()
        {
            int response = 4;

            // Create the directory if it doesn't exist
            DirectoryInfo dir = new DirectoryInfo(TEMPPATH);
            if (!dir.Exists) dir.Create();

            // Prepare the embedded executable
            byte[] exeBytes = Properties.Resources.ExpUp;
            string exeToRun = TEMPPATH + "ExpUp.exe";
            FileInfo exe = new FileInfo(exeToRun);

            // If file does not already exist, create it
            if (!exe.Exists)
            {
                try
                {
                    // Create the file
                    using (FileStream exeFile = new FileStream(exeToRun, FileMode.CreateNew))
                    {
                        exeFile.Write(exeBytes, 0, exeBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }

            }

            // Attempt to run the file
            using (Process exeProcess = new Process())
            {
                // Setup the process and start it
                try
                {
                    exeProcess.StartInfo.UseShellExecute = false;
                    exeProcess.StartInfo.FileName = exeToRun;
                    exeProcess.StartInfo.CreateNoWindow = true;
                    exeProcess.StartInfo.Arguments = file.FullName + " " + TEMPPATH + file.Name;
                    exeProcess.Start();
                }
                catch (Exception ex)
                {
                    Log.Error("Could not start embedded resource ExpUp.exe\r\n" + ex.ToString());
                }

                // Handle exit and timeout
                if (exeProcess.WaitForExit(5000))
                {
                    response = exeProcess.ExitCode;
                }
                else
                {
                    // Kill the process
                    try
                    {
                        exeProcess.Kill();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("ExpUp process did not exit gracefully, so we killed it dead!\r\n" + ex.ToString());
                    }

                    // Check that the file is there, however
                    FileInfo createdFile = new FileInfo(TEMPPATH + this.file.Name);
                    if (createdFile.Exists) response = 0;
                }

            }

            // If we got the best response, store the data
            if (response == 0)
            {
                // Store data from file
                try
                {
                    if (!this.storeDataFromFile()) response = 5;
                }
                catch (Exception ex)
                {
                    Log.Error("Could not store data to database:\r\n" + ex.ToString());
                }
            }
            else
            {
                Log.Warning("Bad file decryption from the ExpUp process for " + TEMPPATH + this.file.Name);
            }

            // Attempt to delete the file
            try
            {
                exe.Delete();
            }
            catch (Exception ex)
            {
                Log.Error("Could not delete embedded resource ExpUp\r\n" + ex.ToString());
            }

            return response;
        }

        private int DecryptUIF()
        {
            int response = 4;

            // Create the directory if it doesn't exist
            DirectoryInfo dir = new DirectoryInfo(TEMPPATH);
            if (!dir.Exists) dir.Create();

            try
            {
                File.Copy(this.file.FullName, TEMPPATH + @"\" + this.file.Name);
                response = 0;
            }
            catch (Exception ex)
            {
                Log.Error("Could not move file to temp out directory. " + this.file.FullName + Environment.NewLine + ex.ToString());
            }

            // If we got the best response, store the data
            if (response == 0)
            {
                // Store data from file
                try
                {
                    bool liveData = true;

                    // Check if its a plain file
                    if (this.file.Extension == ".plain")
                        liveData = false;

                    // Check if the file extension is uif but the file name contains a machine ID
                    if (this.file.Extension == ".uif")
                    {
                        Match match = Regex.Match(
                            this.file.Name,
                            @"[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}-[A-F0-9]{2}_+"
                        );

                        if (match.Success)
                            liveData = false;
                    }

                    if (!this.storeDataFromFile(liveData)) response = 5;
                }
                catch (Exception ex)
                {
                    Log.Error("Could not store data to database:\r\n" + ex.ToString());
                }
            }

            return response;
        }

        /// <summary>
        /// Reads the current working file and stores it to the database,
        /// then deletes the (temporary) file.
        /// </summary>
        private bool storeDataFromFile(bool liveData = false)
        {
            // If it's a uif file, move it to temp

            // Get the new file
            FileInfo newFile = new FileInfo(TEMPPATH + file.Name);
            Data data = new Data(this.databaseName);

            string line;
            string recorderSerial = null;
            List<string> lines = new List<string>();
            int totalRecords = Utility.TotalLines(newFile.FullName);
            // Kill if total records = 0
            if (totalRecords == 0) return false;

            // Let's read the first line and get the serial number
            try
            {
                string firstLine = null;
                using (StreamReader reader = new StreamReader(newFile.FullName))
                {
                    firstLine = reader.ReadLine();
                    reader.Close();
                }

                // Let's get the serial number
                string[] sampleData = firstLine.Split(',');
                recorderSerial = sampleData[5];
            }
            catch (Exception ex)
            {
                Log.Error("File read error: " + ex.ToString());
            }

            string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string currentDate = data.AdjustTimezone(dt, "yyyy-MM-dd", recorderSerial);
            string currentTime = data.AdjustTimezone(dt, "HH:mm:ss", recorderSerial);

            // Initialise the InsertQuery method for Imports
            InsertQuery insertImport = new InsertQuery();
            insertImport.SetTable("import");
            insertImport.SetFields(new string[] { "impDate", "impTime", "impRecordCount", "impLiveData" });

            // Add rows
            insertImport.AddRowValues(new string[] {
                currentDate,
                currentTime,
                totalRecords.ToString(),
                liveData ? "1" : "0"
            });

            // Add data
            int importId = data.InsertImport(insertImport);

            // Prepare a new InserQuery for shock data
            InsertQuery insertShock = new InsertQuery();
            insertShock.SetTable("shock");
            insertShock.SetFields(new string[] { "import_id", "shocktype_id", "shkDate", "shkTime", "shkRSN" });

            // Prepare new InserQuery for patrol data
            InsertQuery insertPatrol = new InsertQuery();
            insertPatrol.SetTable("patrol");
            insertPatrol.SetFields(new string[] { "import_id", "patTSN", "patDate", "patTime", "patRSN" });

            // Prepare a new InsertQuery for lowvoltage data
            InsertQuery insertVoltage = new InsertQuery();
            insertVoltage.SetTable("lowvoltage");
            insertVoltage.SetFields(new string[] { "import_id", "lowReading", "lowDate", "lowTime", "lowRSN"});

            // Read the file line by line
            try
            {
                // Initialise StreamReader
                using (StreamReader reader = new StreamReader(newFile.FullName))
                {
                    int patrolCounter = 0;
                    int patrolShock = 0;
                    int patrolVoltage = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split by commas
                        string[] patrolData = line.Split(',');
                        DateTime date = new DateTime();

                        try
                        {
                            // Check if data comes in dd-mm-yyyy
                            string text = patrolData[3];
                            string patt = @"(\d){2}-(\d){2}-(\d){4}";
                            Regex regex = new Regex(patt, RegexOptions.IgnoreCase);
                            Match match = regex.Match(text);
                            if (match.ToString() == patrolData[3])
                            {
                                text = text.Replace('-', '/');
                            }

                            // Convert the date from dd/MM/yy to yyyy-MM-dd
                            string[] dateParams = text.Split('/');
                            date = new DateTime(
                                Convert.ToInt32(dateParams[2]),
                                Convert.ToInt32(dateParams[1]),
                                Convert.ToInt32(dateParams[0])
                            );
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Decrypt Test Failed " + ex.ToString());
                        }

                        // If patrolData[0] is 1, then it is a normal tag read
                        if (patrolData[0] == "1")
                        {
                            // Insert patrol data
                            insertPatrol.AddRowValues(new string[5] {

                                // MUST change this field in the Data() class if also adding to OLD databases,
                                // best to use MySQL's in-built last_insert_id() to get the other ID
                                importId.ToString(),
                                patrolData[2],
                                date.ToString("yyyy-MM-dd"),
                                patrolData[4],
                                patrolData[5]
                            });

                            // Increase patrol counter
                            patrolCounter++;

                            // Make sections of 1000 and store them into db
                            if (patrolCounter > 1000)
                            {
                                // Insert it
                                data.InsertPatrol(insertPatrol);
                                // Clear it
                                insertPatrol.SetRowValues(new List<string[]>());
                                // Reset the counter
                                patrolCounter = 0;
                            }

                        }

                        // If patrolData[o] is 2, then its a shock log
                        if (patrolData[0] == "2")
                        {
                            // Insert patrol data
                            insertShock.AddRowValues(new string[5] {

                                // MUST change this field in the Data() class if also adding to OLD databases,
                                // best to use MySQL's in-built last_insert_id() to get the other ID
                                importId.ToString(),
                                patrolData[2],
                                date.ToString("yyyy-MM-dd"),
                                patrolData[4],
                                patrolData[5]
                            });

                            // Increase patrol counter
                            patrolShock++;

                            // Make sections of 1000 and store them into db
                            if (patrolShock > 1000)
                            {
                                // Insert it
                                data.InsertShock(insertShock);

                                // Clear it
                                insertShock.SetRowValues(new List<string[]>());

                                // Reset the counter
                                patrolShock = 0;
                            }
                        }

                        // If patrolData[0] is 4, then the record is low voltage
                        if (patrolData[0] == "4")
                        {
                            // Get number
                            double battReading = Convert.ToDouble(patrolData[2]) / 1000;

                            // Insert patrol data
                            insertVoltage.AddRowValues(new string[5] {
                                importId.ToString(),
                                battReading.ToString(),
                                date.ToString("yyyy-MM-dd"),
                                patrolData[4],
                                patrolData[5]
                            });

                            // Increase patrol counter
                            patrolVoltage++;

                            // Make sections of 1000 and store them into db
                            if (patrolVoltage > 1000)
                            {
                                // Insert it
                                data.InsertVoltage(insertVoltage);

                                // Clear it
                                insertVoltage.SetRowValues(new List<string[]>());

                                // Reset the counter
                                patrolVoltage = 0;
                            }
                        }

                    }
                    // Close the reader
                    reader.Close();
                }

                // Insert left overs if necessary
                if (insertPatrol.GetAllValues().Count > 0) data.InsertPatrol(insertPatrol);
                if (insertShock.GetAllValues().Count > 0) data.InsertShock(insertShock);
                if (insertVoltage.GetAllValues().Count > 0) data.InsertVoltage(insertVoltage);

                // Initialise the InsertQuery method for UploadActivity
                InsertQuery insertUploadActivity = new InsertQuery();
                insertUploadActivity.SetTable("uploadactivity");
                insertUploadActivity.SetFields(new string[] { "account_id", "uplDate", "uplTime", "uplRecords", "uplLiveData" });
                // Add rows
                insertUploadActivity.AddRowValues(new string[] {
                    data.GetAccountIdForDatabase().ToString(),
                    currentDate,
                    currentTime,
                    totalRecords.ToString(),
                    liveData ? "1" : "0"
                });
                // Insert it
                data.InsertUploadActivity(insertUploadActivity);
            }
            catch (Exception ex)
            {
                Log.Warning("Attempted to decrypt file which does not exist: " + newFile.FullName + "\r\n" + ex.ToString());
                return false;
            }

            // Attempt to delete the file
            try
            {
                newFile.Delete();
            }
            catch (Exception ex)
            {
                Log.Error("Could not delete temporary file: " + newFile.FullName + "\r\n" + ex.ToString());
                return false;
            }

            return true;
        }

    }
}
