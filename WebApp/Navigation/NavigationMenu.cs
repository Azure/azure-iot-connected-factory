using System.Collections.Generic;
using System.Globalization;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using GlobalResources;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Navigation
{
    public class NavigationMenu
    {
        private readonly List<NavigationMenuItem> _navigationMenuItems;

        public NavigationMenu()
        {
            _navigationMenuItems = new List<NavigationMenuItem>()
            {
                new NavigationMenuItem
                {
                    Text = Strings.NavigationMenuSidebarSizeToggle,
                    Action = "Index",
                    Selected = false,
                    Class = "navigation_link_hamburger",
                    MinimumPermission = Permission.ViewTelemetry,
                },
                new NavigationMenuItem
                {
                    Text = Strings.NavigationMenuDashboard,
                    Action = "Index",
                    Controller = "Dashboard",
                    Selected = false,
                    Class = "navigation_link_home",
                    MinimumPermission = Permission.ViewTelemetry,
                },
                new NavigationMenuItem
                {
                    Text = Strings.NavigationMenuBrowser,
                    Action = "Index",
                    Controller = "Browser",
                    Selected = false,
                    Class = "navigation_link_browser",
                    MinimumPermission = Permission.BrowseOpcServer,
                },
                new NavigationMenuItem
                {
                    Text = Strings.NavigationMenuAddGateway,
                    Action = "Index",
                    Controller = "AddGateway",
                    Selected = false,
                    Class = "navigation_link_addgateway",
                    MinimumPermission = Permission.AddOpcServer,
                },
                new NavigationMenuItem
                {
                    Text = Strings.NavigationMenuAddServer, 
                    Action = "Index",
                    Controller = "AddOpcServer",
                    Selected = false,
                    Class = "navigation_link_add_opc_server",
                    MinimumPermission = Permission.AddOpcServer,
                }
            };
        }

        public List<NavigationMenuItem> NavigationMenuItems
        {
            get
            {
                // only show menu items the user has permission for
                var visibleItems = new List<NavigationMenuItem>();
                foreach (var menuItem in _navigationMenuItems)
                {
                    if (PermsChecker.HasPermission(menuItem.MinimumPermission))
                    {
                        visibleItems.Add(menuItem);
                    }
                }

                return visibleItems;
            }
        }

        public NavigationMenuItem Select(string controllerName, string actionName)
        {
            foreach (var navigationMenuItem in _navigationMenuItems)
            {
                if (navigationMenuItem.Controller == controllerName && navigationMenuItem.Action == actionName)
                {
                    navigationMenuItem.Selected = true;
                    navigationMenuItem.Class = string.Format(CultureInfo.InvariantCulture, "{0} {1}", navigationMenuItem.Class, "selected");
                    return navigationMenuItem;
                }
            }

            return null;
        }
    }
}
