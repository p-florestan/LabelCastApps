using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LabelCast
{
    /// <summary>
    /// Settings for client app
    /// </summary>
    public class Client
    {
        public String ActiveProfile { get; set; } = "";

        public String ActivePrinter { get; set; } = "";

        [JsonConverter(typeof(StringEnumConverter))]
        public Level LogLevel { get; set; } = Level.Notice;
    }
}
