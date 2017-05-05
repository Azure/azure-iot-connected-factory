using System.Web.Optimization;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{

    public sealed class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/scripts")
                .Include(
                    "~/Scripts/jquery-{version}.js",
                    "~/Scripts/bootstrap.min.js",
                    "~/Scripts/jquery.validate.js",
                    "~/Scripts/jquery.unobtrusive-ajax.js",
                    "~/Scripts/jquery.datatables*",
                    "~/Scripts/jquery-ui-{version}.js",
                    "~/Scripts/jquery-ui-i18n.js",
                    "~/Scripts/jquery.signalR-{version}.js",
                    "~/Scripts/globalize.js",
                    "~/Scripts/js.cookie-{version}.js",
                    "~/Scripts/moment-with-locales.js",
                    "~/Scripts/bootstrap-select-{version}.js"
                ));

            bundles.Add(new StyleBundle("~/content/css")
                .Include(
                    "~/content/themes/base/core.css",
                    "~/content/themes/base/dialog.css",
                    "~/content/themes/PCS/style.css",
                    "~/content/bootstrap.css",
                    "~/content/main.css",
                    "~/content/bootstrap-select.min.css"
                ));
        }
    }
}
