using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Net;
using Microsoft.Graph;

namespace MipSdkRazorSample.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
    public class IndexModel : PageModel
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;            
        }        
    }
}