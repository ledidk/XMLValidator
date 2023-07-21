using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace XMLValidator.Models
{
    public class XMLandSchemaFileUpload
    {
        [Display(Name = "XML File")]
        [Required(ErrorMessage = "Please select an XML file.")]
        public IFormFile XmlFile { get; set; }

        [Display(Name = "Schema File")]
        [Required(ErrorMessage = "Please select a schema file.")]
        public IFormFile SchemaFile { get; set; }
    }
}

