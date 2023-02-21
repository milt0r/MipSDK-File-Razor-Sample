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
    public class IndexModel : PageModel
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly MipSdkRazorSample.Data.MipSdkRazorSampleContext _context;
        private readonly IMipService _mipApi;
        private readonly string? _userId;

        public IList<FileData> FileDataList { get; set; }

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public IndexModel(MipSdkRazorSample.Data.MipSdkRazorSampleContext context)
        {
            _context = context;

            _azureStorageService = _context.GetService<IAzureStorageService>();
            _mipApi = _context.GetService<IMipService>();
            _userId = _context.GetService<IHttpContextAccessor>().HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Subject.Name;
        }

        public DataPolicy DataPolicy { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<MipLabel> MipLabels { get; set; }
        public string? Result { get; set; }

        public async Task OnGetAsync()
        {
            FileDataList = await _context.FileData.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            return Page();
        }
    }
}