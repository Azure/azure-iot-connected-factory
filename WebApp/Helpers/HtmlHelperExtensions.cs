using System.Web;
using System.Web.Mvc;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString JavaScriptString(this HtmlHelper htmlHelper, string message)
        {
            return htmlHelper.Raw(HttpUtility.JavaScriptStringEncode(message));
        }
    }
}