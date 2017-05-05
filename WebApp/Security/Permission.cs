namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security
{
    /// <summary>
    /// Defines various permissions for users (of both the web site and
    /// the REST API).
    /// 
    /// Permissions are assigned to roles, and users are assigned to roles.
    /// If a user has multiple roles, only one role needs to have a 
    /// permission for the user to have the permission.
    /// </summary>
    public enum Permission
    {
        ViewTelemetry,
        ActionAlerts,
        AddOpcServer,
        BrowseOpcServer,
        ControlOpcServer,
        PublishOpcNode,
        DownloadCertificate
    }
}
