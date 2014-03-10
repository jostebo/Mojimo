using System;
using System.Collections.Generic;
using MajimoBase;
using MySql.Data.MySqlClient;


namespace MajimoDatabase
{
    public class Datenbank
    {
        public enum GetRecordingsFilter { AttendingDownload, DownloadCompleted }
        public enum MarkRecordingState { DownloadCompleted, WebRecordingDeleted }

        private readonly string _connectionString = CreateConnStr(Umgebung.Tab.Host
                                                                , Umgebung.Tab.Schema
                                                                , Umgebung.Tab.User
                                                                , Umgebung.Tab.Password
                                                                );
        private MySqlConnection _connection = null;
        private int _nextId = 3000;

        private void GetNextId()
        {
            if (_connection == null) return;

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT MAX(id) AS max_id FROM Recordings";

            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    int i = reader.GetInt32(0);
                    if (2000 < i && i < 1000000000)
                    {
                        _nextId = i + 1;
                    }
                }
            }
            reader.Close();
        }

        public bool Open()
        {
            bool retval = true;

            if (_connection == null)
            {
                try
                {
                    _connection = new MySqlConnection(_connectionString);
                    _connection.Open();
                }
                catch (Exception ex)
                {
                    Umgebung.Log.Error("Das Öffnen der Datenbank schlug fehl: {0}", ex.Message);
                    _connection = null;
                    retval = false;
                }

                GetNextId();
            }

            return retval;
        }

        public void Close()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        public void AddRecording(Recording rec)
        {
            var recordingExists = false;

            if (_connection == null) return;

            rec.Id = _nextId;

            try
            {
                var sqlins = Hilfsfunktionen.GetTextRessource("SQL.InsertRecording.txt");
                var command = new MySqlCommand(sqlins, _connection);
                command.Parameters.AddWithValue("@id", rec.Id);
                command.Parameters.AddWithValue("@bong_id", rec.BongId);
                command.Parameters.AddWithValue("@title", rec.Title);
                command.Parameters.AddWithValue("@subtitle", rec.Subtitle);
                command.Parameters.AddWithValue("@description", rec.Description);
                command.Parameters.AddWithValue("@channel", rec.Channel);
                command.Parameters.AddWithValue("@start", rec.Start);
                command.Parameters.AddWithValue("@duration", rec.Duration);
                command.Parameters.AddWithValue("@genre", rec.Genre);
                command.Parameters.AddWithValue("@series_season", rec.SeriesSeason);
                command.Parameters.AddWithValue("@series_number", rec.SeriesNumber);
                command.Parameters.AddWithValue("@series_count", rec.SeriesCount);
                command.Parameters.AddWithValue("@image_url", rec.ImageUrl);
                command.Parameters.AddWithValue("@download_hd_url", rec.DownloadUrlHD);
                command.Parameters.AddWithValue("@download_hq_url", rec.DownloadUrlHQ);
                command.Parameters.AddWithValue("@download_nq_url", rec.DownloadUrlNQ);
                command.ExecuteNonQuery();
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                if (ex.Number != 1062) // alles außer: doppelter Schlüssel (Sender, Startzeitpunkt)
                {
                    Umgebung.Log.Error("MySql-Error {0}: BongRecording konnte nicht eingefügt werden:\n{1}\n{2}", ex.Number, ex.Message, ex.ToString());
                    throw;
                }
                else recordingExists = true;
            }
            catch (Exception ex)
            {
                Umgebung.Log.Error("BongRecording konnte nicht eingefügt werden: {0}", ex.Message);
                throw;
            }

            if (!recordingExists)
            {
                Umgebung.Log.Info("Neue Aufnahme {0}: {1}", rec.BongId, rec.Title);
                _nextId++;
            }
        }

        public List<Recording> GetRecordings(GetRecordingsFilter filter)
        {
            string sql = "";
            var retval = new List<Recording>();

            if (_connection == null) return retval;

            switch (filter)
            {
                case GetRecordingsFilter.AttendingDownload:
                    sql = Hilfsfunktionen.GetTextRessource("SQL.SelectRecordingsForDownload.txt");
                    break;

                case GetRecordingsFilter.DownloadCompleted:
                    sql = Hilfsfunktionen.GetTextRessource("SQL.SelectRecordingsForDeletion.txt");
                    break;
            }

            var command = _connection.CreateCommand();
            command.CommandText = sql;
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var rec = new Recording();

                rec.Id = reader.GetInt32(0);
                rec.BongId = reader.GetString(1);
                rec.Title = reader.GetString(2);
                rec.Subtitle = reader.IsDBNull(3) ? "" : reader.GetString(3); ;
                rec.Description = reader.IsDBNull(4) ? "" : reader.GetString(4); ;
                rec.Channel = reader.GetString(5);
                rec.Start = reader.GetDateTime(6);
                rec.Duration = reader.GetTimeSpan(7);
                rec.Genre = reader.IsDBNull(8) ? "" : reader.GetString(8); ;
                rec.SeriesSeason = reader.IsDBNull(9) ? "" : reader.GetString(9); ;
                rec.SeriesNumber = reader.IsDBNull(10) ? "" : reader.GetString(10); ;
                rec.SeriesCount = reader.IsDBNull(11) ? "" : reader.GetString(11); ;
                rec.ImageUrl = reader.IsDBNull(12) ? "" : reader.GetString(12); ;
                rec.DownloadUrlHD = reader.IsDBNull(13) ? "" : reader.GetString(13); ;
                rec.DownloadUrlHQ = reader.IsDBNull(14) ? "" : reader.GetString(14); ;
                rec.DownloadUrlNQ = reader.IsDBNull(15) ? "" : reader.GetString(15); ;

                retval.Add(rec);
            }
            reader.Close();

            return retval;
        }

        public void MarkRecording(Recording rec, MarkRecordingState state)
        {
            if (_connection == null) return;

            var sql = "";

            switch (state)
            {
                case MarkRecordingState.DownloadCompleted:
                    sql = "UPDATE Recordings SET download_date = SYSDATE() WHERE id = @id AND download_date IS NULL";
                    Umgebung.Log.Info("Setze DownloadCompleted für {0}: {1}", rec.BongId, rec.Title);
                    break;

                case MarkRecordingState.WebRecordingDeleted:
                    sql = "UPDATE Recordings SET delete_date = SYSDATE() WHERE id = @id AND download_date IS NOT NULL AND delete_date IS NULL";
                    Umgebung.Log.Info("Setze WebRecordingDeleted für {0}: {1}", rec.BongId, rec.Title);
                    break;
            }

            var command = new MySqlCommand(sql, _connection);
            command.Parameters.AddWithValue("@id", rec.Id);
            command.ExecuteNonQuery();
        }

        private void Test()
        {
            //create a MySQL connection with a query string
            var connection = new MySqlConnection(_connectionString);

            //open the connection
            connection.Open();

            //close the connection
            connection.Close();
        }

        private static string CreateConnStr(string server, string databaseName, string user, string pass)
        {
            return "server=" + server + ";database=" + databaseName + ";uid=" + user + ";password=" + pass + ";";
        }
    }
}
