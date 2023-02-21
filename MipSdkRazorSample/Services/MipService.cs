using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;
using MipSdkRazorSample.Models;
using NuGet.Configuration;

namespace MipSdkRazorSample.Services
{
    public class MipService : IMipService
    {
        private readonly AuthDelegateImpl _authDelegate;
        private readonly IConfiguration _configuration;
        private readonly string _defaultEngineId;
        private readonly IFileProfile _fileProfile;
        private List<IFileEngine> _fileEngines;
        private readonly MipContext _mipContext;

        /// <summary>
        /// MipService constructor. Creates appInfo, initializes MIP, creates Profile.
        /// </summary>
        /// <param name="configuration"></param>
        public MipService(IConfiguration configuration)
        {
            _configuration = configuration;
            _fileEngines = new List<IFileEngine>();

            // Create application info using config settings. 
            ApplicationInfo appInfo = new ApplicationInfo()
            {
                ApplicationId = _configuration.GetSection("AzureAd").GetValue<string>("ClientId"),
                ApplicationName = _configuration.GetSection("MipConfig").GetValue<string>("ApplicationName"),
                ApplicationVersion = _configuration.GetSection("MipConfig").GetValue<string>("Version")
            };

            // Set the default engine Id to the clientId. This is what we'll use to cache any engines that aren't delegated or on-behalf-of.
            _defaultEngineId = _configuration.GetSection("AzureAd").GetValue<string>("ClientId");

            // Initialize the auth delegate. 
            _authDelegate = new AuthDelegateImpl(_configuration);

            // Initialize MIP, create config and MipContext.
            // *** ONLY ONE MIP CONTEXT SHOULD EXIST PER APPLICATION ***
            MIP.Initialize(MipComponent.File);
            MipConfiguration mipConfig = new(appInfo, "mip_data", Microsoft.InformationProtection.LogLevel.Trace, false);
            _mipContext = MIP.CreateMipContext(mipConfig);

            // Initialize FileProfileSettings and FileProfile.
            // *** ONLY ONE PROFILE SHOULD EXIST PER APPLICATION ***
            FileProfileSettings profileSettings = new FileProfileSettings(_mipContext, CacheStorageType.InMemory, new ConsentDelegateImpl());
            _fileProfile = MIP.LoadFileProfileAsync(profileSettings).Result;
        }

        /// <summary>
        /// Applies the specified labelId to the provided inputStream. Returns the labeled stream.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="labelId"></param>
        /// <returns></returns>
        public MemoryStream ApplyMipLabel(Stream inputStream, string labelId, string fileName)
        {
            IFileEngine engine = GetEngine(_defaultEngineId);

            // Create a handler with a hardcoded file name, using the input stream.
            IFileHandler handler = engine.CreateFileHandlerAsync(inputStream, fileName, true).GetAwaiter().GetResult();

            LabelingOptions options = new()
            {
                AssignmentMethod = AssignmentMethod.Auto
            };

            // Set the label on the handler.
            handler.SetLabel(engine.GetLabelById(labelId), options, new ProtectionSettings());

            MemoryStream outputStream = new MemoryStream();
            
            // Commit the change and write to the outputStream. 
            handler.CommitAsync(outputStream).GetAwaiter().GetResult();
            return outputStream;
        }

        public MemoryStream ApplyMipProtectionFromPL(Stream inputStream, string serializedPublishingLicense, string fileName)
        {
            // Need to talk to engineering about where the ProtectionDescriptorBuilder from PL went. 
            throw new NotImplementedException();

            IFileEngine engine = GetEngine(_defaultEngineId);

            // Create a handler with a hardcoded file name, using the input stream.
            IFileHandler handler = engine.CreateFileHandlerAsync(inputStream, fileName, true).GetAwaiter().GetResult();

            // Set the label on the handler.
            //handler.SetLabel(engine.GetLabelById(labelId), options, new ProtectionSettings());

            MemoryStream outputStream = new MemoryStream();

            // Commit the change and write to the outputStream. 
            handler.CommitAsync(outputStream).GetAwaiter().GetResult();
            return outputStream;
        }

