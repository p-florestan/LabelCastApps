using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using Namotion.Reflection;


namespace LabelCast
{
    /// <summary>
    /// Processing external requests for printing labels.<br/>
    /// Validates the request, matches it to a profile and printer and crates
    /// a LabelDescriptor with the label data.
    /// </summary>
    public class RequestHandler
    {

        #region Fields 

        String mRequest = "";

        String mSchemaDir = "";
        Boolean mSchemaValidated = false;
        String mValidationErrors = ""; 

        LabelContentType mContentType = LabelContentType.JSON;
        Dictionary<String, String>? mJSONContent;
        XPathDocument? mXMLContent;

        Profile mProfile;
        Printer mPrinter;
        LabelDescriptor mDescriptor;

        Boolean mRequireDbQuery = false;

        #endregion

        #region Constructors

        public RequestHandler(String? request, String schemaDir, List<Profile> profileList, List<Printer> printerList, bool requireDbQuery = false)
        {
            if (String.IsNullOrWhiteSpace(request)) throw new ArgumentException("RequestHandler: Empty label request");
            if (String.IsNullOrWhiteSpace(schemaDir)) throw new ArgumentException("RequestHandler: SchemaDir parameter cannot be empty.");
            if (profileList == null) throw new ArgumentException("RequestHandler: ProfileList parameter cannot be null.");
            if (printerList == null) throw new ArgumentException("RequestHandler: PrinterList parameter cannot be null.");
            mRequest = request.Trim();
            mSchemaDir = schemaDir;
            mRequireDbQuery = requireDbQuery;

            mContentType = DetermineContentType();
            CaptureContent();
            mProfile = MatchProfile(profileList);
            ValidateSchema();

            mDescriptor = FillLabelDescriptor();
            mPrinter = DeterminePrinter(printerList);
        }

        #endregion

        #region Public Properties 

        /// <summary>
        /// Format of label request content (XML or JSON).
        /// </summary>
        public LabelContentType ContentType { get { return mContentType; } }

        /// <summary>
        /// Profile matched to the external label request.
        /// </summary>
        public Profile RequestProfile { get { return mProfile; } }

        public Boolean IsSchemaValidated { get { return mSchemaValidated; } }

        /// <summary>
        /// LabelDescriptor filled out with data from the the external label request.
        /// </summary>
        public LabelDescriptor Descriptor { get { return mDescriptor; } }

        /// <summary>
        /// Printer matched to the external label request. If not matched, the default
        /// printer defined in the profile will be used.
        /// </summary>
        public Printer RequestPrinter { get { return mPrinter; } }

        #endregion



        #region Internal Methods - ContentType

        /// <summary>
        /// Check whether request format is XML or JSON.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private LabelContentType DetermineContentType()
        {
            LabelContentType type = LabelContentType.JSON;
            if (mRequest.StartsWith("<?xml"))
            {
                Logger.Write(Level.Debug, "Label request format: XML");
                type = LabelContentType.XML;
            }
            else if (mRequest.StartsWith('{') && mRequest.EndsWith('}'))
            {
                Logger.Write(Level.Debug, "Label request format: JSON");
                type = LabelContentType.JSON;
            }
            else
                throw new ArgumentException("Label request format not supported. " +
                    "Required is either XML (with standard declaration) or JSON " +
                    "(single object, not an array).");

            return type;
        }

        #endregion

        #region Internal Methods - Capturing Content

        /// <summary>
        /// Read and convert the JSON/XML string of the request into an object
        /// for further processing.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private void CaptureContent()
        {
            Logger.Write(Level.Debug, "Label Request received:\r\n" + mRequest);

            if (mContentType == LabelContentType.JSON)
            {
                mJSONContent = JsonConvert.DeserializeObject<Dictionary<String, String>>(mRequest);
                if (mJSONContent == null)
                    throw new ArgumentException("Invalid JSON request - cannot match to label profile. " +
                        "Ensure the JSON request contains a single JSON object with no nested objects or arrays.");
            }
            else
            {
                mXMLContent = new XPathDocument(new MemoryStream(Encoding.UTF8.GetBytes(mRequest)));
                if (mXMLContent == null)
                    throw new ArgumentException("Invalid XML request. Ensure it contains well-formed XML.");
            }
        }

        #endregion

        #region Internal Methods - Matching Profile

        // Find the profile matching label content, if any.
        private Profile MatchProfile(List<Profile> profileList)
        {
            Profile? matchedProfile = null;

            if (mContentType == LabelContentType.JSON)
                matchedProfile = MatchProfileJSON(profileList);
            else
                matchedProfile = MatchProfileXML(profileList);

            if (matchedProfile == null)
                throw new ArgumentException("Cannot match request to a label profile.");

            return matchedProfile;
        }


