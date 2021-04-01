using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBackup
{
    class Program
    {
        IDictionary<string, object> keyValuePairs = new Dictionary<string, object>();

        static void Main(string[] args)
        {
            string BackupFolderFullPath = Properties.AppSettings.Default.BackupFolderFullPath;
            string backupFolderFullPath =
              string.IsNullOrEmpty(BackupFolderFullPath)
                ? AppDomain.CurrentDomain.BaseDirectory
                : BackupFolderFullPath;

            string connectionString = "data source=.;initial catalog=IR-NE-Prod;integrated security=True;MultipleActiveResultSets=True;";

            BackupService backupService = new BackupService(connectionString, backupFolderFullPath);
            backupService.BackupAllUserDatabases();

        }
    }
}
