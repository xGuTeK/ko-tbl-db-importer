﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoTblDbImporter.DataAccess.Connections;
using KoTblDbImporter.DataAccess.Connections.ODBC;
using KoTblDbImporter.Models;

namespace KoTblDbImporter.DataAccess.Factories
{
    public class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IDatabaseConnection GetDatabaseConnection(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.SqlServer:
                     return new OdbcDatabaseConnection();
                case DatabaseType.MySql:
                    // return new MySQLDatabaseConnection(); 
                    throw new NotImplementedException("MySQL database connection is not implemented yet.");
                default:
                    throw new ArgumentException("Invalid database type.");
            }
        }

    }
}
