using System;
using System.Text;

namespace MajimoBase
{
    public class Recording
    {
        public int Id { get; set; }
        public string BongId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string Channel { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public string Genre { get; set; }
        public string SeriesSeason { get; set; }
        public string SeriesNumber { get; set; }
        public string SeriesCount { get; set; }
        public string ImageUrl { get; set; }
        // ReSharper disable InconsistentNaming
        public string DownloadUrlHD { get; set; }
        public string DownloadUrlHQ { get; set; }
        public string DownloadUrlNQ { get; set; }
        // ReSharper restore InconsistentNaming

        public void Dump(string caption)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0}\n", caption ?? "--------------------");
            sb.AppendFormat("    Id             = {0}\n", Id);
            sb.AppendFormat("    BongId         = {0}\n", BongId);
            sb.AppendFormat("    Title          = {0}\n", Title);
            sb.AppendFormat("    Subtitle       = {0}\n", Subtitle);
            sb.AppendFormat("    Description    = {0}\n", Description);
            sb.AppendFormat("    Channel        = {0}\n", Channel);
            sb.AppendFormat("    Start          = {0}\n", Start);
            sb.AppendFormat("    Duration       = {0}\n", Duration);
            sb.AppendFormat("    Genre          = {0}\n", Genre);
            sb.AppendFormat("    SeriesSeason   = {0}\n", SeriesSeason);
            sb.AppendFormat("    SeriesNumber   = {0}\n", SeriesNumber);
            sb.AppendFormat("    SeriesCount    = {0}\n", SeriesCount);
            sb.AppendFormat("    ImageUrl       = {0}\n", ImageUrl);
            sb.AppendFormat("    DownloadUrlHD  = {0}\n", DownloadUrlHD);
            sb.AppendFormat("    DownloadUrlHQ  = {0}\n", DownloadUrlHQ);
            sb.AppendFormat("    DownloadUrlNQ  = {0}\n", DownloadUrlNQ);

            Umgebung.Log.Dump(sb.ToString());
        }
    }
}
