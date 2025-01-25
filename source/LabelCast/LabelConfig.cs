using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Drawing.Text;


namespace LabelCast
{
    public class LabelConfig
    {

        #region Fields 

        // Currently hardcoded 
        String mConfigPath = @"c:\Program Info\LabelCast\";
        
        Profile? mActiveProfile = null;

        Printer? mActivePrinter = null;

        // This is not currently used.
        // String mDefaultAdminCode = "9750";

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public LabelConfig() 
        {
            if (!Directory.Exists(mConfigPath))
                Directory.CreateDirectory(mConfigPath);
        }

        /// <summary>
        /// Constructor for setting a custom configuration file path.
        /// The default is "c:\Program Info\LabelCast\".
        /// </summary>
        public LabelConfig(String configPath) 
        {
            mConfigPath = configPath;
            if (!Directory.Exists(mConfigPath))
                Directory.CreateDirectory(mConfigPath);
        }

        #endregion

        #region Public Properties 

        /// <summary>
        /// Path to configuration file directory.
        /// </summary>
        public String ConfigDir { get { return mConfigPath; } }

        /// <summary>
        /// List of profiles defined in configuration files.
        /// </summary>
        public List<Profile> ProfileList { get; set; } = new List<Profile>();

        /// <summary>
        /// Configuration for this client desktop application.
        /// </summary>
        public Client ClientConf { get; set; } = new Client();

        /// <summary>
        /// Currently selected (active) profile. The settings in this profile
        /// will be used for label entry and printing.
        /// </summary>
        public Profile? ActiveProfile 
        {
            get  { return mActiveProfile; }           
        }

        /// <summary>
        /// Currently selected (active) printer. This may or may not be the same
        /// as the default printer defined in the active profile.
        /// </summary>
        public Printer? ActivePrinter
        {
            get { return mActivePrinter; }
        }

        /// <summary>
        /// Determine if the configuration is locked for editing.
        /// This works by checking the existence of the file "config.lock" in the config dir.
        /// </summary>
        public bool IsLocked
        {
            get { return File.Exists(Path.Join(mConfigPath, "config.lock")); }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Read all configuration profiles from files on disk.
        /// </summary>
        public void ReadConfiguration()
        {
            bool noConfig = false;

            // 1. Read printer configuration from JSON file

            Logger.Write(Level.Debug, "Reading printer configuration");

            String printerConf = Path.Join(mConfigPath, "Printers.json");
            if (File.Exists(printerConf))
            {
                String content = printerConf.ReadToString();
                List<Printer> printerList = JsonConvert.DeserializeObject<List<Printer>>(content) ?? new List<Printer>();
                PrinterStore.Printers.Clear();
                PrinterStore.SetPrinters(printerList);
            }
            else
            {
                Logger.Write(Level.Debug, "Printer configuration file '" + printerConf + "' not found. Creating default printer.");
            }
            if (PrinterStore.Printers.Count == 0)
            {
                noConfig = true;
                PrinterStore.AddPrinter(GetDemoPrinter());
            }

            Logger.Write(Level.Debug, " > Found " + PrinterStore.Printers.Count + " printer" + (PrinterStore.Printers.Count != 1 ? "s" : ""));

            
            // 2. Read all profile JSON files 

            Logger.Write(Level.Debug, "Reading profile configuration");

            ProfileList.Clear();
            String[] configFiles = Directory.GetFiles(mConfigPath, "Profile*.json");
            if (configFiles.Length == 0)
            {
                noConfig = true;
                ProfileList.Add(GetDemoProfile());
            }
            else
            {
                foreach (String filePath in configFiles)
                {
                    String content = filePath.ReadToString();
                    Profile? p = JsonConvert.DeserializeObject<Profile>(content);
                    if (p != null)
                    {
                        ProfileList.Add(p);
                    }
                }
            }

            Logger.Write(Level.Debug, " > Found " + ProfileList.Count + " profile" + (ProfileList.Count != 1 ? "s" : ""));

            
            // 3. Read client configuration from JSON file

            Logger.Write(Level.Debug, "Reading client configuration");

            String conf = Path.Join(mConfigPath, "Client.json");
            if (File.Exists(conf))
            {
                String content = conf.ReadToString();
                ClientConf = JsonConvert.DeserializeObject<Client>(content) ?? new Client();
            }

            
            // Active  profile 

            if (String.IsNullOrWhiteSpace(ClientConf.ActiveProfile))
                ClientConf.ActiveProfile = ProfileList.FirstOrDefault()?.Abbreviation ?? "";

            mActiveProfile = ProfileList.FirstOrDefault(p => p.Abbreviation == ClientConf.ActiveProfile);
            if (mActiveProfile == null)
                mActiveProfile = ProfileList.FirstOrDefault();

            
            // Active printer 

            if (String.IsNullOrWhiteSpace(ClientConf.ActivePrinter))
                ClientConf.ActivePrinter = PrinterStore.Printers.FirstOrDefault()?.Name ?? "";

            mActivePrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name == ClientConf.ActivePrinter);
            if (mActivePrinter == null)
                mActivePrinter = PrinterStore.Printers.FirstOrDefault();

            Logger.Write(Level.Debug, " > Active profile abbreviation: '" + (mActiveProfile != null ? mActiveProfile.Abbreviation : "(none)") + "'");
            Logger.Write(Level.Debug, " > Active printer name: '" + (mActivePrinter != null ? mActivePrinter.Name : "(none)") + "'");

            if (noConfig)
            {
                CreateDemoTemplate(mActiveProfile);
                SaveConfiguration();
            }

            
            // Set current log level 

            Logger.CurrentLogLevel = ClientConf.LogLevel;

        }