        /// <summary>
        /// Fetches the default label for a given user. 
        /// </summary>
        /// <param name="userId">Default label will be fetched for provided userId.</param>
        public string GetDefaultLabel(string userId)
        {
            IFileEngine engine;

            engine = GetDelegatedEngine(userId);

            return engine.DefaultSensitivityLabel.Id ?? string.Empty;
        }

        /// <summary>
        /// Gets the file label from the inputStream. Users userId parameter to perform the operation in the context of input userId. 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public ContentLabel GetFileLabel(string userId, Stream inputStream, string fileName)
        {
            // Fetch an engine for the provided user. If the user has rights to the file, method will return label. 
            // If the user doesn't have rights, it'll throw AccessDeniedException.
            IFileEngine engine = GetDelegatedEngine(userId);
            IFileHandler handler;

            try
            {
                handler = engine.CreateFileHandlerAsync(inputStream, fileName, true).Result;
            }

            catch (Microsoft.InformationProtection.Exceptions.AccessDeniedException ex)
            {
                throw ex;
            }

            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }

            return handler.Label;
        }

        /// <summary>
        /// Get the integer sensitivity value from the specified label. Used to evaluate policy for upload.
        /// </summary>
        /// <param name="labelGuid"></param>
        /// <returns></returns>
        public int GetLabelSensitivityValue(string labelGuid)
        {
            IFileEngine engine = GetEngine(_defaultEngineId);

            return engine.GetLabelById(labelGuid).Sensitivity;
        }

        /// <summary>
        /// Get all labels for the specified userId.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IList<MipLabel> GetMipLabels(string userId)
        {
            IFileEngine engine;

            engine = GetDelegatedEngine(userId);

            List<MipLabel> outputList = new List<MipLabel>();

            foreach (var label in engine.SensitivityLabels)
            {
                if (label.IsActive)
                {
                    outputList.Add(new MipLabel()
                    {
                        Id = label.Id,
                        Name = label.Name,
                        Sensitivity = label.Sensitivity
                    });
                }

                if (label.Children.Count() > 0)
                {
                    foreach (var child in label.Children)
                    {
                        if (child.IsActive)
                        {
                            outputList.Add(new MipLabel()
                            {
                                Id = child.Id,
                                Name = String.Join(" - ", label.Name, child.Name),
                                Sensitivity = child.Sensitivity
                            });
                        }
                    }
                }
            }

            return outputList;
        }

