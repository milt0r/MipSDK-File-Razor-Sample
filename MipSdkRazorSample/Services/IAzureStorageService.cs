using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using MipSdkRazorSample.Models;

namespace MipSdkRazorSample.Services
{
    public interface IAzureStorageService
    {
        public Task DownloadToStream(Stream fileStream, string fileName);
        public Task<List<string>> ListBlobsAsync(int? segmentSize);
        public Task UploadStream(Stream fileStream, string fileName);
    }
}