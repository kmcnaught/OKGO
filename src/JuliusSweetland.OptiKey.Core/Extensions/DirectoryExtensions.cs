using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JuliusSweetland.OptiKey.Extensions
{
    public static class DirectoryExtensions
    {
        public static void DeleteOnApplicationExit(this DirectoryInfo directory, ILog log)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Exit += (o, args) =>
                {
                    if (!directory.Exists) return;

                    try
                    {
                        directory.Delete(true);
                        
                        log.InfoFormat("{0} has been deleted.", directory.FullName);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("Error deleting {0} on OptiKey shutdown", directory.FullName), ex);
                    }
                };
            });
        }
    }
}
