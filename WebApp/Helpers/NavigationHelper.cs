using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Navigation;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers
{
    /// <summary>
    /// A collection of helper methods, related to navigation.
    /// </summary>
    public static class NavigationHelper
    {
        /// <summary>
        /// Applies navigation menu item selection using the current action name.
        /// </summary>
        /// <param name="menuItems">
        /// The NavigationMenuItems, on which to apply selection.
        /// </param>
        /// <param name="controllerName">
        /// The name of the action's controller.
        /// </param>
        /// <param name="actionName">
        /// The name of the current action.
        /// </param>
        public static void ApplySelection(
            IEnumerable<NavigationMenuItem> menuItems,
            string controllerName,
            string actionName)
        {
            if (menuItems == null)
            {
                throw new ArgumentNullException("menuItems");
            }

            menuItems = menuItems.Where(t => (t != null) && (t.Controller == controllerName));
            foreach (var navigationMenuItem in menuItems)
            {
                if ( navigationMenuItem.Action == actionName)
                {
                    navigationMenuItem.Selected = true;
                    navigationMenuItem.Class = string.Format(CultureInfo.InvariantCulture, "{0} {1}", navigationMenuItem.Class, "selected");
                }
            }
        }

        /// <summary>
        /// Applies submenu navigation menu item selection using the current action name.
        /// </summary>
        /// <param name="menuItems">
        /// The NavigationMenuItems, on which to apply selection.
        /// </param>
        /// <param name="controllerName">
        /// The name of the action's controller.
        /// </param>
        /// <param name="actionName">
        /// The name of the current action.
        /// </param>
        public static void ApplySubmenuSelection(
            IEnumerable<NavigationMenuItem> menuItems,
            string controllerName,
            string actionName)
        {
            if (menuItems == null)
            {
                throw new ArgumentNullException("menuItems");
            }

            menuItems = menuItems.Where(t => t != null);
            foreach (var navigationMenuItem in menuItems)
            {
                if (navigationMenuItem.Controller == controllerName && navigationMenuItem.Action == actionName)
                {
                    navigationMenuItem.Selected = true;
                    navigationMenuItem.Class = string.Format(CultureInfo.InvariantCulture, "{0} {1}", navigationMenuItem.Class, "selected");
                }
            }
        }
    }
}