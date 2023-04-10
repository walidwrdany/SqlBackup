using SqlBackup.Properties;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace SqlBackup
{
    internal class Program
    {
        private static readonly DateTime Time = DateTime.Now;

        private static readonly string FolderPath = Path.Combine(AppSettings.Default.BackupFolder, $"SQLBackup{Time:yyyyMMddHHmmss}");

        private static readonly string ConnectionString = AppSettings.Default.ConnectionString;


        private static void Main(string[] args)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Get the list of databases on the server
                    var query = @"
                            SELECT db.name
                            FROM sys.databases AS db
                            WHERE 1 = 1
                                  AND db.name NOT IN ( 'master', 'model', 'msdb', 'tempdb' )
                                  AND db.state = 0
                                  AND db.is_in_standby = 0";

                    if (!string.IsNullOrEmpty(AppSettings.Default.SqlCommand))
                        query += $" AND db.{AppSettings.Default.SqlCommand};";
                    else
                        query += ";";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var databaseName = reader.GetString(0);

                                // Backup the database
                                BackupDatabase(databaseName, FolderPath);
                            }
                        }
                    }
                }

                if (Convert.ToBoolean(AppSettings.Default.CompressBackup)) Archive();

            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred while backing up databases: " + ex.Message);
            }

            Logger.LogInformation("Press any key to exit...");
        }

        private static void Archive()
        {
            foreach (var file in Directory.GetFiles(FolderPath, "*.bak", SearchOption.TopDirectoryOnly))
            {
                var targetName = $"{Path.GetDirectoryName(file)}\\{Path.GetFileNameWithoutExtension(file)}.7z";
                using (var process = new Process())
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\7z.exe",

                        /*
                         Here is the breakdown of each option used:
                            a: Short for "add", this option specifies that we want to create a new archive.
                            -t7z: Specifies the type of archive we want to create, in this case a 7z archive.
                            -mx9: Specifies the maximum compression level, which in this case is 9 (ultra).
                            -aoa: Specifies that we want to overwrite any existing archive without prompting.
                        */
                        Arguments = $"a -t7z -mx9 -aoa \"{targetName}\" \"{file}\"",
                    };

                    process.StartInfo = info;
                    process.Start();
                    process.WaitForExit();
                }

                File.Delete(file);
            }
        }


        private static void BackupDatabase(string databaseName, string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var backupFileName = $"{folderPath}\\{databaseName}_{Time:yyyyMMddHHmmss}.bak";

            // Perform the backup
            var query = $"BACKUP DATABASE [{databaseName}] TO DISK='{backupFileName}' WITH INIT";
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            Logger.LogInformation($"Backup of database '{databaseName}' completed successfully.");
        }
    }
}