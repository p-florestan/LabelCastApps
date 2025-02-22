using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Data;
using System.IO;

namespace LabelCast
{
    public static class UtilityExtensions
    {

        #region Number to String Conversions

        /// <summary>
        /// Convert string data to a nullable reference type
        /// </summary>
        public static Nullable<T> ToNullable<T>(this string s) where T : struct
        {
            Nullable<T> result = new Nullable<T>();
            try
            {
                if (!string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv.ConvertFrom(s);
                }
            }
            catch { }
            return result;
        }


        /// <summary>
        /// Convert string to Int64 (long) data type. If the string is empty or null, returns 0.
        /// </summary>
        public static Int64 CoerceToLong(this string s)
        {
            Int64.TryParse(s, out Int64 result);
            return result;
        }


        /// <summary>
        /// Convert string to Int32 data type. If the string is empty or null, returns 0.
        /// </summary>
        public static int CoerceToInt(this string s)
        {
            int.TryParse(s, out int result);
            return result;
        }


        /// <summary>
        /// Convert string to Int32 data type. If the string is empty or null, returns 0.00.
        /// </summary>
        public static decimal CoerceToDecimal(this string s)
        {
            decimal.TryParse(s, out decimal result);
            return result;
        }


        /// <summary>
        /// Convert a string to an integer, but when the value is "null" or empty, returns null.
        /// </summary>
        public static int? ToNullableInt(this string s)
        {
            if (String.IsNullOrWhiteSpace(s) || s.ToLower().Trim() == "null")
                return null;
            else
                return s.CoerceToInt();
        }


        #endregion

        #region String Utilities

        /// <summary>
        /// Return leftmost 'n' characters of string 's', even if the string is shorter than 'n' chars.
        /// </summary>
        public static String Left(this String s, int n)
        {
            return s.Length < n ? s : s.Substring(0, n);
        }

        /// <summary>
        /// Return rightmost 'n' characters of string 's', even if the string is shorter than 'n' chars.
        /// </summary>
        public static String Right(this String s, int n)
        {
            return s.Length < n ? s : s.Substring(s.Length - n);
        }

        /// <summary>
        /// Verifies if the string is numeric integer, i.e. can be converted to an Int64.
        /// </summary>
        public static Boolean IsInteger(this String? s)
        {
            return Int64.TryParse(s, out long number);
        }

        #endregion

        #region DateTime Utilities

        /// <summary>
        /// Truncate DateTime to a precicion indicated by the TimeSpan parameter.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            // Source
            // https://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime

            if (timeSpan == TimeSpan.Zero)
                return dateTime;
            // do not modify "guard" values:
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
                return dateTime;
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }


        /// <summary>
        /// Truncate DateTime to full seconds (no millisec, no microsec etc)
        /// </summary>
        public static DateTime ToFullSeconds(this DateTime dateTime)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(1);
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
                return dateTime;
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        /// <summary>
        /// Truncate DateTime to full seconds (no millisec, no microsec etc)
        /// </summary>
        public static DateTime? ToFullSeconds(this DateTime? dateTime)
        {
            if (dateTime == null)
                return null;
            else
            {
                DateTime dt = (DateTime)dateTime;
                TimeSpan timeSpan = TimeSpan.FromSeconds(1);
                if (dt == DateTime.MinValue || dt == DateTime.MaxValue)
                    return dt;
                return dt.AddTicks(-(dt.Ticks % timeSpan.Ticks));
            }
        }

        /// <summary>
        /// Converts a DateTime object to ISO-8601 compliant date-time string (including milliseconds).
        /// </summary>
        public static String ToISO8601Date(this DateTime dt)
        {
            return ((DateTime)dt).ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }

        /// <summary>
        /// Converts a DateTime object to ISO-8601 compliant date-time string (including milliseconds).
        /// </summary>
        public static String ToISO8601Date(this DateTime? dt)
        {
            if (dt != null)
                return ((DateTime)dt).ToString("yyyy-MM-ddTHH:mm:ss.fff");
            else
                return "";
        }

        #endregion 

        #region Files and Paths

        /// <summary>
        /// Reads content of a text file into a string. Invoke this method on the file name.
        /// </summary>
        /// <param name="fileName">Name (incl full path) of file to read.</param>
        /// <returns></returns>
        public static String ReadToString(this String fileName)
        {
            String str = "";
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    str = sr.ReadToEnd();
                    sr.Close();
                }
                fs.Close();
            }
            return str;
        }


        /// <summary>
        /// Saves text string to the specified file name. Invoke this on the text to be saved.
        /// If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="content">Text string to be saved to file</param>
        /// <param name="fileName">File name (incl full path) to save to</param>
        public static void SaveToFile(this String content, String fileName)
        {
            // note - overwrites the file if a file with the same name already exists!
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                    sw.Flush();
                    sw.Close();
                }
                fs.Close();
            }
        }


        /// <summary>
        /// Concatenates 'folder' with 'pathSection', ensuring there is a proper directory 
        /// delimiter between folder and pathSection. The string 'pathSection' can be a folder
        /// or a file name.
        /// </summary>
        /// <param name="folder">Directory path (with or without trailing backslash</param>
        /// <param name="pathSection">Path section (folder or file name) to append</param>
        /// <returns></returns>
        public static String AddPath(this String folder, String pathSection)
        {
            folder = folder.Trim();
            if (folder.EndsWith(@"\"))
                return folder + pathSection.Trim().TrimStart('\\');
            else
                return folder + @"\" + pathSection.Trim().TrimStart('\\');
        }


        /// <summary>
        /// Converts a string to "0" if empty
        /// </summary>
        public static String ZeroIfEmpty(this String s)
        {
            return String.IsNullOrEmpty(s) ? "0" : s;
        }

        #endregion

        #region DataTable Converters

        // This is primarily used by export classes, converting data-table to strings.

        /// <summary>
        /// Appends content of DataTable cells (as strings) to a string builder.
        /// </summary>
        public static StringBuilder TableToString(this StringBuilder sb, DataTable dt)
        {
            int colCount = dt.Columns.Count;
            foreach (DataRow row in dt.Rows)
            {
                for (int col = 0; col < colCount; col++)
                {
                    sb.Append(row[col].ToString());
                    //if (col < colCount - 1)
                    sb.Append('|');
                }
                sb.Append("\r\n");
            }
            return sb;
        }

        #endregion

        #region Type Names

        public static string GetTypeName(this object obj)
        {
            Type type = obj.GetType();

            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetTypeName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        #endregion

    }
}

