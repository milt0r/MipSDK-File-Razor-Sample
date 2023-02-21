using Microsoft.Extensions.Configuration;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using Microsoft.InformationProtection.File;
using MipSdkRazorSample.Models;

namespace MipSdkRazorSample.Services
{
    public interface IMipService
    {
        public MemoryStream ApplyMipLabel(Stream inputStream, string labelId, string fileName);
        public MemoryStream ApplyMipProtectionFromPL(Stream inputStream, string serializedPublishingLicense, string fileName);
        public string GetDefaultLabel(string userId);
        ContentLabel GetFileLabel(string userId, Stream inputStream, string fileName);
        public int GetLabelSensitivityValue(string labelGuid);          
        public IList<MipLabel> GetMipLabels(string userId);             
        public Stream GetTemporaryDecryptedStream(Stream inputStream, string userId, string fileName);
        public bool IsLabeledOrProtected(Stream inputStream, string fileName);
        public bool IsProtected(Stream inputStream, string fileName);
        public string GetSerializedPublishingLicense(Stream inputStream, string fileName);
        public MemoryStream RemoveProtection(Stream inputStream, string fileName, string labelId, string userId);
    }
}
