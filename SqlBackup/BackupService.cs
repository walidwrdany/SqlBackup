using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SqlBackup
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolderFullPath;
        private readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };

        public BackupService(string connectionString, string backupFolderFullPath)
        {
            _connectionString = connectionString;
            _backupFolderFullPath = backupFolderFullPath;
        }

        public void BackupAllUserDatabases()
        {
            Parallel.ForEach(GetAllUserDatabases(), databaseName =>
            {
                BackupDatabase(databaseName, Properties.AppSettings.Default.CompressBackup);
            });
        }

        public void BackupDatabase(string databaseName, bool compress = false)
        {
            string filePath = BuildBackupPathWithFilename(databaseName);

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = string.Format(Properties.AppSettings.Default.SQLBackupCommand, databaseName, filePath);

                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            if (compress)
            {
                CompressBackup(filePath);
            }
        }

        private void CompressBackup(string filePath)
        {
            Console.WriteLine(filePath);

            FileInfo fileInfo = new FileInfo(filePath);

            try
            {
                string sourceName = fileInfo.FullName;
                string targetName = Path.Combine(fileInfo.DirectoryName, fileInfo.Name.Replace(".bak", ".7z"));

                Process process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = @"C:\Program Files\7-Zip\7zG.exe",
                        Arguments = "a -t7z -mx9 -aoa \"" + targetName + "\" \"" + sourceName + "\"",
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                );
                process.WaitForExit();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                fileInfo.Delete();
            }
        }

        private IEnumerable<string> GetAllUserDatabases()
        {
            var databases = new List<string>();

            DataTable databasesTable;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                databasesTable = connection.GetSchema("Databases");
                connection.Close();
            }

            foreach (DataRow row in databasesTable.Rows)
            {
                string databaseName = row["database_name"].ToString();

                if (_systemDatabaseNames.Contains(databaseName))
                    continue;

                databases.Add(databaseName);
            }

            return databases;
        }

        private string BuildBackupPathWithFilename(string databaseName)
        {
            string filename = string.Format(Properties.AppSettings.Default.BackupFileFormmat + ".bak", databaseName, DateTime.Now.ToString(Properties.AppSettings.Default.BackupFileDateFormmat));

            string fullPath = Path.Combine(_backupFolderFullPath, databaseName);

            DirectoryInfo directoryInfo = new DirectoryInfo(fullPath);

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            else
            {
                // delete older backups for saving disk space
                if (Properties.AppSettings.Default.DeleteOlderBackups)
                    Parallel.ForEach(directoryInfo.GetFiles()
                                    .Where(fileInfo => DateTime.Now > fileInfo.CreationTime.AddDays(7)),
                                    fileInfo =>
                                    {
                                        fileInfo.Delete();
                                    });
            }

            return Path.Combine(fullPath, filename);
        }


    }
}