        /// <summary>
        /// Save entire configuration to disk - this includes both profiles and
        /// printers. They should not be saved separately because they are interdependent.<br/>
        /// This saves the current values of 'ProfileList' and 'PrinterStore.Printers' to disk.<br/>
        /// You must ensure these properties are up-to-date before invoking SaveConfiguration().
        /// </summary>
        public String SaveConfiguration()
        {
            String validateResult = ValidateConfiguration();
            if (!String.IsNullOrWhiteSpace(validateResult))
                return validateResult;

            try
            {
                SaveAllProfiles();
                SavePrinters();
                SaveClientConfig();
            }
            catch(Exception ex)
            {
                return ex.Message;
            }

            return "";
        }

        /// <summary>
        /// Sets ActiveProfile to the one matching the supplied abbreviation, and saves 
        /// the active profile information to disk.<br/>
        /// Same for active printer.<br/>
        /// (Does not save other info.)
        /// </summary>
        public string SaveActiveProfileAndPrinter(string activeProfileAbbr, string activePrinterName)
        {
            try
            {
                // Active Profile

                Profile? newProfile = ProfileList.FirstOrDefault(p => p.Abbreviation == activeProfileAbbr);
                if (newProfile == null)
                    throw new ArgumentException("Cannot set profile '" + activeProfileAbbr + "' as active profile because it does not exist.");
                mActiveProfile = newProfile;
                ClientConf.ActiveProfile = activeProfileAbbr;

                // Active Printer

                Printer? newPrinter = PrinterStore.Printers.FirstOrDefault(p => p.Name == activePrinterName);
                if (newPrinter == null)
                    throw new ArgumentException("Cannot set printer '" + activePrinterName + "' as active printer because it does not exist.");
                mActivePrinter = newPrinter;
                ClientConf.ActivePrinter = activePrinterName;

                SaveClientConfig();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "";
        }

        #endregion

        #region Internal Helper Methods

        /// <summary>
        /// Validate configuration and return error message upon failure.<br/>
        /// An empty string is returned if the validation passed the tests.
        /// </summary>
        private String ValidateConfiguration()
        {
            String msgResult = "";

            if (!ProfileNamesUnique())
                msgResult = "Cannot save configuration - profile names must be unique.";
            else if (!ProfileAbbrevsUnique())
                msgResult = "Cannot save configuration - profile abbreviations must be unique.";
            else if (!PrinterNamesUnique())
                msgResult = "Cannot save configuration - printer names must be unique.";
            else
            {
                msgResult = VerifyLabelTemplates();
                if (!String.IsNullOrEmpty(msgResult))
                    return msgResult;

                UpdateAPIConfiguration();
            }

            return msgResult;
        }


        private void SaveAllProfiles()
        {
            // We save profiles to a TEMP dir ...
            String subdir = Path.Join(mConfigPath, "temp");
            if (!Directory.Exists(subdir))
                Directory.CreateDirectory(subdir);

            foreach (Profile p in this.ProfileList)
            {
                SaveSingleProfile(p);
            }

            // .. and only when that succeeded, remove existing disk files and move
            String[] fileList = Directory.GetFiles(mConfigPath, "Profile*.json");
            foreach (String file in fileList)
            {
                File.Delete(file);
            }
            String[] newFiles = Directory.GetFiles(Path.Join(mConfigPath, "temp"), "Profile*.json");
            foreach (String file in newFiles)
            {
                String name = Path.GetFileName(file);
                File.Move(file, Path.Join(mConfigPath, name));
            }
        }

        private void SaveSingleProfile(Profile p)
        {
            String json = JsonConvert.SerializeObject(p, Formatting.Indented);
            // We save to "temp" subdir first:
            String fileName = Path.Join(mConfigPath, @"temp\Profile-" + p.Name.Trim().Replace(" ", "_") + ".json");
            json.SaveToFile(fileName);
        }

        private void SavePrinters()
        {
            String json = JsonConvert.SerializeObject(PrinterStore.Printers, Formatting.Indented);
            String fileName = Path.Join(mConfigPath, "Printers.json");
            json.SaveToFile(fileName);

        }

        private void SaveClientConfig()
        {
            String json = JsonConvert.SerializeObject(ClientConf, Formatting.Indented);
            String fileName = Path.Join(mConfigPath, "Client.json");
            json.SaveToFile(fileName);
        }


        private bool ProfileNamesUnique()
        {
            bool IsUnique = true;
            for (int n = 0; n < ProfileList.Count; n++)
            {
                for (int i = 0; i > n && i < ProfileList.Count; i++)
                {
                    if (ProfileList[n].Name.ToLower().Trim() == ProfileList[i].Name.ToLower().Trim())
                    {
                        IsUnique = false; break;
                    }
                }
            }
            return IsUnique;
        }

        private bool ProfileAbbrevsUnique()
        {
            bool IsUnique = true;
            for (int n = 0; n < ProfileList.Count; n++)
            {
                for (int i = 0; i > n && i < ProfileList.Count; i++)
                {
                    if (ProfileList[n].Abbreviation.ToLower().Trim() == ProfileList[i].Abbreviation.ToLower().Trim())
                    {
                        IsUnique = false; break;
                    }
                }
            }
            return IsUnique;
        }

        private bool PrinterNamesUnique()
        {
            bool IsUnique = true;
            for (int n = 0; n < PrinterStore.Printers.Count; n++)
            {
                for (int i = 0; i > n && i < PrinterStore.Printers.Count; i++)
                {
                    if (PrinterStore.Printers[n].Name.ToLower().Trim() == PrinterStore.Printers[i].Name.ToLower().Trim())
                    {
                        IsUnique = false; break;
                    }
                }
            }
            return IsUnique;
        }

        private String VerifyLabelTemplates()
        {
            String msgResult = "";
            foreach (Profile p in ProfileList)
            {
                if (String.IsNullOrWhiteSpace(p.LabelTemplate))
                    msgResult += "LabelTemplate for profile '" + p.Name + "' is empty.\r\n";
                else
                {
                    try
                    {
                        String t = p.LabelTemplate.ReadToString();
                    }
                    catch (Exception ex)
                    {
                        msgResult += "Cannot read LabelTemplate file for profile '" + p.Name + "'. Error: " + ex.Message + "\r\n";
                    }
                }
            }

            return msgResult;
        }

        /// <summary>
        /// Update the API configuration properties with the field names which are
        /// configured for that profile. <br/>
        /// This makes it easier to fill them out, especially as they are somewhat
        /// confusing, being split by SearchField / DataField and EditFields mapping props.<br/>
        /// Deleting a field name also result in deleting the field map prop, helping to
        /// ensure integrity of the configuration.
        /// </summary>
        private void UpdateAPIConfiguration()
        {
            // (1) Adding keys to mapping properties if they are found in field props

            foreach (Profile p in ProfileList)
            {
                if (p.SearchFields.Count > 0)
                {
                    foreach (String field in p.SearchFields)
                    {
                        p.JSONSearchFieldMap.TryAdd(field, "");
                        p.XMLSearchFieldMap.TryAdd(field, "");
                    }
                }

                if (p.DataFields.Count > 0)
                {
                    foreach (String field in p.DataFields)
                    {
                        p.JSONDataFieldMap.TryAdd(field, "");
                        p.XMLDataFieldMap.TryAdd(field, "");
                    }
                }

                if (p.EditableFields.Count > 0)
                {
                    foreach (String field in p.EditableFields)
                    {
                        p.JSONEditFieldMap.TryAdd(field, "");
                        p.XMLEditFieldMap.TryAdd(field, "");
                    }
                }

                // (2) Removing key-value-pairs from mapping props if not found in field props

                List<String> removeList;

                // Search Fields
                removeList = p.JSONSearchFieldMap.Keys.Where(key => !p.SearchFields.Contains(key)).ToList();
                RemoveKeys(p.JSONSearchFieldMap, removeList);
                removeList = p.XMLSearchFieldMap.Keys.Where(key => !p.SearchFields.Contains(key)).ToList();
                RemoveKeys(p.XMLSearchFieldMap, removeList);

                // Data Fields
                removeList = p.JSONDataFieldMap.Keys.Where(key => !p.DataFields.Contains(key)).ToList();
                foreach (String key in removeList)
                    RemoveKeys(p.JSONDataFieldMap, removeList);
                removeList = p.XMLDataFieldMap.Keys.Where(key => !p.DataFields.Contains(key)).ToList();
                RemoveKeys(p.XMLDataFieldMap, removeList);

                // Editable Fields
                removeList = p.JSONEditFieldMap.Keys.Where(key => !p.EditableFields.Contains(key)).ToList();
                RemoveKeys(p.JSONEditFieldMap, removeList);
                removeList = p.XMLEditFieldMap.Keys.Where(key => !p.EditableFields.Contains(key)).ToList();
                RemoveKeys(p.XMLEditFieldMap, removeList);
            }
        }

        /// <summary>
        /// Remove key-value pairs from a string dictionary based on a list of keys to remove.
        /// </summary>
        private void RemoveKeys(Dictionary<String, String> dictionary, List<String> keysToRemoveList)
        {
            foreach (String key in keysToRemoveList)
            {
                dictionary.Remove(key);
            }
        }

        #endregion

        #region Sample and Demo Configuration Data 

        // This mainly exists to ensure the app will never crash even if 
        // incorrectly installed or all configuration removed by error

        /// <summary>
        /// Create a demo profile which can be used as a sample on the first startup
        /// of this application. It is generated when no configuration file is found,
        /// and it is using Oracle as data provider.
        /// </summary>
        private Profile GetDemoProfile()
        {
            Profile profile = new Profile
            {
                Name = "Demo Profile",
                Abbreviation = "DEMO",
                Description = "Demonstration Profile",
                //
                DatabaseType = DBType.SQLite,
                DbConnectionString = @"Data Source=c:\Program Info\LabelCast\demo-sqlite.db;Version=3;",
                DbTimeZone = "UTC",
                SqlQuery = "SELECT description, code, price FROM Flowers WHERE name = '{name}' LIMIT 1",
                SearchSqlQuery = "SELECT description, code, price FROM Flowers WHERE code LIKE '{name}'",
                //
                SearchFields = { "Name" },
                DataFields = { "Description", "Code", "Price" },
                EditableFields = { "Price" },
                //
                LabelTemplate = "",
                DefaultPrinter = PrinterStore.Printers.FirstOrDefault()?.Name
            };

            return profile;
        }


        /// <summary>
        /// Create a demo printer which can be used as a sample on the first startup
        /// of this application. It is generated when no printer is configured.
        /// </summary>
        private Printer GetDemoPrinter()
        {
            Printer printer = new Printer
            {
                Name = "Demo Printer",
                Description = "Sample printer for demo purposes",
                IPAddress = IPAddress.Parse("0.0.0.0"),
                Port = 9100
            };

            return printer;
        }


        /// <summary>
        /// Create demo template
        /// </summary>
        private void CreateDemoTemplate(Profile? p)
        {
            if (p == null)
                return; 

            String template = @"
CT~~CD,~CC^~CT~
^XA
~TA000
~JSN
^LT0
^MNW
^MTT
^PON
^PMN
^LH0,0
^JMA
^PR4,4
~SD30
^JUS
^LRN
^CI27
^PA0,1,1,0
^XZ
^XA
^MMT
^PW735
^LL456
^LS0
^FPH,1^FT35,284^A0N,37,38^FH\^CI28^FDPrice: $^FS^CI27
^FPH,2^FO35,118^A0N,36,35^FB658,3,9,L^FH\^CI28^FDDescription^FS^CI27
^FT209,284^A0N,37,38^FH\^CI28^FDPrice^FS^CI27
^BY4,3,112^FT113,416^BCN,,N,N,,A
^FDCode^FS
^FO46,424^AFN,26,13^FB638,1,0,C^FH\^FDCode^FS
^PQ1,,,Y
^XZ
";
            String path = Path.Combine(ConfigDir, "Demo-Template.prn");
            template.SaveToFile(path);
            p.LabelTemplate = path;
        }

        #endregion

}


#region Custom JSON Converter Classes 

/// <summary>
/// Converter for IPAddress object - JsonConvert does not understand how to do that by default.
/// (see https://stackoverflow.com/questions/18668617/json-net-error-getting-value-from-scopeid-on-system-net-ipaddress)
/// </summary>
public class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // prior to NetCore 3.0:
            //    return (objectType == typeof(IPAddress));
            // NetCore 3.0 and later:
            return typeof(IPAddress).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteValue("");
            else
                writer.WriteValue(value.ToString());
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || String.IsNullOrWhiteSpace((string)reader.Value))
                return null;
            else
                return IPAddress.Parse((string)reader.Value);
        }
    }

    #endregion


}
