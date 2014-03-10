using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MajimoBase
{
    public class Umgebung
    {
        public static string VersionInfo { get { return @"0.1"; } }

        public class WebService
        {
            public string User { get; private set; }
            public string Password { get; private set; }

            public WebService(string user, string password)
            {
                User = user;
                Password = password;

                if (String.IsNullOrWhiteSpace(User))
                    throw new Exception("Der Bong-Benutzername wurde nicht angegeben");

                if (String.IsNullOrWhiteSpace(Password))
                    throw new Exception("Das Bong-Password wurde nicht angegeben");
            }
        }

        public static WebService Web;

        public class Database
        {
            public string Host { get; private set; }
            public string Schema { get; private set; }
            public string User { get; private set; }
            public string Password { get; private set; }

            public Database(string host, string schema, string user, string password)
            {
                Host = host;
                Schema = schema;
                User = user;
                Password = password;

                if (String.IsNullOrWhiteSpace(Host))
                    throw new Exception("Der MySql-Host wurde nicht angegeben");

                if (String.IsNullOrWhiteSpace(Schema))
                    throw new Exception("Das MySql-Schema wurde nicht angegeben");

                if (String.IsNullOrWhiteSpace(User))
                    throw new Exception("Der MySql-Benutzername wurde nicht angegeben");

                if (String.IsNullOrWhiteSpace(Password))
                    throw new Exception("Das MySql-Password wurde nicht angegeben");
            }
        }
        public static Database Tab;

        public class FileSystem
        {
            public string LibraryRoot { get; private set; }

            public FileSystem(string libraryRoot)
            {
                LibraryRoot = libraryRoot;

                if (String.IsNullOrWhiteSpace(LibraryRoot) || !Directory.Exists(LibraryRoot))
                    throw new Exception(String.Format("Das Wurzelverzeichnis der Aufzeichnungen {0} existiert nicht", LibraryRoot));
            }
        }
        public static FileSystem Dir;

        public class Logger
        {
            private readonly string _logFullName;

            public void Dump(string format, params Object[] args)
            {
                WriteMessage(String.Format("D: " + format, args));
            }

            public void Info(string format, params Object[] args)
            {
                WriteMessage(String.Format("I: " + format, args));
            }

            public void Warning(string format, params Object[] args)
            {
                WriteMessage(String.Format("W: " + format, args));
            }

            public void Error(string format, params Object[] args)
            {
                WriteMessage(String.Format("E: " + format, args));
            }

            private void WriteMessage(string message)
            {
                using (var fw = new StreamWriter(_logFullName, true, Encoding.UTF8))
                {
                    fw.WriteLine(message);
                }
            }

            public Logger(string basePath, string baseName, int limitDays, int limitBytes, int maxFileCount)
            {

                if (String.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
                    throw new Exception(String.Format("Das Protokollverzeichnis {0} existiert nicht", basePath));

                if (String.IsNullOrWhiteSpace(baseName))
                    throw new Exception("Der Stammname des Protokolls wurde nicht angegeben");

                if (limitDays < 0 || 120 < limitDays)
                    throw new Exception(String.Format("Der Wert {0} ist für das Tagelimit des Protokolls nicht zulässig (0 zum deaktivieren oder 1..120)", limitDays));

                if (limitBytes != 0 && (limitBytes < 4096 || 4194304 < limitBytes))
                    throw new Exception(String.Format("Der Wert {0} ist für das Größenlimit des Protokolls nicht zulässig (0 zum deaktivieren oder 4096 (4 KB) .. 4194304 (4 MB))", limitBytes));

                if (maxFileCount < 1 || 10 < maxFileCount)
                    throw new Exception(String.Format("Der Wert {0} ist für die Anzahl der Protokolldateien nicht zulässig (1..10)", maxFileCount));

                var chrono =
                    (from eintrag in Directory.EnumerateFiles(basePath, baseName + "_????-??-??_????.log")
                    where Regex.Match(eintrag, "^*" + baseName + "_20[1-6][0-9]-[01][0-9]-[0-3][0-9]_[0-2][0-9][0-5][0-9]\\.log$").Success
                    orderby eintrag descending
                    select eintrag).ToList();

                var createNewLog = true;

                if (chrono.Any())
                {
                    createNewLog = false;

                    if (limitBytes != 0)
                    { 
                        var anzBytes = new FileInfo(chrono[0]).Length;

                        createNewLog = (limitBytes < anzBytes);
                    }

                    if (limitDays != 0 && !createNewLog)
                    {
                        var regex = new Regex("^*_(?<Jahr>20[1-6][0-9])-(?<Monat>[01][0-9])-(?<Tag>[0-3][0-9])_(?<Stunde>[0-2][0-9])(?<Minute>[0-5][0-9])\\.log$"
                                             , RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        var tm = regex.Matches(chrono[0])[0];
                        var dt = new DateTime(int.Parse(tm.Groups["Jahr"].Value)
                                             , int.Parse(tm.Groups["Monat"].Value)
                                             , int.Parse(tm.Groups["Tag"].Value)
                                             , int.Parse(tm.Groups["Stunde"].Value)
                                             , int.Parse(tm.Groups["Minute"].Value)
                                             , 0);
                        var anzTage = DateTime.Now.Subtract(dt).Days;

                        createNewLog = (limitDays < anzTage);
                    }
                }

                if (createNewLog)
                {
                    maxFileCount--;

                    var fn = String.Format("{0}_{1:yyyy-MM-dd_HHmm}.log", baseName, DateTime.Now);
                    _logFullName = Path.Combine(basePath, fn);
                }
                else _logFullName = chrono[0];

                for (int i = 0; i < chrono.Count; i++)
                {
                    if (maxFileCount < 1)
                        File.Delete(chrono[i]);
                    maxFileCount--;
                }
            }
        }
        public static Logger Log;

        static Umgebung()
        {
            try
            {
                var ue = new Umgebungseinstellungen();

                Web = new WebService(ue.BongUser, ue.BongPassword);
                Tab = new Database(ue.MySqlHost, ue.MySqlSchema, ue.MySqlUser, ue.MySqlPassword);
                Dir = new FileSystem(ue.RecordingLibraryRootPath);
                Log = new Logger(ue.LoggingPath, ue.LoggingBaseName, ue.LoggingLimitDays, ue.LoggingLimitBytes, ue.LoggingMaxFileCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Die Konfiguration konnte nicht gelesen werden oder enthielt ungültige Werte", ex);
            }
        }

        public static void Open()
        {
            Log.Info("Job gestartet am {0:dd.MM.yyyy HH:mm}", DateTime.Now);
            Log.Info("Versionsinformation: {0}", VersionInfo);
        }

        public static void Close()
        {
            Log.Info("Job beendet am {0:dd.MM.yyyy HH:mm}", DateTime.Now);
        }
    }
}
