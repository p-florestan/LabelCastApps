using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using DataAccess;
using DataAccess.MSSQL;
using Microsoft.Data.SqlClient;

namespace LabelCast
{
    public class DbWrapperMSSQL : DbWrapperBase
    {
        #region Fields 

        dbMSSQL mssqlDB;
        String mConnectionString = "";

        #endregion

        #region Constructors
        public DbWrapperMSSQL(String connStr, String timeZone)
        {
            mssqlDB = new dbMSSQL();
            mConnectionString = connStr;
        }

        #endregion

        #region Public API - Overrides 

        /// <summary>
        /// Test database connection
        /// </summary>
        public override void TestConnection(String connectionString)
        {
            Logger.Write(Level.Debug, "MSSQL connection string: " + connectionString);

            var connection = new SqlConnection(connectionString);
            dbInfo info = mssqlDB.ExecuteQuery("select 1", connection);
            if (info.status != dbError.Ok)
                throw new DataException(info.errorText);                
        }

        /// <summary>
        /// Query for a single result row based on the specified query values.
        /// The result is returned as a string value dictionary matching LabelDescriptor.DbResultFields.
        /// </summary>
        public override Dictionary<String, String> QueryData(String sql, Dictionary<String, String> queryVars, Dictionary<String, String> resultVars)
        {
            String finalQuery = ReplaceQueryVariables(sql, queryVars);
            ValidateSqlSelect(finalQuery, resultVars);
            Logger.Write(Level.Debug, " - MSSQL.QueryData, final SQL query:\r\n" + finalQuery);

            dbInfo info = mssqlDB.ExecuteQuery(finalQuery, new SqlConnection(mConnectionString));
            if (info.status == dbError.Ok)
            {
                int cnt = info.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - MSSQL.QueryData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillReturnValues(resultVars, info.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - MSSQL.QueryData failed: " + info.errorText);
                throw new DataException(info.errorText);
            }
        }


        /// <summary>
        /// Query for a list of values based based on the specified query values (which typically
        /// involves a wildcard search).
        /// The result is returned as a list of string value dictionaries 
        /// (each matching LabelDescriptor.DbResultFields).
        /// </summary>
        public override List<Dictionary<String, String>> QueryListData(String sql, Dictionary<String, String> queryVars, Dictionary<String, String> resultVars)
        {
            String finalQuery = ReplaceQueryVariables(sql, queryVars);
            ValidateSqlSelect(finalQuery, resultVars);
            Logger.Write(Level.Debug, " - MSSQL.QueryListData, final SQL query:\r\n" + finalQuery);

            dbInfo info = mssqlDB.ExecuteQuery(finalQuery, new SqlConnection(mConnectionString));
            if (info.status == dbError.Ok)
            {
                int cnt = info.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - MSSQL.QueryListData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillOptionListValues(resultVars, info.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - MSSQL.QueryListData failed: " + info.errorText);
                throw new DataException(info.errorText);
            }
        }

        #endregion

    }
}