        /// <summary>
        /// Find the profile matching JSON label content.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private Profile? MatchProfileJSON(List<Profile> profileList)
        {
            if (mJSONContent == null)
                throw new ArgumentException("Invalid JSON request");

            Profile? matchedProfile = null;

            List<Profile> MappedProfiles = new List<Profile>();
            foreach (Profile p in profileList)
            {
                Logger.Write(Level.Debug, "Checking ProfileMap (JSON) in profile '" + p.Abbreviation + "'");

                bool IsMatched = true;
                foreach (var prop in p.JSONProfileMap.Keys)
                {
                    if (mJSONContent.ContainsKey(prop))
                    {
                        if (mJSONContent[prop] != p.JSONProfileMap[prop])
                        {
                            Logger.Write(Level.Debug, " * Request contains property '" + prop + "' but value does not match.");
                            IsMatched = false;
                            break;
                        }
                    }
                    else
                    {
                        Logger.Write(Level.Debug, " * Request does not contain property '" + prop + "' - not matched.");
                        IsMatched = false;
                        break;
                    }
                }

                if (IsMatched)
                {
                    MappedProfiles.Add(p);
                    Logger.Write(Level.Debug, "Successfully matched to profile '" + p.Abbreviation + "'");
                }
            }

            matchedProfile = FindMostSpecificProfile(MappedProfiles);
            return matchedProfile;
        }


        /// <summary>
        /// Find the profile matching XML label content.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private Profile? MatchProfileXML(List<Profile> profileList)
        {
            if (mXMLContent == null)
                throw new ArgumentException("Invalid XML request");

            Profile? matchedProfile = null;

            XPathNavigator nav = mXMLContent.CreateNavigator();

            List<Profile> MappedProfiles = new List<Profile>();
            foreach (Profile p in profileList)
            {
                Logger.Write(Level.Debug, "Checking ProfileMap (XML) in profile '" + p.Abbreviation + "'");

                bool IsMatched = false;
                foreach (var key in p.XMLProfileMap.Keys)
                {
                    IsMatched = false;
                    var xmlValue = nav.Evaluate(key);
                    XPathNodeIterator iterator = (XPathNodeIterator)xmlValue;
                    if (iterator != null)
                    {
                        if (iterator.MoveNext() && iterator.Current != null)
                        {
                            string strValue = iterator.Current.Value;
                            if (strValue == p.XMLProfileMap[key])
                                IsMatched = true;
                            else
                                Logger.Write(Level.Debug, " * Request contains key '" + key + "' but value '" + strValue + "' does not match (expected '" + p.XMLProfileMap[key] + "').");
                        }
                        else
                            Logger.Write(Level.Debug, " * Request does not contain required key '" + key + "'");
                    }
                    
                    // All keys must match 
                    if (IsMatched == false)
                        break;
                }

                if (IsMatched)
                {
                    MappedProfiles.Add(p);
                    Logger.Write(Level.Debug, "Successfully matched to profile '" + p.Abbreviation + "'");
                }
            }

            matchedProfile = FindMostSpecificProfile(MappedProfiles);
            return matchedProfile;
        }


        /// <summary>
        /// Determine final match. If multiple profiles match, the one which is the most specific,
        /// i.e. has the most variables, will match. This avoids the case where several profiles
        /// contain the same variable name but some have additional variables, some not.
        /// </summary>
        Profile? FindMostSpecificProfile(List<Profile> MappedProfiles)
        {
            if (MappedProfiles.Count == 1)
            {
                Logger.Write(Level.Debug, "Profile matched: " + MappedProfiles[0].Abbreviation);
                return MappedProfiles[0];
            }
            else if (MappedProfiles.Count > 1)
            {
                // Find first profile with maximum nbr of profile-map conditions
                // (this is the "most specific" profile)
                int idx = 0, max = 0;
                for (int i = 0; i < MappedProfiles.Count; i++)
                {
                    int count = 0;
                    if (mContentType == LabelContentType.JSON)
                        count = MappedProfiles[i].JSONProfileMap.Count;
                    else
                        count = MappedProfiles[i].XMLProfileMap.Count;

                    if (count > max)
                    {
                        max = count;
                        idx = i;
                    }
                }
                Logger.Write(Level.Debug, "Profile matched (the most specific one): " + MappedProfiles[idx].Abbreviation);
                return MappedProfiles[idx];
            }
            else
            {
                return null;
            }
        }


