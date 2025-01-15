using LabelCast;


namespace LabelCastTest
{
    public class RequestHandlerTests
    {
        #region Fields

        String mConfigDir = "";
        List<Printer> PrinterList = new List<Printer>();
        List<Profile> ProfileList = new List<Profile>();

        #endregion

        #region Constructors + Setup 

        public RequestHandlerTests()
        {
            Logger.CurrentLogLevel = Level.Debug;
            Logger.CurrentLogFile = @"c:\Program Info\LabelCast\Logs\UnitTestLog.txt";

            String projectDir = @"E:\Source\Projects\LabelCast\WebApp\Test\LabelCastTest\";
            mConfigDir = Path.Combine(projectDir, @"__TestData__\RequestHandler");

            CreatePrinters();
            CreateProfiles();

            Logger.Write(Level.Debug, "RequestHandlerTests started.");
            Logger.Write(Level.Debug, "Config Dir (location of schema files) = " + mConfigDir);
        }

        [SetUp]
        public void Setup()
        {
            // Runs before every test - reset all profiles to "DoNotValidate"
            SetDoNotValidateSchema();
        }

        #endregion


        #region Tests - Content Format

        [Test]
        public void ContentFormatXML()
        {
            Logger.Write(Level.Debug, "Test ContentFormatXML", true);

            String request = "<?xml version='1.0' encoding='utf-8'?>\r\n" +
                             "<label format='codeLabel' Quantity='7'>\r\n" +
                             "  <variable name= 'Code'>93049145</variable>\r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable>\r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
        }


        [Test]
        public void ContentFormatJSON()
        {
            Logger.Write(Level.Debug, "Test ContentFormatJSON", true);

            String request = "{ \r\n" +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak' \r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
        }


        [Test]
        public void ContentFormatInvalid()
        {
            Logger.Write(Level.Debug, "Test ContentFormatInvalid", true);

            String request = "[{  code: 09454, description: blah, }, {code: 03045, description: x}]";

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }

        #endregion

        #region Tests - Matching Profile - XML

        [Test]
        public void MatchXMLProfile1()
        {
            Logger.Write(Level.Debug, "Test MatchXMLProfile1", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label format='codeLabel' Quantity = '7'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
        }


        [Test]
        public void MatchXMLProfile2()
        {
            Logger.Write(Level.Debug, "Test MatchXMLProfile2", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label type='name-label' Quantity='7'> \r\n" +
                             "  <variable name= 'Item'>93049145</variable> \r\n" +
                             "  <variable name= 'Name'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test2"));
        }

        [Test]
        public void MatchXMLProfile3()
        {
            // Multiple profile map conditions.
            // The first one is the SAME as on profile 2 though - so it will map also
            // profile 2. 
            // RequestHandler MUST map only the most specific one, i.e. the one with
            // the most profile-map-conditions: 

            Logger.Write(Level.Debug, "Test MatchXMLProfile3", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label type='name-label' labelformat='medium' Quantity='7'> \r\n" +
                             "  <variable name= 'Item'>93049145</variable> \r\n" +
                             "  <variable name= 'Name'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test4"));
        }

        #endregion

        #region Tests - Match Profile - JSON

        [Test]
        public void MatchJSONProfile1()
        {
            Logger.Write(Level.Debug, "Test MatchJSONProfile1", true);

            String request = "{ \r\n" +
                             " '$schema': 'schema1-codeLabel.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
        }


