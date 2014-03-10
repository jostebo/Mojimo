using System;
using MajimoBase;
using MajimoBong; 
using MajimoDatabase;


namespace Majimo
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Open())
            {
                foreach (var rec in Bong.GetDownloadableRecordings())
                {
                    Daten.AddRecording(rec);
                }

                foreach (var rec in Daten.GetRecordings(Datenbank.GetRecordingsFilter.AttendingDownload))
                {
                    Daten.MarkRecording(rec, Datenbank.MarkRecordingState.DownloadCompleted);
                }

                foreach (var rec in Daten.GetRecordings(Datenbank.GetRecordingsFilter.DownloadCompleted))
                {
                    Daten.MarkRecording(rec, Datenbank.MarkRecordingState.WebRecordingDeleted);
                }
                
                Close();
            }

            Console.WriteLine("Taste drücken zum Beenden");
            Console.ReadKey();
        }

        public static Datenbank Daten { get; private set; }
        public static RecordingService Bong { get; private set; }

        private static bool Open()
        {
            var retval = true;

            try
            {
                Umgebung.Open();
                Daten = new Datenbank();
                if (Daten.Open())
                {
                    Bong = new RecordingService();
                    if (!Bong.CheckWebServiceAccess())
                        throw new Exception("Der Bong-WebService ist nicht erreichbar");
                }
                else throw new Exception("Verbindung zur Datenbank konnte nicht geöffnet werden.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Umgebung konnte nicht initalisiert werden\n{0}", ex.ToString());
                retval = false;    
            }

            return retval;
        }

        private static void Close()
        {
            Bong = null;
            Daten.Close();
            Daten = null;
            Umgebung.Close();
        }
    }
}
