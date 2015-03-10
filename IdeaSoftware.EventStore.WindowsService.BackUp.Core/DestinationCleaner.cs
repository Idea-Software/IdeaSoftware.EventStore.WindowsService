using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public class DestinationCleaner
    {
        public async Task<EventArgs> ExecuteAsync(DirectoryInfo source, DirectoryInfo destination)
        {

            var files = destination.GetFiles(@"chunk-*.*").ToList();
            files.Add(destination.GetFiles("chaser.chk").Single());
            files.Add(destination.GetFiles("epoch.chk").Single());
            files.Add(destination.GetFiles("writer.chk").Single());
            for (int i = 0; i < files.Count(); i++)
            {
                var sourceFile = files.ElementAt(i);
                var destFile = new FileInfo(Path.Combine(destination.FullName, sourceFile.Name));
                destFile.Delete();
            }
            Directory.Delete(destination.FullName + "/index");

            return new DestinationCleaningCompleted();
        }
    }
}