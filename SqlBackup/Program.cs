using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqlBackup
{
    class Program
    {

        static void Main(string[] args)
        {
            string BackupFolderFullPath = Properties.AppSettings.Default.BackupFolderFullPath;
            string backupFolderFullPath =
              string.IsNullOrEmpty(BackupFolderFullPath)
                ? AppDomain.CurrentDomain.BaseDirectory
                : BackupFolderFullPath;

            string connectionString = Properties.AppSettings.Default.ConnectionString;

            ParallelOptions options = new ParallelOptions()
            {
                TaskScheduler = TaskScheduler.Default
            };

            Action[] actions = new Action[] {
                delegate()
                {
                    Console.WriteLine("Backup All User Databases Started...");

                    BackupService backupService = new BackupService(connectionString, backupFolderFullPath);
                    backupService.BackupAllUserDatabases();

                    Console.WriteLine("Backup All User Databases Finished...");
                },
            };


            Parallel.Invoke(options, actions);

        }

    }
}
