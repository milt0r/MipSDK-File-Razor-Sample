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
    public class UploadModel : PageModel
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly MipSdkRazorSample.Data.MipSdkRazorSampleContext _context;
        private readonly IMipService _mipApi;
        private readonly string? _userId;

        [BindProperty]
        public IFormFile? Upload { get; set; }

        public UploadModel(MipSdkRazorSample.Data.MipSdkRazorSampleContext context)
        {
             // Change this to read name from POST            
            _context = context;

            // Do I need anything here? Maybe change this to write to blob? New service.
            _azureStorageService = _context.GetService<IAzureStorageService>();
            _mipApi = _context.GetService<IMipService>();
            _userId = _context.GetService<IHttpContextAccessor>().HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Subject.Name;
        }

        public DataPolicy DataPolicy { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<MipLabel> MipLabels { get; set; }
        public string? Result { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            MemoryStream uploadStream = new();

            FileData FileData = new FileData();
                        
            // Fetch policy for downloaded files.
            // If the labeled file is more sensitive that policy, upload is rejected.            
            DataPolicy = _context.DataPolicy.First(d => d.PolicyName == "Upload Policy");

            // Upload file to uploadStream
            if (Upload != null)
            {                
                await Upload.CopyToAsync(uploadStream);
                FileData.FileName = Upload.FileName;
                FileData.Size = Upload.Length;   
                FileData.Owner = _userId;             
            }

            // Make sure we have _mipApi initialized and a userId. 
            if (_mipApi != null && _userId != null)
            {
                uploadStream.Position = 0;

                ContentLabel label;
                                
                // Can probably move all this to a helper. 
                try
                {                    
                    if (_mipApi.IsLabeledOrProtected(uploadStream, FileData.FileName))
                    {
                        // Read the file label.
                        label = _mipApi.GetFileLabel(_userId, uploadStream, FileData.FileName);

                        FileData.LabelId = label.Label.Id;
                        
                        System.Console.WriteLine("**** Read Label: {0}", label.Label.Id);

                        // Check the file label against upload policy.
                        // If file is more sensitive than policy allows, store message in Result and fall to return. 
                        if (_mipApi.GetLabelSensitivityValue(label.Label.Id) > _mipApi.GetLabelSensitivityValue(DataPolicy.MinLabelIdForAction))
                        {
                            if (label.Label.Parent.Id == null)
                                Result = String.Format("Failed to upload file. Service doesn't permit {0}", label.Label.Name);
                            else
                                Result = String.Format("Failed to upload file. Service doesn't permit {0} - {1}", label.Label.Parent.Name, label.Label.Name);
                        }

                        // If we're here, the file passed policy check.
                        else
                        {
                            Stream localStream;
                            localStream = uploadStream;
                            
                            // If it's a protected file, we need to get a decrypted copy. 
                            if (_mipApi.IsProtected(uploadStream, FileData.FileName))
                            {
                                FileData.IsProtected = true;  
                                                              
                                //localStream = _mipApi.GetTemporaryDecryptedStream(uploadStream, _userId);
                                localStream = _mipApi.RemoveProtection(uploadStream, FileData.FileName, _mipApi.GetFileLabel(_userId, uploadStream, FileData.FileName).Label.Id, _userId);
                            }
                            
                            try
                            {
                                localStream.Position = 0;
                                await _azureStorageService.UploadStream(localStream, FileData.FileName);                                
                                Result = String.Format("Successfully uploaded file.");
                            }

                            catch (Exception ex)
                            {
                                Result = String.Format("Failed to upload file with exception {0}", ex.Message);
                            }
                        }
                    }                    
                }

                catch (Microsoft.InformationProtection.Exceptions.NoAuthTokenException)
                {
                    Result = "No auth token provided.";
                }

                catch (Microsoft.InformationProtection.Exceptions.NoPermissionsException)
                {
                    Result = "User doesn't have rights to uploaded file.";
                }

                catch (Microsoft.InformationProtection.Exceptions.AccessDeniedException)
                {
                    Result = "User doesn't have rights to uploaded file.";
                }
            }

            _context.FileData.Add(FileData);
            await _context.SaveChangesAsync();

            //return Page();
            return RedirectToPage("./Index");
        }

        private bool EmployeesExists(int id)
        {
            return _context.Employees.Any(e => e.ID == id);
        }

        /// <summary>
        /// Handles parsing list of employees from Excel input and writing to model. 
        /// </summary>
        /// <param name="EmployeeUpload"></param>
        /// <returns></returns>
        private async Task<bool> UpdateEmployees(List<Employee> EmployeeUpload)
        {
            foreach (var employee in EmployeeUpload)
            {
                if (Employees.Any(e => e.ID == employee.ID))
                {
                    var updatedEmployee = Employees.Where(e => e.ID == employee.ID).Single();

                    updatedEmployee.FirstName = employee.FirstName;
                    updatedEmployee.Surname = employee.Surname;
                    updatedEmployee.Salary = employee.Salary;
                    updatedEmployee.HireDate = employee.HireDate;
                    updatedEmployee.DateOfBirth = employee.DateOfBirth;
                    updatedEmployee.Title = employee.Title;
                    _context.Employees.Update(updatedEmployee);
                }

                else
                {
                    var newEmployee = new Employee();
                    newEmployee.FirstName = employee.FirstName;
                    newEmployee.Surname = employee.Surname;
                    newEmployee.Salary = employee.Salary;
                    newEmployee.HireDate = employee.HireDate;
                    newEmployee.DateOfBirth = employee.DateOfBirth;
                    newEmployee.Title = employee.Title;

                    _context.Employees.Add(newEmployee);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }

                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeesExists(Employees.FirstOrDefault().ID))
                    {
                        return false;
                        //return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return true;
        }
    }
}