using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Threading;

namespace UniGuardLib
{
    public class Database
    {
        // Mysql Server params
        private const string DBHOST = "localhost";
        private const string DBUSER = "ug12server";
        private const string DBPASS = "87S6KetBWUYqBfWt";
        private const string MAINDB = "ug12_maindb";
        MySqlConnectionStringBuilder conString = new MySqlConnectionStringBuilder();
        MySqlConnection con;

        public Database(string database = MAINDB)
        {
            this.conString.Database = database;
            this.conString.Server   = DBHOST;
            this.conString.UserID   = DBUSER;
            this.conString.Password = DBPASS;
            this.conString.Pooling  = true;
            this.conString.MaximumPoolSize = 100;
            this.conString.AllowUserVariables = true;

            con = new MySqlConnection(this.conString.ConnectionString);
        }

        public static string GetSchemaName(string schemaAlias)
        {
            string output = null;
            Database db   = new Database();
            string query  = String.Format("SELECT schCode FROM schemalocation WHERE schCode LIKE '%{0}%'", schemaAlias);

            // Initialize reader
            using (var reader = db.Query(query))
            while (reader.Read())
            {
                output = reader.GetValue(0).ToString();
            }

            return output;
        }

        /// <summary>
        /// Connects to database
        /// </summary>
        public MySqlConnection Connect()
        {
            if (con.State != ConnectionState.Open)
            {
                // Attempt connection
                try
                {
                    con.Open();
                }
                catch (Exception)
                {
                    Reconnect();
                }
            }

            return con;
        }

        /// <summary>
        /// Attempts to reconnect continuously every 1 second for 10 seconds
        /// </summary>
        public void Reconnect()
        {
            // Attempt to reconnect 10 times
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    con.Open();
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Returns the (highest) id column from selected table. It's a ghetto
        /// last_insert_id(), but in most cases it works.
        /// </summary>
        /// <param name="tableName">Table name to get the id from</param>
        /// <returns>Returns the MAX integer (id) value</returns>
        public int GetLastColumn(string tableName, string key)
        {
            string query = String.Format("SELECT MAX({0}) FROM `{1}`", key, tableName);
            int output = 0;

            using (var reader = Query(query))
            while (reader.Read())
            {
                output = Convert.ToInt32(reader.GetValue(0));
            }

            return output;
        }

        /// <summary>
        /// Utility method: It returns the account number for a particular database
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public int GetAccountFromDatabase(string databaseName)
        {
            int output = 0;
            string query = String.Format(@"
                SELECT  account.id
                FROM    account
                        LEFT JOIN schemalocation ON account.schemalocation_id = schemalocation.id
                WHERE schemalocation.schCode = '{0}'", databaseName);

            using (var reader = Query(query))
            while (reader.Read())
            {
                output = Convert.ToInt32(reader.GetValue(0));
            }

            return output;
        }

        /// <summary>
        /// Dynamic queries, not ideal but it works...
        /// </summary>
        /// <param name="queryString">Pass the full query here</param>
        /// <returns>Returns a MySqlDataReader object</returns>
        public IDataReader Query(string queryString)
        {
            MySqlDataReader reader;

            try
            {
                using (MySqlConnection con = Connect())
                using (MySqlCommand cmd = new MySqlCommand(queryString, con))
                {
                    cmd.CommandTimeout = 180;
                    reader = cmd.ExecuteReader();
                    var dt = new DataTable();
                    dt.Load(reader);

                    con.Close();
                    return dt.CreateDataReader();
                }
            }

            catch (Exception ex)
            {
                Log.Error(String.Format(
                    "Could not execute MySQL query for database ({0}):" + Environment.NewLine + "Query: {1}" + Environment.NewLine + "Reason: {2}",
                    this.conString.Database,
                    queryString,
                    ex.Message
                ));
            }

            return null;
        }

        public void Delete(string table, int id)
        {
            string query = String.Format("DELETE FROM `{0}` WHERE id = {1}", table, id);
            try
            {
                using (MySqlConnection con = Connect())
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                Log.Error(String.Format(
                    "Could not execute MySQL query:" + Environment.NewLine + "{0}" + Environment.NewLine + "{1}",
                    ex.ToString()
                ));
            }
        }

        /// <summary>
        /// Generalised method to insert into the database, it must be passed the
        /// table name as a first param and a Hashtable with key/value pairs with
        /// the row data.
        /// </summary>
        public int Insert(InsertQuery insert)
        {
            int output = 4;

            using (MySqlConnection con = Connect())
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    // Prepare query
                    cmd.Connection  = con;
                    cmd.CommandText = insert.GetQuery();
                    cmd.Prepare();

                    // Get values and fields
                    List<string[]> valuesList = insert.GetAllValues();
                    string[][] valuesArray    = valuesList.ToArray();
                    string[] fields           = insert.fields;

                    // Enter parameters
                    try
                    {
                        for (int i = 0; i < valuesArray.Length; ++i)
                        {
                            string[] values = valuesArray[i];
                            for (int x = 0; x < values.Length; ++x)
                                cmd.Parameters.AddWithValue("@" + fields[x] + i, values[x]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Database Error: \r\nQuery: " + insert.GetQuery() + "\r\n" + ex.ToString());
                    }

                    // Execute query
                    try
                    {
                        output = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Database Error: \r\nQuery: " + insert.GetQuery() + "\r\n" + ex.ToString());
                    }

                }

                con.Close();
            }

            return output;
        }

        public void Update(UpdateQuery update)
        {
            using (MySqlConnection con = Connect())
            using (MySqlCommand cmd    = new MySqlCommand())
            {
                // Prepare query
                cmd.Connection  = con;
                cmd.CommandText = update.GetQuery();
                cmd.Prepare();

                // Get values and fields
                List<string> valuesList = update.GetAllValues();
                string[] valuesArray    = valuesList.ToArray();
                string[] fields         = update.fields;

                // Enter parameters
                try
                {
                    for (int i = 0; i < valuesArray.Length; ++i)
                        cmd.Parameters.AddWithValue("@" + fields[i], valuesArray[i]);
                }
                catch (Exception ex)
                {
                    Log.Error("Database Error: \r\nQuery: " + update.GetQuery() + "\r\n" + ex.ToString());
                }

                // Execute query
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error("Database Error: \r\nQuery: " + update.GetQuery() + "\r\n" + ex.ToString());
                }

                con.Close();
            }
        }

    }

