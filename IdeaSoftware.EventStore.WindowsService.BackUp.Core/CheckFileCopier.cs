using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public class CheckFileCopier
    {
        public async Task<EventArgs> ExecuteAsync(DirectoryInfo source, DirectoryInfo destination)
        {
            var files = source.GetFiles("*.chk");

            for (int i = 0; i < files.Count(); i++)
            {
                var sourceFile = files.ElementAt(i);
                var destFile = new FileInfo(Path.Combine(destination.FullName, sourceFile.Name));
                await FileCopier.CopyAsync(sourceFile, destFile);
            }
            return new CheckPointBackupCompletedArgs();
        }
    }
}