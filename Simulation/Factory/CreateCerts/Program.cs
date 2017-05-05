
using System;
using Opc.Ua;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace CreateCerts
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: CreateCerts <OutputPath> <ApplicationName> <ApplicationURI>");
            }
            else
            {
                Console.WriteLine("Output directory: " + args[0]);

                // cleanup previous runs
                try
                {
                    Directory.Delete(args[0] + Path.DirectorySeparatorChar + "certs", true);
                    Directory.Delete(args[0] + Path.DirectorySeparatorChar + "private", true);
                }
                catch (Exception)
                {
                    // do nothing
                }

                // create certs
                string storeType = "Directory";
                string storePath = args[0];
                string password = "password";
                string applicationURI = args[2];
                string applicationName = args[1];
                string subjectName = applicationName;
                List<string> domainNames = null; // not used
                const ushort keySizeInBits = 2048;
                DateTime startTime = DateTime.Now;
                const ushort lifetimeInMonths = 120;
                const ushort hashSizeInBits = 256;
                bool isCA = false;
                X509Certificate2 issuerCAKeyCert = null; // not used

                if (!applicationURI.StartsWith("urn:"))
                {
                    applicationURI = "urn:" + applicationURI;
                }

                CertificateFactory.CreateCertificate(
                    storeType,
                    storePath,
                    password,
                    applicationURI,
                    applicationName,
                    subjectName,
                    domainNames,
                    keySizeInBits,
                    startTime,
                    lifetimeInMonths,
                    hashSizeInBits,
                    isCA,
                    issuerCAKeyCert);

                // rename cert files to something we can copy easily
                DirectoryInfo dir = new DirectoryInfo(args[0] + Path.DirectorySeparatorChar + "certs");
                foreach(FileInfo file in dir.EnumerateFiles())
                {
                    if (file.Extension == ".der")
                    {
                        File.Move(file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + args[1].Replace(" ", "") + file.Extension);
                    }
                }
                dir = new DirectoryInfo(args[0] + Path.DirectorySeparatorChar + "private");
                foreach (FileInfo file in dir.EnumerateFiles())
                {
                    if (file.Extension == ".pfx")
                    {
                        File.Move(file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + args[1].Replace(" ", "") + file.Extension);
                    }
                }
            }
        }
    }
}
