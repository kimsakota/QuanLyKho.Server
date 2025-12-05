using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Linq;

namespace QuanLyKho.Server.Controllers
{
    public class DocsController : Controller
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiExplorer;

        public DocsController(IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            _apiExplorer = apiExplorer;
        }

        [HttpGet("/docs")] // Đường dẫn truy cập sẽ là domain/docs
        public IActionResult Index()
        {
            // Lấy tất cả các API endpoint, nhóm theo Controller name
            var apiGroups = _apiExplorer.ApiDescriptionGroups.Items
                .SelectMany(g => g.Items)
                .GroupBy(x => x.ActionDescriptor.RouteValues["controller"])
                .OrderBy(g => g.Key)
                .ToList();

            return View(apiGroups);
        }
    }
}