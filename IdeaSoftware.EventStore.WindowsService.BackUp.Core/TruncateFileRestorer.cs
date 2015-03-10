using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public class TruncateFileRestorer
    {
        public async Task<EventArgs> ExecuteAsync(DirectoryInfo source, DirectoryInfo destination)
        {
            File.Delete(new FileInfo(Path.Combine(destination.FullName, "chaser.chk")).FullName);
            var chaserInfo = source.GetFiles("chaser.chk").Single();
            var destFile = new FileInfo(Path.Combine(destination.FullName, "truncate.chk"));
            await FileCopier.CopyAsync(chaserInfo, destFile);

            return new ChaserRestoreCompleted();
        }
    }
}