        #endregion

        #region Internal Methods - Validating Schema

        /// <summary>
        /// Match JSON or XML schema of the request.
        /// </summary>
        private void ValidateSchema()
        {
            
            if (mContentType == LabelContentType.JSON)
            {
                // JSON Validation using "NJsonSchema" package

                if (mProfile.JSONSchemaOption == ValidateOption.DoNotValidate)
                {
                    mSchemaValidated = true;
                    return;
                }

                var schema = JsonSchema.FromFileAsync(Path.Combine(mSchemaDir, mProfile.JSONSchema)).Result;

                var jsonObject = JObject.Parse(mRequest);
                var errors = schema.Validate(jsonObject);

                if (errors.Count == 0)
                {
                    mSchemaValidated = true;
                    Logger.Write(Level.Debug, "Validation against JSON Schema '" + mProfile.JSONSchema + "' passed.");
                }
                else
                {
                    mSchemaValidated = false;
                    String msg = "";
                    foreach (var error in errors)
                    {
                        msg += $"Error: {error.Path} - {error.Kind}";

                    }
                    Logger.LogAndThrowArgEx("Validation failed: Label request does not match JSON schema '" + mProfile.JSONSchema +
                        "' for profile '" + mProfile.Abbreviation + "'" +
                        "Errors:\r\n" + msg);
                }
            }
            else
            {
                // XML validation against DTD 

                if (mProfile.XMLSchemaOption == ValidateOption.DoNotValidate)
                {
                    mSchemaValidated = true;
                    return;
                }

                var resolver = new XmlCcustomResolver(mSchemaDir, mProfile.XMLSchemaOption, mProfile.XMLSchema);
                resolver.Credentials = System.Net.CredentialCache.DefaultCredentials;
                XmlReaderSettings settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    ValidationType = ValidationType.DTD,
                    XmlResolver = resolver
                };

                settings.ValidationEventHandler += new ValidationEventHandler(ValidationEventHandler);
                var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(mRequest));

