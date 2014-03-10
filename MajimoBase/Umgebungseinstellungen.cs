using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace MajimoBase
{
    internal class Umgebungseinstellungen
    {
        private const string XmlFileName = "majimo-settings.xml";
        private const string XsdFileName = "majimo-settings.xsd";

        private readonly StringBuilder _validationErrors = new StringBuilder();

        public string BongUser { get; private set; }
        public string BongPassword { get; private set; }
        public string MySqlHost { get; private set; }
        public string MySqlSchema { get; private set; }
        public string MySqlUser { get; private set; }
        public string MySqlPassword { get; private set; }
        public string RecordingLibraryRootPath { get; private set; }
        public string LoggingPath { get; private set; }
        public string LoggingBaseName { get; private set; }
        public int LoggingLimitDays { get; private set; }
        public int LoggingLimitBytes { get; private set; }
        public int LoggingMaxFileCount { get; private set; }

        public Umgebungseinstellungen()
        {
            var settingsPath = LocateSettingFile();

            if (settingsPath != null)
            {
                OpenAndValidateSettingsFile(settingsPath);
            }
            else throw new Exception("Die Konfigurationsdatei wurde weder im Anwendungs-, noch im Home-Verzeichnis gefunden");
        }

        private static string LocateSettingFile()
        {
            var curdir = new System.IO.DirectoryInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Parent;

            while (curdir != null)
            {
                if (curdir.GetFiles(XmlFileName, SearchOption.TopDirectoryOnly).Any() && curdir.GetFiles(XsdFileName, SearchOption.TopDirectoryOnly).Any())
                {
                    return curdir.FullName;
                }
                else curdir = curdir.Parent;
            }

            var homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                            Environment.OSVersion.Platform == PlatformID.MacOSX)
                            ? Environment.GetEnvironmentVariable("HOME")
                            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            Debug.Assert(homePath != null, "homePath is null");
            return SearchSettingsFileRecursively(homePath);
        }

        private static string SearchSettingsFileRecursively(string path)
        {
            string retval = null;
            var curdir = new DirectoryInfo(path);

            if (curdir.GetFiles(XmlFileName, SearchOption.TopDirectoryOnly).Any() && curdir.GetFiles(XsdFileName, SearchOption.TopDirectoryOnly).Any())
            {
                retval = curdir.FullName;
            }
            else
            {
                foreach (string subFolderPath in Directory.GetDirectories(path))
                {
                    try
                    {
                        retval = SearchSettingsFileRecursively(subFolderPath);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // geschützte Verzeichnisse überspringen
                    }

                    if (retval != null)
                        break;
                }
            }
            return retval;
        }

        private void OpenAndValidateSettingsFile(string settingsPath)
        {
            // Create a schema validating XmlReader.
            var settings = new XmlReaderSettings();
            settings.Schemas.Add(null, Path.Combine(settingsPath, XsdFileName));
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationEventHandler);
            settings.ValidationFlags = settings.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationType = ValidationType.Schema;

            var settingsXmlPath = Path.Combine(settingsPath, XmlFileName);
            var reader = XmlReader.Create(settingsXmlPath, settings);

            var document = new XmlDocument();
            document.Load(reader);

            if (_validationErrors.Length == 0)
            {
                BongUser = GetNodeText(document, "/Settings/Bong/@User");
                BongPassword = GetNodeText(document, "/Settings/Bong/@Password");
                MySqlHost = GetNodeText(document, "/Settings/MySql/@Host");
                MySqlSchema = GetNodeText(document, "/Settings/MySql/@Schema");
                MySqlUser = GetNodeText(document, "/Settings/MySql/@User");
                MySqlPassword = GetNodeText(document, "/Settings/MySql/@Password");
                RecordingLibraryRootPath = GetNodeText(document, "/Settings/Dateisystem/RecordingLibraryRoot/@Pfad");
                LoggingPath = GetNodeText(document, "/Settings/Dateisystem/LoggingTargets/@Pfad");
                LoggingBaseName = GetNodeText(document, "/Settings/Dateisystem/LoggingTargets/@Basisname");
                LoggingLimitDays = int.Parse(GetNodeText(document, "/Settings/Dateisystem/LoggingTargets/@LimitTage"));
                LoggingLimitBytes = int.Parse(GetNodeText(document, "/Settings/Dateisystem/LoggingTargets/@LimitInBytes"));
                LoggingMaxFileCount = int.Parse(GetNodeText(document, "/Settings/Dateisystem/LoggingTargets/@Anzahl"));
            }
            else throw new Exception(String.Format("Die Konfigurationsdatei {1} ist falsch aufgebaut\n{0}", _validationErrors.ToString(), settingsXmlPath));
        }

        private void ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                _validationErrors.AppendFormat("Validierungswarnung in Zeile {0} an Position {1}: {2}\n", args.Exception.LineNumber, args.Exception.LinePosition, args.Message);
            else if (args.Severity == XmlSeverityType.Error)
                _validationErrors.AppendFormat("Validierungsfehler in Zeile {0} an Position {1}: {2}\n", args.Exception.LineNumber, args.Exception.LinePosition, args.Message);
        }

        private static String GetNodeText(XmlDocument doc, String xpathExpression)
        {
            XmlNodeList nodes = doc.SelectNodes(xpathExpression);

            if (nodes == null || nodes.Count != 1)
                return null;

            String result = nodes[0].InnerXml;

            if (result != null && result.Trim().Equals(""))
                result = null;

            return result;
        }
    }
}
