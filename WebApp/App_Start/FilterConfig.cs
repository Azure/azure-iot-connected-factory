using System.Web.Mvc;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Filters;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ErrorHandlingFilter());
            filters.Add(new AuthorizeAttribute());
        }
    }
}
