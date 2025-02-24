using DataAccess.Oracle;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LabelCast
{
    public class DbWrapperOracle : DbWrapperBase
    {
        #region Fields 

        private dbOracle oracleDB;

        #endregion

        #region Constructors
        public DbWrapperOracle(String connStr, String timeZone)
        {
            oracleDB = new dbOracle(connStr, timeZone);
        }

        #endregion

        #region Public API - Overrides

        /// <summary>
        /// Test database connection
        /// </summary>
        public override void TestConnection(String connectionString)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleInfo oInfo = oracleDB.OracleConnectWithTest(connection);
            if (oInfo.status != QueryStatus.OK)
                throw new DataException(oInfo.errorText);
        }


        /// <summary>
        /// Query for a single result row based on the specified query values.
        /// The result is returned as a string value dictionary matching LabelDescriptor.DbResultFields.
        /// </summary>
        public override Dictionary<String, String> QueryData(String sql, Dictionary<String, String> queryVars, Dictionary<String, String> resultVars)
        {
            String finalQuery = ReplaceQueryVariables(sql, queryVars);
            ValidateSqlSelect(finalQuery, resultVars);
            Logger.Write(Level.Debug, " - Oracle.QueryData, final SQL query:\r\n" + finalQuery);

            OracleInfo oInfo = oracleDB.OracleQuery(finalQuery);
            if (oInfo.status == QueryStatus.OK)
            {
                int cnt = oInfo.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - Oracle.QueryData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillReturnValues(resultVars, oInfo.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - Oracle.QueryData failed: " + oInfo.errorText);
                throw new DataException("Database query failed: " + oInfo.errorText);
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
            Logger.Write(Level.Debug, " - Oracle.QueryListData, final SQL query:\r\n" + finalQuery);

            OracleInfo oInfo = oracleDB.OracleQuery(finalQuery);
            if (oInfo.status == QueryStatus.OK)
            {
                int cnt = oInfo.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - Oracle.QueryListData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillOptionListValues(resultVars, oInfo.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - Oracle.QueryListData failed: " + oInfo.errorText);
                throw new DataException("Database query failed: " + oInfo.errorText);
            }
        }

        #endregion

    }
}