    public class InsertQuery
    {
        public string tableName;
        public string[] fields;
        List<string[]> values;

        /// <summary>
        /// Used to build Hashtables for dynamically ran queries. Used only for insert queries
        /// </summary>
        public InsertQuery()
        {
            // Initialise defaults
            this.values = new List<string[]>();
        }

        public int Count
        {
            get { return values.Count; }
        }

        public void SetTable(string tableName)
        {
            this.tableName = tableName;
        }

        public void SetFields(string[] fields)
        {
            this.fields = fields;
        }

        /// <summary>
        /// Sets all the rows to be prepared in query
        /// </summary>
        /// <param name="values">Rows to be inserted</param>
        public void AddRowValues(string[] values)
        {
            this.values.Add(values);
        }

        public void SetRowValues(List<string[]> valuesList)
        {
            this.values = valuesList;
        }

        /// <summary>
        /// Returns field sets for MySql in a string
        /// </summary>
        /// <returns>Ftrigified field sets</returns>
        private string FieldString()
        {
            return "(" + String.Join(", ", this.fields) + ")";
        }

        /// <summary>
        /// Stringified values for MySql in a string
        /// </summary>
        /// <returns></returns>
        private string ValuesString()
        {
            List<string> arrayOutput = new List<string>();
            int valueCount = this.values.Count;

            for (int i = 0; i < valueCount; ++i)
            {
                arrayOutput.Add("(");
                for (int x = 0; x < this.fields.Length; ++x)
                {
                    if (x != 0) arrayOutput.Add(", ");
                    arrayOutput.Add("@" + this.fields[x] + i);
                }
                arrayOutput.Add(i != (valueCount - 1) ? "), " : ");");
            }

            string[] queryOutput = arrayOutput.ToArray();

            return String.Join("", arrayOutput);
        }

        /// <summary>
        /// Builds the MySql query command and returns it
        /// </summary>
        public string GetQuery()
        {
            // Ensure that neither tables, fields or values are null
            if (this.tableName == null || this.values == null || this.fields == null)
                throw new ArgumentException("Insufficient parameters. Table, fields and values must be set.");

            // Start building the query
            return "INSERT INTO `" + this.tableName + "` " + this.FieldString() + " VALUES " + this.ValuesString();

        }

        /// <summary>
        /// Returns a List object with all the values
        /// </summary>
        /// <returns>All rows of data to be inserted</returns>
        public List<string[]> GetAllValues()
        {
            return this.values;
        }

    }

    public class UpdateQuery
    {
        public string tableName;
        public string id;
        public string[] fields;
        List<string> values;

        /// <summary>
        /// Used to build Hashtables for dynamically ran queries. Used only for insert queries
        /// </summary>
        public UpdateQuery()
        {
            // Initialise defaults
            this.values = new List<string>();
        }

        /// <summary>
        /// Sets the table in the object
        /// </summary>
        /// <param name="tableName"></param>
        public void SetTable(string tableName)
        {
            this.tableName = tableName;
        }

        /// <summary>
        /// Sets the fields to be updated in the object
        /// </summary>
        /// <param name="fields"></param>
        public void SetFields(string[] fields)
        {
            this.fields = fields;
        }

        /// <summary>
        /// Sets the Id for update
        /// </summary>
        /// <param name="id">Id number</param>
        public void SetId(string id)
        {
            this.id = id;
        }

        /// <summary>
        /// Sets all the rows to be prepared in query
        /// </summary>
        /// <param name="values">Rows to be inserted</param>
        public void AddRowValue(string values)
        {
            this.values.Add(values);
        }

        /// <summary>
        /// Set a row of values
        /// </summary>
        /// <param name="valuesList"></param>
        public void SetRowValues(List<string> valuesList)
        {
            this.values = valuesList;
        }

        /// <summary>
        /// Returns field sets for MySql in a string
        /// </summary>
        /// <returns>Ftrigified field sets</returns>
        private string FieldString()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < this.fields.Length; ++i)
            {
                list.Add(fields[i] + " = @" + fields[i]);
            }

            string[] output = list.ToArray();

            return String.Join(", ", output);
        }


        /// <summary>
        /// Builds the MySql query command and returns it
        /// </summary>
        public string GetQuery()
        {
            // Ensure that neither tables, fields or values are null
            if (this.tableName == null || this.values == null || this.fields == null)
                throw new ArgumentException("Insufficient parameters. Table, fields and values must be set.");

            // Start building the query
            return String.Format("UPDATE `" + this.tableName + "` SET " + this.FieldString() + " WHERE id = {0}", this.id);

        }

        /// <summary>
        /// Returns a List object with all the values
        /// </summary>
        /// <returns>All rows of data to be inserted</returns>
        public List<string> GetAllValues()
        {
            return this.values;
        }

    }
}
