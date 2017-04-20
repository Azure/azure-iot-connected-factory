using System;
using System.Globalization;
using System.IO;
using System.Web.SessionState;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration
{
    public static class EnvironmentDescription
    {
        static XmlDocument _document = null;
        static string _fileName = null;
        const string _valueAttributeName = "value";
        const string _settingXpath = "//setting[@name='{0}']";
        public static bool IsInitialized { get; set; }

        public static void Init(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            _fileName = fileName;
            _document = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(fileName))
            {
                _document.Load(reader);
            }
            IsInitialized = true;
        }

        public static string GetSetting(string settingName, bool errorOnNull = true)
        {
            if (string.IsNullOrEmpty(settingName))
            {
                throw new ArgumentNullException(nameof(settingName));
            }

            if (string.IsNullOrEmpty(_fileName))
            {
                throw new Exception("The setting file is not yet opened.");
            }

            string result = string.Empty;
            XmlNode node = GetSettingNode(settingName.Trim());
            if (node != null)
            {
                result = node.Attributes[_valueAttributeName].Value;
            }
            else
            {
                if (errorOnNull)
                {
                    var message = string.Format(CultureInfo.InvariantCulture, "{0} was not found", settingName);
                    throw new ArgumentException(message);
                }
            }
            return result;
        }

        private static XmlNode GetSettingNode(string settingName)
        {
            string xpath = string.Format(CultureInfo.InvariantCulture, _settingXpath, settingName);
            return _document.SelectSingleNode(xpath);
        }
    }
}
