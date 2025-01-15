using System;
using System.Xml;

namespace LabelCast
{

    /// <summary>
    /// Custom XmlUrlResolver which obtains a DTD from a local file in a specified folder.<br/>
    /// You need to create an instance of this and assign to the "settings" object when
    /// creating the XmlReader to read the doc. It is that XmlReader instance which will
    /// call the overridden methods of this resolver.
    /// </summary>
    public class XmlCcustomResolver : XmlUrlResolver
    {
        #region Fields 

        // Directory containing all schema files
        private String mBaseDir = "";
        
        // Schema defined in profile
        private String mProfileSchema = "";
        
        // Should we use the schema defined in the profile or the one in the XML?
        private ValidateOption mSchemaOption = ValidateOption.UseProfileSchema;

        #endregion

        #region Constructors

        public XmlCcustomResolver(String baseDir, ValidateOption schemaOption, String profileSchema) : base()
        {
            mBaseDir = baseDir;
            mProfileSchema = profileSchema;
            mSchemaOption = schemaOption;
        }

        #endregion

        #region Public Overrides

        /// <summary>
        /// Override of GetEntity method of XmlUrlResolver.<br/>
        /// This method is called by XmlReader when the entity (such as a DTD) 
        /// is specified in the XML by an absolute URI.<br/>
        /// The override handles the case of a file:/// URI.
        /// </summary>
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            Logger.Write(Level.Debug, "XmlCustomResolver.GetEntity: absoluteUri = " + absoluteUri.ToString() +
                                      ", role = " + role + ", ofObjectToReturn = " + ofObjectToReturn?.ToString() ?? "null");

            if (absoluteUri.Scheme == "file" && (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream)))
            {
                String filePath = absoluteUri.LocalPath;
                Logger.Write(Level.Debug, " --> resolve resources from local directory: " + filePath);
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return fileStream;
            }
            else
            {
                Logger.Write(Level.Debug, " --> default behavior of the XmlUrlResolver class (follow URI)");
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

        /// <summary>
        /// Override of ResolveUri method of XmlUrlResolver.<br/>
        /// This method gets called by XmlReader when the XML contains a relative URI.
        /// Usually, the base URI is determined by the base URI of the source XML doc, 
        /// What we do here is to get the entity (DTD) from local file,
        /// interpreting the relative URI as the file name.
        /// </summary>
        public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
        {
            Logger.Write(Level.Debug, "XmlCustomResolver.ResolveUri: baseUri = '" + (baseUri?.ToString() ?? "") + "', relativeUri = " + relativeUri);
            
            String filePath = "";
            if (mSchemaOption == ValidateOption.UseSchemaInDocument)
                filePath = Path.Combine(mBaseDir, relativeUri ?? "");
            else 
                filePath = Path.Combine(mBaseDir, mProfileSchema);      // defined in profile

            return new Uri("file:///" + filePath);
        }

        #endregion
    }


}


