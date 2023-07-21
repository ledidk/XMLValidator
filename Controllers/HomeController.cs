using Microsoft.AspNetCore.Mvc;
using XMLValidator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Xml;
using System.Xml.Schema;

namespace XMLValidator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController>? _logger;
        // ...
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(XMLandSchemaFileUpload files)
        {
            if (ModelState.IsValid)
            {
                // Saving the uploaded files to a specific location
                string xmlFilePath = SaveFile(files.XmlFile, "XmlFiles");
                string schemaFilePath = SaveFile(files.SchemaFile, "SchemaFiles");

                // Getting the file names without the directory path
                string xmlFileName = Path.GetFileName(files.XmlFile.FileName);
                string schemaFileName = Path.GetFileName(files.SchemaFile.FileName);

                // Assigning the file names to ViewBag
                ViewBag.XMLFileName = xmlFileName;
                ViewBag.SchemaFileName = schemaFileName;

                // Validating XML against the schema
                List<XmlValidationError> errors = ValidateXML(xmlFilePath, schemaFilePath);

                // Processing the error messages and extract the last word before the colon and hyphrn
                foreach (var error in errors)
                {
                    try { error.Element = GetLastWordBeforeColon(error.Message); } catch { }
                }


                if (errors.Count == 0)
                {
                    ViewBag.Message = $"No errors found when validating XML file '{files.XmlFile.FileName}' against the schema '{files.SchemaFile.FileName}'.";

                    return View("ValidationSuccess");
                }
                else
                {
                    ViewBag.Message = $"The following errors were found when validating XML file '{files.XmlFile.FileName}' against the schema '{files.SchemaFile.FileName}'.";
                    return View("ValidationError", errors);
                }
            }

            return View("Index");
        }

        private string SaveFile(IFormFile file, string folder)
        {
            string uploadsFolder = Path.Combine(_environment.WebRootPath, folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return filePath;
        }

        private List<XmlValidationError> ValidateXML(string xmlFilePath, string schemaFilePath)
        {
            List<XmlValidationError> errors = new List<XmlValidationError>();

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += (sender, args) =>
                {
                    XmlValidationError error = new XmlValidationError
                    {
                        Element = args.Exception.SourceUri,
                        ErrorType = args.Severity.ToString(),
                        Line = args.Exception.LineNumber,
                        Column = args.Exception.LinePosition,
                        Message = args.Message
                    };

                    errors.Add(error);
                };

                // Loading the XML schema & set to be used //Missing part 

                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, schemaFilePath);
                settings.Schemas = schemaSet;

                using (XmlReader reader = XmlReader.Create(xmlFilePath, settings))
                {
                    while (reader.Read())
                    {
                        // Reading  the XML file to trigger validation events
                    }
                }

                ViewBag.ValidationPassed = true;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error validating: {ex.Message}");
                ViewBag.ValidationPassed = false;
            }

            return errors;
        }


        private string GetLastWordBeforeColon(string errorMessage)
        { 
            // XML Elemet displaying message was long
            int lastColonIndex = errorMessage.LastIndexOf(":");
            int lastSpaceIndex = errorMessage.LastIndexOf(" ");

            int index = Math.Max(lastColonIndex, lastSpaceIndex);

            if (index != -1)
            {
                string substring = errorMessage.Substring(index + 1).Trim();
                string[] words = substring.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length > 0)
                {
                    string lastWord = words[^1];
                    lastWord = lastWord.TrimStart('\'').TrimEnd('.', '\'');
                    return lastWord;
                }
            }

            return "Unknown";
        }

    }
}