        [Test]
        public void MatchJSONProfile2()
        {
            Logger.Write(Level.Debug, "Test MatchJSONProfile2", true);

            String request = "{ \r\n" +
                             " '$schema': 'schema2-nameLabel.json', \r\n" +
                             " 'labeltype': 'name-label', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'item': '93049145', \r\n" +
                             " 'name': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test2"));
        }


        [Test]
        public void MatchProfileImpossible1()
        {
            // This request is valid JSON but does not contain
            // elements allowing matching a profile

            Logger.Write(Level.Debug, "Test MatchProfileImpossible1", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak' \r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void MatchProfileImpossible2()
        {
            // Request valid but the value of 'labeltype' prop does not match
            // any configured profile

            Logger.Write(Level.Debug, "Test MatchProfileImpossible2", true);

            String request = "{ \r\n" +
                             " '$schema': 'schema2-nameLabel.json', \r\n" +
                             " 'labeltype': 'super-special-label', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void MatchJSONProfile3()
        {
            // profile with multiple match conditions

            Logger.Write(Level.Debug, "Test MatchJSONProfile3", true);

            String request = "{ \r\n" +
                             " '$schema': 'schema2-nameLabel.json', \r\n" +
                             " 'type': 'name-label', \r\n" +
                             " 'format': 'medium', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'item': '93049145', \r\n" +
                             " 'name': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test4"));
        }


        #endregion

        #region Tests - Validating against Schema - XML

        [Test]
        public void ValidateXMLSchema1()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Valid data - but the XML does not reference a DTD at all
            // This will result in a DTD warning "no DTD found", and thus validation fails
            Logger.Write(Level.Debug, "Test ValidateXMLSchema1 (no DTD ref)", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label format='codeLabel' Quantity = '8'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void ValidateXMLSchema2()
        {
            // Not validating at all
            SetDoNotValidateSchema();

            // Valid data - but the XML does not reference a DTD at all
            // This would result in a DTD warning "no DTD found" - but we skip validation
            // so it will pass
            Logger.Write(Level.Debug, "Test ValidateXMLSchema2 (no validation)", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label format='codeLabel' Quantity = '8'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
            Assert.IsTrue(handler.IsSchemaValidated);
        }


        [Test]
        public void ValidateXMLSchema3()
        {
            SetUseProfileSchema();

            // Inline schema - inside the XML doc
            // * This should pass validation
            Logger.Write(Level.Debug, "Test ValidateXMLSchema3 (inline)", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             " <!DOCTYPE label [\r\n" +
                             "  <!ELEMENT label (variable, variable)>\r\n" +
                             " <!ATTLIST label format CDATA #REQUIRED Quantity CDATA #REQUIRED> \r\n" +
                             " <!ELEMENT variable (#PCDATA)>\r\n" +
                             "  <!ATTLIST variable name CDATA #REQUIRED>\r\n" +
                             " ]>\r\n" +
                             "<label format='codeLabel' Quantity = '8'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
            Assert.IsTrue(handler.IsSchemaValidated);
        }


        [Test]
        public void ValidateXMLSchema4()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Same schema as the above inline one
            // * Valid data, with DTD reference line
            // * The profile name in the reference line is nonsense but we don't read this
            //   when ValidateOption is set to "UseProfileSchema" - existence of the line is enough
            // * This should pass validation
            Logger.Write(Level.Debug, "Test ValidateXMLSchema4 (local DTD)", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<!DOCTYPE label SYSTEM \"schema1-codeLabel.dtd\">\r\n" +
                             "<label format='codeLabel' Quantity = '8'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
            Assert.IsTrue(handler.IsSchemaValidated);
        }


        [Test]
        public void ValidateXMLSchema5()
        {
            // No validation
            SetDoNotValidateSchema();

            // With DTD reference line
            // No errors shall occur, although format does not match profile variables
            // (Field Description changed to "SpecialName")

            Logger.Write(Level.Debug, "Test ValidateXMLSchema5 (no validation)", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<!DOCTYPE label SYSTEM 'xxx.dtd'>\r\n" +
                             "<label format='codeLabel' Quantity='7'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "  <variable name= 'SpecialName'>Oaky-Woody</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
        }


        [Test]
        public void ValidateXMLSchema6()
        {
            // Using the schema referenced in the XML doc
            SetUseDocumentSchema();

            // * Valid data, with DTD reference line
            // * The profile name in the reference line is nonsense but we don't read this
            //   when ValidateOption is set to "UseProfileSchema" - existence of the line is enough
            // * This should pass validation
            Logger.Write(Level.Debug, "Test ValidateXMLSchema", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<!DOCTYPE label SYSTEM 'schema1-codeLabel.dtd'>\r\n" +
                             "<label format='codeLabel' Quantity = '8'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Description'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
            Assert.IsTrue(handler.IsSchemaValidated);
        }


        [Test]
        public void ValidateXMLSchemaNotFound()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Schema file referenced in matching profile "Test3" does not exist
            // Must throw ArgumentException

            Logger.Write(Level.Debug, "Test ValidateXMLSchemaNotFound", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<!DOCTYPE label SYSTEM 'xxx.dtd'>\r\n" +
                             "<label type='NameLabel3' Quantity='7'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'Name'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void ValidateXMLSchemaInvalid()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Validation fails because content does not match schema 
            // Field "Description" missing - called "SpecialName" instead

            Logger.Write(Level.Debug, "Test ValidateXMLSchemaInvalid", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<!DOCTYPE label SYSTEM 'xxx.dtd'>\r\n" +
                             "<label format='codeLabel' Quantity='7'> \r\n" +
                             "  <variable name= 'Code'>93049145</variable> \r\n" +
                             "  <variable name= 'SpecialName'>Wood Chair, Oak</variable> \r\n" +
                             "</label>";

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        #endregion

        #region Tests - Validating against Schema - JSON

        [Test]
        public void ValidateJSONSchema()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Valid profile - should validate fine
            Logger.Write(Level.Debug, "Test ValidateJSONSchema", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
            Assert.IsTrue(handler.IsSchemaValidated);
        }


        [Test]
        public void ValidateJSONSchemaNotFound()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Local Schema file referenced in matching profile "Test3" does not exist
            Logger.Write(Level.Debug, "Test ValidateJSONSchemaNotFound", true);

            String request = "{ " +
                             " '$schema': 'schema3.json', \r\n" +
                             " 'labelformat': 'NameLabel3', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'name': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void ValidateJSONSchemaInvalid()
        {
            // Using Profile-Schema
            SetUseProfileSchema();

            // Validation fails because content does not match schema
            // Field "Description" missing - called "special-name" instead

            Logger.Write(Level.Debug, "Test ValidateJSONSchemaInvalid", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'special-name': 'Wood Chair, Oak'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }

        [Test]
        public void ValidateJSON_None()
        {
            // "Do not validate" option enabled
            SetDoNotValidateSchema();

            // Although it's the same invalid document format, no validation error 

            Logger.Write(Level.Debug, "Test ValidateJSONSchemaInvalid", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 7, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Wood Chair, Oak',\r\n" +
                             " 'special-name': 'Woody-Oaky'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));
        }

        #endregion

        #region Tests - Creating LabelDescriptor from XML

        [Test]
        public void DescriptorFromXML1()
        {
            // "Do not validate" option enabled to avoid schema validation exceptions
            SetDoNotValidateSchema();

            // Must error because ProfileFieldMap contains field "Description"
            // which isn't contained in the XML doc:

            Logger.Write(Level.Debug, "Test DescriptorFromXML1", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label format='codeLabel' Quantity='15'> \r\n" +
                             "  <variable name='Code'>93049145</variable> \r\n" +
                             "  <variable name='SpecialName'>Oaky-Woody</variable> \r\n" +
                             "</label>";

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void DescriptorFromXML2()
        {
            // "Do not validate" option enabled to avoid schema validation exceptions
            SetDoNotValidateSchema();

            // Valid XML doc 

            Logger.Write(Level.Debug, "Test DescriptorFromXML2", true);

            String request = "<?xml version='1.0' encoding='utf-8'?> \r\n" +
                             "<label format='codeLabel' Quantity='15'> \r\n" +
                             "  <variable name='Code'>93049145</variable> \r\n" +
                             "  <variable name='Description'>Oaky-Woody</variable> \r\n" +
                             "</label>";

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.XML));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));

            var fields = handler.Descriptor.EditableFields;
            Assert.That(fields.Count, Is.EqualTo(2));
            Assert.IsTrue(fields.Keys.Contains("Code"));
            Assert.IsTrue(fields.Keys.Contains("Description"));
            foreach (String key in fields.Keys)
            {
                if (key == "Code")
                    Assert.That(fields[key], Is.EqualTo("93049145"));
                if (key == "Description")
                    Assert.That(fields[key], Is.EqualTo("Oaky-Woody"));
            }

            Assert.That(handler.Descriptor.LabelCount, Is.EqualTo(15));
        }


        #endregion

        #region Tests - Creating LabelDescriptor from JSON

        [Test]
        public void DescriptorFromJSON1()
        {
            // "Do not validate" option enabled to avoid schema validation exceptions
            SetDoNotValidateSchema();

            // Must error because ProfileFieldMap contains field "Description"
            // which isn't contained in the doc:

            Logger.Write(Level.Debug, "Test ValidateJSONSchemaInvalid", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 15, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'special-name': 'Woody-Oaky'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act + Assert
            Assert.Throws<ArgumentException>(delegate
            {
                var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);
            });
        }


        [Test]
        public void DescriptorFromJSON2()
        {
            // "Do not validate" option enabled to avoid schema validation exceptions
            SetDoNotValidateSchema();

            // Valid doc 

            Logger.Write(Level.Debug, "Test ValidateJSONSchemaInvalid", true);

            String request = "{ " +
                             " '$schema': 'schema1.json', \r\n" +
                             " 'labelformat': 'codeLabel', \r\n" +
                             " 'labelcount': 15, \r\n" +
                             " 'code': '93049145', \r\n" +
                             " 'description': 'Woody-Oaky'\r\n" +
                             "}";
            request = request.Replace("'", "'");

            // Act 
            var handler = new RequestHandler(request, mConfigDir, ProfileList, PrinterList);

            // Asserts
            Assert.That(handler.ContentType, Is.EqualTo(LabelContentType.JSON));
            Assert.That(handler.RequestProfile.Abbreviation, Is.EqualTo("Test1"));

            var fields = handler.Descriptor.EditableFields;
            Assert.That(fields.Count, Is.EqualTo(2));
            Assert.IsTrue(fields.Keys.Contains("Code"));
            Assert.IsTrue(fields.Keys.Contains("Description"));
            foreach (String key in fields.Keys)
            {
                if (key == "Code")
                    Assert.That(fields[key], Is.EqualTo("93049145"));
                if (key == "Description")
                    Assert.That(fields[key], Is.EqualTo("Woody-Oaky"));
            }

            Assert.That(handler.Descriptor.LabelCount, Is.EqualTo(15));
        }


        #endregion


        #region Helper Methods 

        private void SetUseProfileSchema()
        {
            foreach (Profile p in ProfileList)
            {
                p.JSONSchemaOption = ValidateOption.UseProfileSchema;
                p.XMLSchemaOption = ValidateOption.UseProfileSchema;
            }
        }

        private void SetUseDocumentSchema()
        {
            foreach (Profile p in ProfileList)
            {
                p.JSONSchemaOption = ValidateOption.UseSchemaInDocument;
                p.XMLSchemaOption = ValidateOption.UseSchemaInDocument;
            }
        }

        private void SetDoNotValidateSchema()
        {
            foreach (Profile p in ProfileList)
            {
                p.JSONSchemaOption = ValidateOption.DoNotValidate;
                p.XMLSchemaOption = ValidateOption.DoNotValidate;
            }
        }

        private void CreatePrinters()
        {
            PrinterList = new List<Printer>
            {
                new Printer { Name = "Zebra2" },
                new Printer { Name = "Label4" }
            };
        }

        private void CreateProfiles()
        {
            ProfileList = new List<Profile>
            {
             new Profile
             {
                Abbreviation = "Test1",
                EditableFields = new List<String> { "Code", "Description" },
                DefaultPrinter = PrinterList.FirstOrDefault()?.Name,

                XMLSchema = "schema1-codeLabel.dtd",
                XMLSchemaOption = ValidateOption.DoNotValidate,
                XMLProfileMap = new Dictionary<string, string>
                {
                    // XPath for <label format=""> and value
                    { "/label/@format", "codeLabel" },
                },
                XMLEditFieldMap = new Dictionary<string, string>
                {
                    // XPath to the location of nodes for Code and Description fields
                    { "Code", "/label/variable[@name='Code']" },
                    { "Description", "/label/variable[@name='Description']" }
                },
                XMLLabelCount = "/label/@Quantity",

                JSONSchema = "schema1-codeLabel.json",
                JSONSchemaOption = ValidateOption.DoNotValidate,
                JSONProfileMap = new Dictionary<string, string>
                {
                    // JSON must contain a "labelformat" element with value "codeLabel"
                    { "labelformat", "codeLabel" }
                },
                JSONEditFieldMap = new Dictionary<string, string>
                {
                    // Mapping profile field names to JSON prop names
                    { "Code", "code" },
                    { "Description", "description" },
                    { "Number of Labels", "labelcount" }
                },
                JSONLabelCount = "labelcount"
            },

            new Profile
            {
                Abbreviation = "Test2",
                EditableFields = new List<String> { "Item", "Name" },
                DefaultPrinter = PrinterList.FirstOrDefault()?.Name,

                XMLSchema = "schema2-nameLabel.dtd",
                XMLSchemaOption = ValidateOption.DoNotValidate,
                XMLProfileMap = new Dictionary<string, string>
                {
                    // XPath for <label type=""> and value
                    { "/label/@type", "name-label" },
                },
                XMLEditFieldMap = new Dictionary<string, string>
                {
                    // XPath to the location of nodes for Code and Description fields
                    { "Item", "/label/variable[@name='Item']" },
                    { "Name", "/label/variable[@name='Name']" },
                    { "Number of Labels", "/label/@Quantity" }
                },
                XMLLabelCount = "/label/@Quantity",

                JSONSchema = "schema2-nameLabel.json",
                JSONSchemaOption = ValidateOption.DoNotValidate,
                JSONProfileMap = new Dictionary<string, string>
                {
                    // JSON must contain a "labeltype" element with value "name-label"
                    { "labeltype", "name-label" }
                },
                JSONEditFieldMap = new Dictionary<string, string>
                {
                    // Mapping profile field names to JSON prop names
                    { "Item", "item" },
                    { "Name", "name" },
                    { "Number of Labels", "labelcount" }
                },
                JSONLabelCount = "labelcount"
            },

            // this profile points to non-existent XML / JSON schema files
            new Profile
            {
                Abbreviation = "Test3",
                EditableFields = new List<String> { "Item", "Name" },
                DefaultPrinter = PrinterList.FirstOrDefault()?.Name,

                XMLSchema = "schema3-nonexist.dtd",
                XMLSchemaOption = ValidateOption.DoNotValidate,
                XMLProfileMap = new Dictionary<string, string>
                {
                    // XPath for <label type=""> and value
                    { "/label/@type", "NameLabel3" },
                },
                XMLEditFieldMap = new Dictionary<string, string>
                {
                    // XPath to the location of nodes for Code and Description fields
                    { "Item", "/label/variable[@name='Item']" },
                    { "Name", "/label/variable[@name='Name']" },
                    { "Number of Labels", "/label/@Quantity" }
                },
                XMLLabelCount = "/label/@Quantity",

                JSONSchema = "schema3-nonexist.json",
                JSONSchemaOption = ValidateOption.DoNotValidate,
                JSONProfileMap = new Dictionary<string, string>
                {
                    // JSON must contain a "labeltype" element with value "name-label"
                    { "labeltype", "NameLabel3" }
                },
                JSONEditFieldMap = new Dictionary<string, string>
                {
                    // Mapping profile field names to JSON prop names
                    { "Item", "item" },
                    { "Name", "name" },
                    { "Number of Labels", "labelcount" }
                },
                JSONLabelCount = "labelcount"
            }, 

            // Profile with multiple ProfileMap conditions
            // Note that if profiles exist which have just one, but the same, map-field
            // for profiles, there may be multiple matches. Ensure map-fields are unique!
            new Profile
            {
                Abbreviation = "Test4",
                EditableFields = new List<String> { "Item", "Name" },
                DefaultPrinter = PrinterList.FirstOrDefault()?.Name,

                XMLSchema = "schema3.dtd",
                XMLSchemaOption = ValidateOption.DoNotValidate,
                XMLProfileMap = new Dictionary<string, string>
                {
                    // Multiple conditions to map this profile:
                    { "/label/@type", "name-label" },
                    { "/label/@labelformat", "medium" }
                },
                XMLEditFieldMap = new Dictionary<string, string>
                {
                    // XPath to the location of nodes for Code and Description fields
                    { "Item", "/label/variable[@name='Item']" },
                    { "Name", "/label/variable[@name='Name']" },
                    { "Number of Labels", "/label/@Quantity" }
                },
                XMLLabelCount = "/label/@Quantity",

                JSONSchema = "schema3.json",
                JSONSchemaOption = ValidateOption.DoNotValidate,
                JSONProfileMap = new Dictionary<string, string>
                {
                    // JSON must contain a "labeltype" element with value "name-label"
                    { "type", "name-label" },
                    { "format", "medium" }
                },
                JSONEditFieldMap = new Dictionary<string, string>
                {
                    // Mapping profile field names to JSON prop names
                    { "Item", "item" },
                    { "Name", "name" },
                    { "Number of Labels", "labelcount" }
                },
                JSONLabelCount = "labelcount"
             }
          };
        }

        #endregion

    }
}