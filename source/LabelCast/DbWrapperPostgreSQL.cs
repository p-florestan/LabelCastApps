using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using DataAccess;
using Npgsql;

namespace LabelCast
{
    public class DbWrapperPostgreSQL : DbWrapperBase
    {
        #region Fields 

        String mConnectionString = "";

        #endregion

        #region Constructors
        public DbWrapperPostgreSQL(String connStr, String timeZone)
        {
            mConnectionString = connStr;
        }

        #endregion

        #region Public API - Overrides 

        /// <summary>
        /// Test database connection
        /// </summary>
        public override void TestConnection(String connectionString)
        {
            Logger.Write(Level.Debug, "SQLite connection string (path to DB): " + connectionString);
            dbInfo info = QueryPostgreSQL("select 1");
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
            Logger.Write(Level.Debug, " - SQLite.QueryData, final SQL query:\r\n" + finalQuery);

            dbInfo info = QueryPostgreSQL(finalQuery);
            if (info.status == dbError.Ok)
            {
                int cnt = info.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - SQLite.QueryData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillReturnValues(resultVars, info.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - SQLite.QueryData failed: " + info.errorText);
                throw new DataException("Database query failed: " + info.errorText);
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
            Logger.Write(Level.Debug, " - SQLite.QueryListData, final SQL query:\r\n" + finalQuery);

            dbInfo info = QueryPostgreSQL(finalQuery);
            if (info.status == dbError.Ok)
            {
                int cnt = info.queryResult.Rows.Count;
                Logger.Write(Level.Debug, " - SQLite.QueryListData succeeded (" + cnt + " row" + (cnt == 1 ? "" : "s") + " returned)");
                return FillOptionListValues(resultVars, info.queryResult);
            }
            else
            {
                Logger.Write(Level.Debug, " - SQLite.QueryListData failed: " + info.errorText);
                throw new DataException("Database query failed: " + info.errorText);
            }
        }

        #endregion

        #region Internal Methods 

        private dbInfo QueryPostgreSQL(String query)
        {
            dbInfo info = new dbInfo();

            using (NpgsqlConnection connection = new NpgsqlConnection(mConnectionString))
            {
                try
                {
                    connection.Open();
                    using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection))
                    {
                        dataAdapter.Fill(info.queryResult);
                        //
                        info.status = dbError.Ok;
                        info.recordsAffected = info.queryResult.Rows.Count;
                    }
                }
                catch (Exception ex)
                {
                    info.status = dbError.ErrorRetrievingRecords;
                    info.errorText = ex.Message;
                }
            }

            return info;
        }

        #endregion

    }
}
