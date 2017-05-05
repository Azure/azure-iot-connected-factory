
using Opc.Ua.Configuration;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace Opc.Ua.Sample.Simulation
{
    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (ask)
            {
                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
                }
                catch
                {
                    // intentionally fall through
                }
            }
            return await Task.FromResult(true);
        }
    }

    public class Program
    {
        public static bool GenerateAlerts { get; set; }
        public static double PowerConsumption { get; set; }
        public static ulong CycleTime { get; set; }

        public static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                throw new ArgumentException("You must specify a station name, and base address, power consumption (in [kW]). cycle time default (in [s]) and if alerts should be generated (yes/no) as command line arguments!");
            }

            try
            {
                Task t = ConsoleServer(args);
                t.Wait();
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Utils.Trace("Exception: {0}", ex.Message);
            }
        }

        private static async Task ConsoleServer(string[] args)
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();

            string stationName = args[0].ToLowerInvariant();
            Uri stationUri = new Uri(args[1]);
            string stationPath = stationUri.AbsolutePath.TrimStart('/').ToLowerInvariant();
            
            application.ApplicationName = stationUri.DnsSafeHost.ToLowerInvariant();
            application.ConfigSectionName = "Opc.Ua.Station";
            application.ApplicationType = ApplicationType.Server;

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);
            if (config == null)
            {
                throw new Exception("Application configuration is null!");
            }

            // replace our placeholders with specific settings from the commandline
            config.ApplicationName = stationUri.DnsSafeHost.ToLowerInvariant();
            config.ApplicationUri = "urn:" + stationName + ":" + stationPath.Replace("/", ":");
            config.ProductUri = "http://contoso.com/UA/" + stationName;
            if (config.SecurityConfiguration.ApplicationCertificate.Certificate == null)
            {
                // create a cert 
                config.SecurityConfiguration.ApplicationCertificate.SubjectName = application.ApplicationName;
            }
            config.ServerConfiguration.BaseAddresses[0] = stationUri.ToString();

            // PowerConsumption in [kW], cycle time in [s]
            PowerConsumption = ulong.Parse(args[2], NumberStyles.Integer);
            CycleTime = ulong.Parse(args[3], NumberStyles.Integer);
            GenerateAlerts = (args[4] == "yes") ? true : false;

            // check the application certificate.
            await application.CheckApplicationInstanceCertificate(false, 0);

            // start the server.
            await application.Start(new FactoryStationServer());

            Console.WriteLine("Server started. Press any key to exit.");

            try
            {
                Console.ReadKey(true);
            }
            catch
            {
                // wait forever if there is no console
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
