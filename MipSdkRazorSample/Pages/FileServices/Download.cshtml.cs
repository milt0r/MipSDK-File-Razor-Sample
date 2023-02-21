
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.InformationProtection;
using MipSdkRazorSample.Models;
using MipSdkRazorSample.Services;
using System.Linq;
using System.Security.Claims;

namespace MipSdkRazorSample.Pages.FileServices
{
    public class DownloadModel : PageModel
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly MipSdkRazorSample.Data.MipSdkRazorSampleContext _context;
        private readonly IMipService _mipApi;
        private readonly string? _userId;

        public FileData FileData { get; set; }

        public DownloadModel(MipSdkRazorSample.Data.MipSdkRazorSampleContext context)
        {
            _context = context;

            _azureStorageService = _context.GetService<IAzureStorageService>();
            _mipApi = _context.GetService<IMipService>();
            _userId = _context.GetService<IHttpContextAccessor>().HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Subject.Name;
        }

        public DataPolicy DataPolicy { get; set; }

        public IList<MipLabel> MipLabels { get; set; }
        public string? Result { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            FileData = await _context.FileData.FirstOrDefaultAsync(m => m.ID == id);

            if (FileData == null)
            {
                throw new Exception("Something broke.");
            }

            // TODO: Add access check to validate that downloader is owner. 

            using (MemoryStream fileStream = new())
            {
                // Get file from blob, put in fileStream
                await _azureStorageService.DownloadToStream(fileStream, FileData.FileName);

                using (MemoryStream mipStream = _mipApi.ApplyMipLabel(fileStream, FileData.LabelId, FileData.FileName))
                {
                    mipStream.Position = 0;
                    return File(mipStream.ToArray(), "application/pdf", FileData.FileName);
                }
            }
        }
    }
}