using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelCast
{
    /// <summary>
    /// Base class wrapping database and commaon validation functionality.
    /// You must derive from this class for specific databases, such as MS SQL Server, Oracle etc.
    /// </summary>
    public class DbWrapperBase
    {
        #region Overrides 

        /// <summary>
        /// Test database connection
        /// </summary>
        public virtual void TestConnection(String connectionString)
        {
            // Must override in derived classes
            throw new NotImplementedException("DbWrapperBase.TestConnection(): you must override this method to use it.");
        }

        /// <summary>
        /// Query for a single result row based on the specified query values.
        /// The result is returned as a string value dictionary matching LabelDescriptor.DbResultFields.
        /// </summary>
        public virtual Dictionary<String, String> QueryData(String sql, Dictionary<String, String> queryVars, Dictionary<String, String> resultVars)
        {
            // Must override in derived classes
            throw new NotImplementedException("DbWrapperBase.QueryData(): you must override this method to use it.");
        }

        /// <summary>
        /// Query for a list of values based based on the specified query values (which typically
        /// involves a wildcard search).
        /// The result is returned as a list of string value dictionaries 
        /// (each matching LabelDescriptor.DbResultFields).
        /// </summary>
        public virtual List<Dictionary<String, String>> QueryListData(String sql, Dictionary<String, String> queryVars, Dictionary<String, String> resultVars)
        {
            // Must override in derived classes
            throw new NotImplementedException("DbWrapperBase.QueryListData(): you must override this method to use it.");
        }

        #endregion

        #region Common Internal Methods 

        /// <summary>
        /// Replaces the query placeholders in the SQL query string (marked by {} brackets)
        /// with the values from the 'dataVars' dictionary.
        /// </summary>
        internal String ReplaceQueryVariables(String sql, Dictionary<String, String> dataVars)
        {
            if (dataVars == null)
                throw new ArgumentNullException("Internal Error (OracleData.QueryData): value list is null.");

            ValidateSearchFields(sql, dataVars);

            // Turn everything uppercase for placeholder replacement:

            String finalSql = sql.ToUpper();
            foreach (var key in dataVars.Keys)
            {
                finalSql = finalSql.Replace("{" + key.ToUpper() + "}", dataVars[key]);
            }

            return finalSql;
        }


        /// <summary>
        /// Validating SQL query against SearchFields list.
        /// </summary>
        internal void ValidateSearchFields(String sqlQuery, Dictionary<String, String> dataVars)
        {
            // Preps - turn everything into uppercase 

            String sql = sqlQuery.ToUpper(); ;
            Logger.Write(Level.Debug, "Validating SQL query against SearchFields list. SQL Query:" + sql);

            Dictionary<String, String> queryVars = new Dictionary<string, string>();
            foreach (var key in dataVars.Keys)
            {
                queryVars.Add(key.ToUpper(), "");
            }

            // First ensure each query variable is found in the SQL

            foreach (var key in queryVars.Keys)
            {
                if (sql.ToUpper().IndexOf("{" + key.ToUpper() + "}") < 0)
                    throw new ArgumentException("Configuration Error: the field list does not match the placeholder variables in the SQL statement: field '" + key + "' not found in SQL");
            }

            // Then ensure all placeholder variables in the SQL are found in queryVars

            int varCount = 0, idxStart = 0, idxEnd = 0;

            idxStart = sql.IndexOf('{');

            if (idxStart < 0 && queryVars.Count > 0)
                throw new ArgumentException("Configuration Error: the field list does not match the placeholder variables in the SQL statement: no placeholders in the SQL but there are data variables to insert.");
            else
            {
                do
                {
                    sql = sql.Substring(idxStart);
                    idxEnd = sql.IndexOf('}');
                    if (idxEnd < 0)
                        throw new ArgumentException("Invalid placeholder syntax - missing closing bracket.");
                    else
                    {
                        varCount++;
                        String varName = sql.Substring(1, idxEnd - 1);
                        Logger.Write(Level.Debug, " - Variable name found: " + varName);
                        if (!queryVars.ContainsKey(varName))
                            throw new ArgumentException("Configuration Error: the field list does not match the placeholder variables in the SQL statement: field '" + varName + "'. found in SQL but not in variable list.");
                    }

                    sql = sql.Substring(idxEnd + 1);

                    idxStart = sql.IndexOf('{');
                    if (idxStart < 0)
                        break;
                }
                while (true);
            }
        }


        /// <summary>
        /// Validating SQL query against database result fields. 
        /// This ensures each one of the result fields appears in the SELECT part of the SQL.
        /// </summary>
        internal void ValidateSqlSelect(String sqlQuery, Dictionary<String, String> dataVars)
        {
            // We only do a weak test - does the name of the variable appear at all in the SQL?
            // This is due to potential complexities with subqueries, sub-selects etc.

            String sql = sqlQuery.ToUpper();
            Logger.Write(Level.Debug, "Validating SQL query against DataFields (result fields) list. SQL Query:" + sql);

            foreach (var key in dataVars.Keys)
            {
                if (sql.IndexOf(key.ToUpper()) < 0)
                    throw new ArgumentException("Configuration Error: the database-result-field list does not match SQL SELECT query: field '" + key + "' not found in SQL");
            }
        }


        /// <summary>
        /// Update the DbResultFields dictionary from the database result set.<br/>
        /// If no data was returned, throw an exception.
        /// </summary>
        internal Dictionary<String, String> FillReturnValues(Dictionary<String, String> dataVars, DataTable table)
        {
            if (table.Rows.Count == 0)
            {
                // This means invalid search key(s) submitted
                // Throw ArgumentException as it is invalid argument submission
                Logger.Write(Level.Debug, "Database result set is empty - DbResultFields not updated.");
                Logger.Write(Level.Debug, "No data found (to print a label we MUST find data - empty result sets won't work.");
                throw new ArgumentException("No data found.");
            }

            foreach (var key in dataVars.Keys)
            {
                if (table.Columns.Contains(key))
                    dataVars[key] = table.Rows[0][key].ToString() ?? "";
                else
                    throw new ApplicationException("Error: Column '" + key + "' mismatch between SQL and DataFields in profile");
            }
            return dataVars;
        }



        /// <summary>
        /// Update the DbResultFields dictionary from the database result set.<br/>
        /// If no data was returned, throw an exception.<br/>
        /// Verifies at the same time that each variable from dataVars (= dbResultFields) actually
        /// appears in the SQL result set.
        /// </summary>
        internal List<Dictionary<String, String>> FillOptionListValues(Dictionary<String, String> dataVars, DataTable table)
        {
            var optionList = new List<Dictionary<String, String>>();
            if (table.Rows.Count == 0)
                return optionList;

            for (int idx = 0; idx < table.Rows.Count; idx++)
            {
                var option = new Dictionary<String, String>(dataVars);
                foreach (var key in dataVars.Keys)
                {
                    if (table.Columns.Contains(key))
                        option[key] = table.Rows[idx][key].ToString() ?? "";
                    else
                        throw new ApplicationException("Error: Data-field '" + key + "' not part of SQL result (SQL query incorrect)");
                }
                optionList.Add(option);
            }

            return optionList;
        }


        #endregion

    }

}
