using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using MajimoBase;

namespace MajimoBong
{
    public class RecordingService
    {
        private const String UrlRecordings = "http://www.bong.tv/api/recordings.xml?username={0}&password={1}";
        private const String UrlDeleteRecording = "http://www.bong.tv/api/recordings/{0}/delete.xml?username={1}&password={2}";

        public bool CheckWebServiceAccess()
        {
            return true;
        }

        public List<Recording> GetDownloadableRecordings()
        {
            var retval = new List<Recording>();
            var urlRecordings = String.Format(UrlRecordings, Umgebung.Web.User, Umgebung.Web.Password);

            var docRecordings = new XmlDocument();
            docRecordings.Load(urlRecordings);

            Umgebung.Log.Dump("Bong recordings.xml Ergebnisdokument:\n{0}", docRecordings.ToString());

            var root = docRecordings.DocumentElement;
            if (root != null)
            {
                var nodes = root.SelectNodes("/recordings/recording");

                if (nodes != null)
                    foreach (XmlNode node in nodes)
                    {
                        var docRecording = new XmlDocument();

                        try
                        {
                            docRecording.InnerXml = node.OuterXml;
                            var xmlDecl = docRecording.CreateXmlDeclaration("1.0", "UTF-8", null);
                            var rootRecording = docRecording.DocumentElement;
                            docRecording.InsertBefore(xmlDecl, rootRecording);

                            Umgebung.Log.Dump("Neues XML-Dokument erstellt:\n{0}", docRecording.ToString());

                            var rec = new Recording()
                                          {
                                              // Id wird erst später nachgetragen
                                              BongId = GetNodeText(docRecording, "/recording/id"),
                                              Title = GetNodeText(docRecording, "/recording/title"),
                                              Subtitle = GetNodeText(docRecording, "/recording/subtitle"),
                                              Description = GetNodeText(docRecording, "/recording/description"),
                                              Channel = GetNodeText(docRecording, "/recording/channel"),
                                              Start = DateTime.Parse(GetNodeText(docRecording, "/recording/start"), new CultureInfo("de-DE"), DateTimeStyles.None),
                                              Duration = TimeSpan.Parse(GetNodeText(docRecording, "/recording/duration"), new CultureInfo("de-DE")), 
                                              Genre = GetNodeText(docRecording, "/recording/genre"),
                                              SeriesSeason = GetNodeText(docRecording, "/recording/series_season"),
                                              SeriesNumber = GetNodeText(docRecording, "/recording/series_number"),
                                              SeriesCount = GetNodeText(docRecording, "/recording/series_count"),
                                              ImageUrl = GetNodeText(docRecording, "/recording/image"),
                                              DownloadUrlHD = GetNodeText(docRecording, "/recording/files/file[type='download' and quality='HD']/url"),
                                              DownloadUrlHQ = GetNodeText(docRecording, "/recording/files/file[type='download' and quality='HQ']/url"),
                                              DownloadUrlNQ = GetNodeText(docRecording, "/recording/files/file[type='download' and quality='NQ']/url")
                                          };

                            rec.Dump("Recording-Instanz aus Bong-XML erstellt");

                            retval.Add(rec);
                        }
                        catch (Exception ex)
                        {
                            Umgebung.Log.Error("Initialisieren von Recording aus der XML-Struktur schlug fehl: {0}\n{1}", ex.Message, docRecording.ToString());
                        }
                    }
            }

            return retval;
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
