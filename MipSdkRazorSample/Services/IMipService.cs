using Microsoft.Extensions.Configuration;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using Microsoft.InformationProtection.File;
using MipSdkRazorSample.Models;

namespace MipSdkRazorSample.Services
{
    public interface IMipService
    {
        public MemoryStream ApplyMipLabel(Stream inputStream, string labelId);
        public string GetDefaultLabel(string userId);
        ContentLabel GetFileLabel(string userId, Stream inputStream);
        public int GetLabelSensitivityValue(string labelGuid);        
        public IList<MipLabel> GetMipLabels(string userId);
        public Stream GetTemporaryDecryptedStream(Stream inputStream, string userId);
        public bool IsLabeledOrProtected(Stream inputStream);
        public bool IsProtected(Stream inputStream);
        public MemoryStream RemoveProtection(Stream inputStream, string fileName, string labelId, string userId);
    }
}
