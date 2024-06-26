﻿using System;
using System.Data.Common;
using System.Data;
using System.Data.Odbc;
using KoTblDbImporter.DataAccess.Connections;
using KoTblDbImporter.Utlis;
using System.Text;

namespace KoTblDbImporter.DataAccess.Connections.ODBC
{
    public class OdbcDatabaseConnection : IDatabaseConnection
    {
        private OdbcConnection _connection;

        public bool Connect(string server = "", string dbName = "", string username = null, string password = null)
        {
            try
            {
                string connectionString = $"Driver={{SQL Server}};Server={(string.IsNullOrEmpty(server) ? "localhost\\sqlexpress" : server)};Database={(string.IsNullOrEmpty(dbName) ? "kodb_tbl" : dbName)};";
                _connection = new OdbcConnection(connectionString);
                _connection.Open();
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Connected to the database using SqlServer.");
                Console.ResetColor();

                return true;
            }
            catch (OdbcException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error connecting to the database: {ex.Message}");
                Console.ResetColor();

                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();

                return false;
            }
        }
        public void Disconnect()
        {
            try
            {
                if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
                {
                    _connection.Close();
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Disconnected from the database using ODBC.");
                    Console.ResetColor();
                }
            }
            catch (OdbcException ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Error disconnecting from the database: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();
            }
        }

        public void ExecuteQuery(string sql, string comment, ConsoleColor color = ConsoleColor.Green)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (OdbcCommand command = new OdbcCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                    Console.ForegroundColor = color;
                    Console.WriteLine($"{comment}");
                    Console.ResetColor();
                }
            }
            catch (OdbcException ex)
            {
                
                if (ex.Errors.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    foreach (OdbcError error in ex.Errors)
                    {
                        Console.WriteLine($"SQL Error {error.SQLState}: {error.Message}");
                        
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ODBC Error: {ex.Message}");
                    Console.ResetColor();
                }
                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();
            }
        }

        public bool CreateDatabase(string databaseName)
        {
            string sql = $"CREATE DATABASE [{databaseName}]";
            try
            {
                using (OdbcCommand command = new OdbcCommand(sql, _connection))
                {
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (OdbcException ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Error creating database '{databaseName}': {ex.Message}");
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        public bool DatabaseExists(string databaseName)
        {
            string sql = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";

            using (OdbcCommand command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                int databaseCount = (int)command.ExecuteScalar();
                bool databaseExists = databaseCount > 0;

                return databaseExists;
            }
        }

        public bool DropAllTables(string databaseName)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                DataTable tables = _connection.GetSchema("Tables");
                ProgressBar progressBar = new ProgressBar(0, tables.Rows.Count-2, additionalInfo: "Processing... ");
                int i = 1;

                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();

                    if (tableName.StartsWith("sys") || tableName == "trace_xe_action_map" || tableName == "trace_xe_event_map")
                        continue;

                    string dropTableSql = $"DROP TABLE [{tableName}]";
                    ExecuteQuery(dropTableSql, $"Table '{tableName}' {i} of {tables.Rows.Count-2} was dropped successfully.");
                    progressBar.Update(i);
                    i++;
                }

                return true;
            }
            catch (OdbcException ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Error dropping tables: {ex.Message}");
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
        public bool TableVersionExists()
        {
            string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_VERSION'";

            try
            {
                using (OdbcCommand command = new OdbcCommand(sql, _connection))
                {
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (OdbcException ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Error checking if '_VERSION' table exists: {ex.Message}");
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
        public bool CreateVersionTable()
        {
            string sql = "CREATE TABLE _VERSION (VersionID INT, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP)";
            ExecuteQuery(sql, $"Successful creation of _VERSION table.");

            return true;
        }
        public bool CreateVersionEntry(int clientVersion)
        {
            string sql = $"INSERT INTO _VERSION (VersionID) VALUES ({clientVersion});";
            Console.WriteLine();
            ExecuteQuery(sql, $"The entry with client version has been created in the _VERSION table.");

            return true;
        }
        public DataTable GetVersionEntry()
        {
            string sql = "SELECT * FROM _VERSION";

            using (OdbcCommand command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                using (DbDataReader reader = command.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return dataTable;
                }
            }
        }

        public string MapDataTypeToString(Type dataType)
        {
            Dictionary<Type, string> typeMappings = new Dictionary<Type, string>
            {
                { typeof(int), "INT" },
                { typeof(string), "VARCHAR(MAX)" },
                { typeof(double), "FLOAT" },
                { typeof(sbyte), "SMALLINT" }, //Tinyint ?
                { typeof(byte), "SMALLINT" },
                { typeof(short), "SMALLINT" },
                { typeof(uint), "INT" },
                { typeof(float), "REAL" }
            };

            if (typeMappings.ContainsKey(dataType))
            {
                return typeMappings[dataType];
            }
            else
            {
                throw new NotSupportedException($"Data type {dataType} not supported.");
            }
        }

        public string GenerateCreateTableQuery(DataTable table, string tableName)
        {
            string createTableSQL = $"CREATE TABLE {tableName} (\n";

            foreach (DataColumn column in table.Columns)
            {
                string columnName = "col_" + column.ColumnName;
                Type dataType = column.DataType;
                string sqlType = MapDataTypeToString(dataType);

                createTableSQL += $"    {columnName} {sqlType},\n";
            }

            createTableSQL = createTableSQL.TrimEnd(',', '\n') + "\n);";

            return createTableSQL;
        }

        public string GenerateInsertQuery(string tableName, DataRow row)
        {
            StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");

            List<string> columns = new List<string>();
            List<string> values = new List<string>();

            foreach (DataColumn column in row.Table.Columns)
            {
                columns.Add($"col_{column.ColumnName}");

                var item = row[column];

                string value;
                if (item is string)
                {
                    value = $"'{((string)item).Replace("'", "''")}'";
                }
                else if (item is float || item is double || item is decimal)
                {
                    value = ((IFormattable)item).ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    value = item.ToString();
                }

                values.Add(value);
            }

            insertQuery.Append(string.Join(", ", columns));

            insertQuery.Append(") VALUES (");

            insertQuery.Append(string.Join(", ", values));

            insertQuery.Append(");");

            return insertQuery.ToString();
        }



    }
}