        public string GetSerializedPublishingLicense(Stream inputStream, string fileName)
        {
            if (FileHandler.IsProtected(inputStream, fileName, _mipContext))
            {
                return FileHandler.GetSerializedPublishingLicense(inputStream, fileName, _mipContext).ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets a decrypted copy of the protected input stream for the specified user. 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Microsoft.InformationProtection.Exceptions.AccessDeniedException"></exception>
        /// <exception cref="Exception"></exception>
        public Stream GetTemporaryDecryptedStream(Stream inputStream, string userId, string fileName)
        {
            // Create a delegated engine and a new handler. 
            IFileEngine engine = GetDelegatedEngine(userId);
            IFileHandler handler = engine.CreateFileHandlerAsync(inputStream, fileName, true).GetAwaiter().GetResult();

            // Validate that input is protected.
            if (handler.Protection != null)
            {
                // Validate that user has rights to remove protection from the file.
                // Rights enforcement is up to the application implementing the SDK!                
                if (handler.Protection.AccessCheck(Rights.Extract))
                {
                    // If user has Extract right, return decrypted copy. Otherwise, throw exception. 
                    return handler.GetDecryptedTemporaryStreamAsync().GetAwaiter().GetResult();
                }
                throw new Microsoft.InformationProtection.Exceptions.AccessDeniedException("User lacks EXPORT right.");
            }
            else
            {
                throw new Exception("File Not Protected");
            }
        }

        /// <summary>
        /// Returns true if input stream is labeled or protected. 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public bool IsLabeledOrProtected(Stream inputStream, string fileName)
        {
            IFileStatus status = FileHandler.GetFileStatus(inputStream, fileName, _mipContext);
            bool isLabeled = status.IsLabeled();
            bool isProtected = status.IsProtected();

            return (isLabeled || isProtected);
        }

        /// <summary>
        /// Returns protection status of input stream.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public bool IsProtected(Stream inputStream, string fileName)
        {
            IFileStatus status = FileHandler.GetFileStatus(inputStream, fileName, _mipContext);
            bool result = status.IsProtected();
            return result;
        }

        public MemoryStream RemoveProtection(Stream inputStream, string fileName, string labelId, string userId)
        {
            // Get a delegated engine because we want the access check to be in the context of the user.
            IFileEngine engine = GetDelegatedEngine(userId);

            // Create a file handler.
            IFileHandler handler = engine.CreateFileHandlerAsync(inputStream, fileName, true).GetAwaiter().GetResult();

            // Validate that the handler is protected. If not, throw.
            if (handler.Protection != null)
            {
                // Perform an access check to validate that the user has rights to remove protection.
                // This could probably be in a more generic helper function. 
                if (handler.Protection.AccessCheck(Rights.Export) || handler.Protection.AccessCheck(Rights.Owner))
                {
                    handler.RemoveProtection();
                }
                else
                {
                    // User doesn't have rights to remove access, so throw access denied. 
                    throw new Microsoft.InformationProtection.Exceptions.AccessDeniedException("User doesn't have rights to remove protection.");
                }
            }
            else
            {
                throw new Microsoft.InformationProtection.Exceptions.BadInputException("File not protected.");
            }

            MemoryStream outputStream = new MemoryStream();

            // Commit the change and write to the outputStream. 
            handler.CommitAsync(outputStream).GetAwaiter().GetResult();
            return outputStream;
        }

        /// <summary>
        /// Get an engine of the specified engineId. If it doesn't exist in app cache, fetch from MIP cache or create a new one. 
        /// </summary>
        /// <param name="engineId"></param>
        /// <returns></returns>
        private IFileEngine GetEngine(string engineId)
        {
            IFileEngine engine;

            // Check for existing engine. If it doesn't exist in cache, create a new one. 
            if (_fileEngines.Count == 0 || _fileEngines.Find(e => e.Settings.EngineId == engineId) == null)
            {
                FileEngineSettings settings = new(engineId, _authDelegate, "", "en-US")
                {
                    Cloud = Cloud.Commercial // Hard code commercial cloud.             
                };

                // Get engine and add to cache.
                engine = _fileProfile.AddEngineAsync(settings).Result;
                _fileEngines.Add(engine);
            }

            else
            {
                // Fetch engine from cache and return.
                engine = _fileEngines.Where(e => e.Settings.EngineId == engineId).First();
            }

            return engine;
        }

        /// <summary>
        /// Creates a delegated FileEngine for the specified user. All operations will be performed as that user. 
        /// Requires the Content.DelegatedReader and Content.DelegatedWriter permission.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private IFileEngine GetDelegatedEngine(string userId)
        {
            IFileEngine engine;

            // Check cache for existing engine. If it doesn't exist, create one.
            if (_fileEngines.Count == 0 || _fileEngines.Where(e => e.Settings.EngineId == userId).Count() == 0)
            {
                // Fix the engineId
                FileEngineSettings settings = new FileEngineSettings(userId, _authDelegate, "", "en-US")
                {
                    Cloud = Cloud.Commercial,
                    DelegatedUserEmail = userId
                };

                // Add async? 
                engine = _fileProfile.AddEngineAsync(settings).Result;
                _fileEngines.Add(engine);
            }
            else
            {
                // Fetch existing engine from cache.
                engine = _fileEngines.Where(e => e.Settings.EngineId == userId).First();
            }

            return engine;
        }
    }
}