                mValidationErrors = "";
                using (XmlReader reader = XmlReader.Create(requestStream, settings))
                {
                    try
                    {
                        while (reader.Read()) 
                        {
                            Logger.Write(Level.Debug, "  XmlReader - Node Type: " + reader.NodeType + ", Name: " + reader.Name);
                        }
                    }
                    catch (XmlException ex)
                    {
                        mValidationErrors += $"XmlException: {ex.Message} at Line {ex.LineNumber}, Position {ex.LinePosition} \r\nStack Trace:\r\n" + ex.StackTrace; ;
                    }
                    finally
                    {
                        if (!String.IsNullOrWhiteSpace(mValidationErrors))
                        {
                            mSchemaValidated = false;
                            Logger.LogAndThrowArgEx("Validation Errors with DTD '" + mProfile.XMLSchema + "': " + mValidationErrors);
                        }
                        else
                        {
                            mSchemaValidated = true;
                            Logger.Write(Level.Debug, "Validation against XML DTD Schema '" + mProfile.XMLSchema + "' passed.");
                        }
                    }
                }
                

            }
        }


        private void ValidationEventHandler(object? sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
                mValidationErrors += $"Validation-Error: {e.Message}\r\n";
            else
                mValidationErrors += $"Validation-Warning: {e.Message}\r\n";
        }

        #endregion

        #region Internal Methods - Constructing LabelDescriptor

        /// <summary>
        /// Fill out field values in a LabelDescriptor. <br/>
        /// The descriptor field-names are not created by the request - they are created 
        /// from the profile configuration (just as they would in the UI), ensuring that 
        /// all fields defined in the profile appear.<br/>
        /// </summary>
        private LabelDescriptor FillLabelDescriptor()
        {
            LabelDescriptor descriptor = new LabelDescriptor(mProfile);

            if (mContentType == LabelContentType.JSON)
                return FillDescriptorFromJSON(descriptor);
            else
                return FillDescriptorFromXML(descriptor);
        }


        // --------- JSON ----------------------------------------------------------------------------

        /// <summary>
        /// Fill values in LabelDescriptor from JSON request.<br/>
        /// If the API request needs a database query, we need to fill out the db-query fields
        /// (SearchFields) and not the db-result fields (DataFields).<br/>
        /// If it's a regular full-final-data API request, we ignore db-query fields but we
        /// need to fill out fields configured as DataFields if any.
        /// </summary>
        private LabelDescriptor FillDescriptorFromJSON(LabelDescriptor descriptor)
        {
            if (mJSONContent == null)
                throw new ArgumentException("Invalid JSON request. Ensure the JSON request contains a single JSON object with no nested objects or arrays.");

            if (mRequireDbQuery)
                descriptor = FillValuesFromJSON(descriptor, mProfile.JSONSearchFieldMap);
            else
                descriptor = FillValuesFromJSON(descriptor, mProfile.JSONDataFieldMap);

            descriptor = FillValuesFromJSON(descriptor, mProfile.JSONEditFieldMap);
            descriptor = FillLabelCountJSON(descriptor, mProfile.JSONLabelCount);

            return descriptor;
        }

        /// <summary>
        /// Fill values for one of the FieldMap dictionaries from JSON request.
        /// </summary>
        private LabelDescriptor FillValuesFromJSON(LabelDescriptor descriptor, Dictionary<String, String> fieldList)
        {
            foreach (var key in fieldList.Keys)
            {
                Logger.Write(Level.Debug, " Getting value for field '" + key + "'");

                String jsonProp = fieldList[key];
                if (mJSONContent.ContainsKey(jsonProp))
                    descriptor.EditFieldValue(key, mJSONContent[jsonProp]);
                else
                    throw new ArgumentException("Invalid label request format (JSON): Field '" + key + "' not matched in JSON (expected JSON property name '" + jsonProp + "').");
            }

            return descriptor;
        }

        /// <summary>
        /// Fill out label-quantity
        /// </summary>
        private LabelDescriptor FillLabelCountJSON(LabelDescriptor descriptor, String countProp)
        {
            if (mJSONContent.ContainsKey(countProp))
                descriptor.LabelCount = Convert.ToInt32(mJSONContent[countProp]);
            else
                throw new ArgumentException("Invalid label request format (JSON): Configured field for label-quantity '" + countProp + "' not found in JSON request.");
            //
            return descriptor;
        }



        // --------- XML ----------------------------------------------------------------------------


        /// <summary>
        /// Fill values in LabelDescriptor from XML request.<br/>
        /// If the API request needs a database query, we need to fill out the db-query fields
        /// (SearchFields) and not the db-result fields (DataFields).<br/>
        /// If it's a regular full-final-data API request, we ignore db-query fields but we
        /// need to fill out fields configured as DataFields if any.
        /// </summary>
        private LabelDescriptor FillDescriptorFromXML(LabelDescriptor descriptor)
        {
            if (mXMLContent == null)
                throw new ArgumentException("Invalid XML request. Ensure it contains well-formed XML.");

            if (mRequireDbQuery)
                descriptor = FillValuesFromXML(descriptor, mProfile.XMLSearchFieldMap);
            else
                descriptor = FillValuesFromXML(descriptor, mProfile.XMLDataFieldMap);

            descriptor = FillValuesFromXML(descriptor, mProfile.XMLEditFieldMap);
            descriptor = FillLabelCountXML(descriptor, mProfile.XMLLabelCount);

            return descriptor;
        }



        private LabelDescriptor FillValuesFromXML(LabelDescriptor descriptor, Dictionary<String, String> fieldList)
        {
            XPathNavigator nav = mXMLContent.CreateNavigator();

            String errorList = "";
            foreach (var key in fieldList.Keys)
            {
                Logger.Write(Level.Debug, " Getting value for field '" + key + "'");

                String xpathExpression = fieldList[key];
                XPathNodeIterator iterator = nav.Select(xpathExpression);

                if (iterator != null)
                {
                    if (iterator.MoveNext() && iterator.Current != null)
                    {
                        string strValue = iterator.Current.Value;
                        descriptor.EditFieldValue(key, strValue);
                    }
                    else
                        errorList += "XML node/attribute for field-name '" + key + "' not found (XPath expression: '" + xpathExpression + "').\r\n";
                }
                else
                    errorList += "There are no XML nodes for the field variables at all.";
            }

            if (!String.IsNullOrWhiteSpace(errorList))
                throw new ArgumentException(errorList);

            return descriptor;
        }


        /// <summary>
        /// Fill out label-quantity
        /// </summary>
        private LabelDescriptor FillLabelCountXML(LabelDescriptor descriptor, String countExpr)
        {
            XPathNavigator nav = mXMLContent.CreateNavigator();
            XPathNodeIterator iterator = nav.Select(countExpr);

            if (iterator != null)
            {
                if (iterator.MoveNext() && iterator.Current != null)
                {
                    string strValue = iterator.Current.Value;
                    descriptor.LabelCount = Convert.ToInt32(strValue);
                }
                else
                    throw new ArgumentException("Invalid label request format (JSON): Configured XPath expression for label-quantity '" + countExpr + "' invalid.");
            }

            return descriptor;
        }

        #endregion

        #region Internal Methods - Printer Selection

        private Printer DeterminePrinter(List<Printer> printerList)
        {
            if (mContentType == LabelContentType.JSON)
                return GetPrinterFromJSON(printerList);
            else
                return GetPrinterFromXML(printerList);
        }

        private Printer GetPrinterFromXML(List<Printer> printerList)
        {
            if (mXMLContent == null)
                throw new ArgumentException("Invalid XML request. Ensure it contains well-formed XML.");
            
            // If no printer-mapping defined in profile, use default:
            if (mProfile.XMLPrinterList.Count == 0)
            {
                Printer? defPrinter = printerList.FirstOrDefault(p => p.Name == mProfile.DefaultPrinter);
                if (defPrinter == null)
                    throw new ApplicationException("API request does not specify a printer, and default printer is NULL. Cannot print.");
                //
                return defPrinter;
            }

            List<String> printerNameList = new List<String>();
            String errorList = "";

            XPathNavigator nav = mXMLContent.CreateNavigator();

            // There may be multiple printer options specified
            // (We will select just one of them in the end)
            foreach (String xpathExpression in mProfile.XMLPrinterList)
            {
                XPathNodeIterator iterator = nav.Select(xpathExpression);

                if (iterator != null)
                {
                    if (iterator.MoveNext() && iterator.Current != null)
                    {
                        string strValue = iterator.Current.Value;
                        printerNameList.Add(strValue);
                    }
                    // (silently ignore errors, see if we find a match amongst the options)
                    // else
                    //    errorList += "XML node/attribute for printer-name-list XPath expression: '" + xpathExpression + "' not found).\r\n";
                }
                // else
                // errorList += "There are no XML nodes for the printer list variables at all.";
            }

            if (!String.IsNullOrWhiteSpace(errorList))
                throw new ArgumentException(errorList);

            // Use the first match of the printer names found:
            Printer? chosenPrinter = null;
            if (printerNameList.Count > 0)
                chosenPrinter = printerList.Find(p => printerNameList.Any(pn => p.Name == pn));
            else
            {
                Logger.Write(Level.Notice, "XML Label request does not contain valid info to select a printer. Falling back on using default printer.");
                chosenPrinter = printerList.FirstOrDefault(p => p.Name == mProfile.DefaultPrinter);
            }

            if (chosenPrinter == null)
            {
                String msg = "No valid printer found in the XML label request, and no default printer defined.";
                Logger.Write(Level.Error, msg);
                throw new ArgumentException(msg);
            }

            return chosenPrinter;
        }



        private Printer GetPrinterFromJSON(List<Printer> printerList)
        {
            if (mJSONContent == null)
                throw new ArgumentException("Invalid JSON request. Ensure the JSON request contains a single JSON object with no nested objects or arrays.");

            // If no printer-mapping defined in profile, use default:
            if (mProfile.JSONPrinterList.Count == 0)
            {
                Printer? defPrinter = printerList.FirstOrDefault(p => p.Name == mProfile.DefaultPrinter);
                if (defPrinter == null)
                    throw new ApplicationException("API request does not specify a printer, and default printer is NULL. Cannot print.");
                //
                return defPrinter;
            }

            List<String> printerNameList = new List<String>();

            foreach (var key in mProfile.JSONEditFieldMap.Keys)
            {
                String jsonProp = mProfile.JSONEditFieldMap[key];
                if (mJSONContent.ContainsKey(jsonProp))
                {
                    printerNameList.Add(mJSONContent[jsonProp]);
                }
                // (silently ignore errors, see if we find a match amongst the options)
                // else
                //     throw new ArgumentException("Invalid label request format (JSON): expected JSON property name '" + jsonProp + "' not found).");
            }

            // Use the first match of the printer names found:
            Printer? chosenPrinter = null;
            if (printerNameList.Count > 0)
                chosenPrinter = printerList.Find(p => printerNameList.Any(pn => p.Name == pn));
            else
            {
                Logger.Write(Level.Notice, "JSON Label request does not contain valid info to select a printer. Falling back on using default printer.");
                chosenPrinter = printerList.FirstOrDefault(p => p.Name == mProfile.DefaultPrinter);
            }

            if (chosenPrinter == null)
            {
                String msg = "No valid printer found in the JSON label request, and no default printer defined.";
                Logger.Write(Level.Error, msg);
                throw new ArgumentException(msg);
            }

            return chosenPrinter;
        }

        #endregion

    }

}
