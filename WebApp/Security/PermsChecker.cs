
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security
{
    /// <summary>
    /// Helper class for checking permissions in code.
    /// </summary>
    public static class PermsChecker
    {
        private static readonly RolePermissions _rolePermissions;

        static PermsChecker()
        {
            _rolePermissions = new RolePermissions();
        }

        /// <summary>
        /// Call this method in code to determine if a user has a given permission
        /// </summary>
        public static bool HasPermission(Permission permission)
        {
#if GRANT_FULL_ACCESS_PERMISSIONS
            return true;
#else
            return _rolePermissions.HasPermission(permission, new HttpContextWrapper(HttpContext.Current));
#endif
        }

        /// <summary>
        /// Call this method in code to determine if a user has a given permissions
        /// </summary>
        public static bool HasPermission(List<Permission> permissions) 
        {
#if GRANT_FULL_ACCESS_PERMISSIONS
            return true;
#else
            var httpContext = new HttpContextWrapper(HttpContext.Current);

            if (permissions == null || !permissions.Any())
            {
                return true;
            }

            // return true only if the user has ALL permissions
            return permissions
                    .Select(p => _rolePermissions.HasPermission(p, httpContext))
                    .All(val => val == true);
#endif
        }
    }
}