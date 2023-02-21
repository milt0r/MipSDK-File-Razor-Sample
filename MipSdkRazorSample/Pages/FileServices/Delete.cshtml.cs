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
    public class DeleteModel : PageModel
    {
        private readonly MipSdkRazorSample.Data.MipSdkRazorSampleContext _context;
        private readonly IAzureStorageService _azureStorageService;

        public DeleteModel(MipSdkRazorSample.Data.MipSdkRazorSampleContext context)
        {
            _context = context;            
            _azureStorageService = _context.GetService<IAzureStorageService>();
        }

        [BindProperty]
        public FileData FileItem { get; set; }
        
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            FileItem = await _context.FileData.FindAsync(id);

            if (FileItem != null)
            {
                _context.FileData.Remove(FileItem);
                // add delete to blob service
                //await _azureStorageService.
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
