using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel.DataAnnotations;

namespace MipSdkRazorSample.Models
{
    public class FileData
    {
        public int ID { get; set; }
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;
        [Display(Name = "Container")]
        public string Container { get; set; } = string.Empty;

        [Display(Name = "Label Id")]
        public string LabelId { get; set; } = string.Empty;
        public Int64 Size { get; set; } = 0;

        [Display(Name = "Owner")]
        public string Owner { get; set; } = string.Empty;

        [Display(Name = "Is Protected")]
        public bool IsProtected { get; set; } = false;

        [Display(Name = "Serialized Publishing License")]
        public string SerializedPublishingLicense { get; set; } = string.Empty;
    }
}
