using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using MipSdkRazorSample.Models;
using NuGet.Configuration;

namespace MipSdkRazorSample.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _secret;
        private readonly string _containerName;
        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;

        /// <summary>
        /// AzureStorageService constructor.
        /// </summary>
        /// <param name="configuration"></param>
        public AzureStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secret = _configuration["App:StorageSecret"];
            _connectionString = _configuration.GetSection("AzureStorage").GetValue<string>("ConnectionString");
            _connectionString = _connectionString.Replace("{ACCOUNTKEY}", _secret);
            _containerName = string.Format("container-{0}", _configuration.GetSection("AzureStorage").GetValue<string>("ContainerSuffix"));
            _blobServiceClient = new BlobServiceClient(_connectionString);

            Console.WriteLine("***** ConnectionString: {0}", _connectionString);

            CreateContainerAsync(_blobServiceClient).GetAwaiter().GetResult();

            _blobContainerClient = new BlobContainerClient(_connectionString, _containerName);
        }

        public async Task UploadStream(Stream fileStream, string fileName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, true);
            fileStream.Close();
        }

        public async Task DownloadToStream(Stream fileStream, string fileName)
        {
            var blobClient = new BlobClient(_connectionString, _containerName, fileName);
            await blobClient.DownloadToAsync(fileStream);
            //fileStream.Close();
        }

        public async Task<List<string>> ListBlobsAsync(int? segmentSize)
        {
            List<string> blobList = new List<string>();

            try
            {
                // Call the listing operation and return pages of the specified size.
                var resultSegment = _blobContainerClient.GetBlobsAsync()
                    .AsPages(default, segmentSize);

                // Enumerate the blobs returned for each page.
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        blobList.Add(blobItem.Name);
                        //Console.WriteLine("Blob name: {0}", blobItem.Name);
                    }                    
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            return blobList;
        }

        private async Task<BlobContainerClient> CreateContainerAsync(BlobServiceClient blobServiceClient)
        {
            // Name the sample container based on new GUID to ensure uniqueness.
            // The container name must be lowercase.

            try
            {
                // Create the container
                BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(_containerName);

                if (await container.ExistsAsync())
                {
                    Console.WriteLine("Created container {0}", container.Name);
                    return container;
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
            }

            return null;
        }
    }
}