using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public static class FileCopier
    {
        private const int MaxRetry = 3;

        public static async Task CopyAsync(FileInfo source, FileInfo destination, int retryCount = 0)
        {
            //var copySuccessful = false;
            var filedLocked = false;
            try
            {
                using (var destStream = destination.Open(FileMode.OpenOrCreate))
                using (var sourceStream = source.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

            }
            catch (IOException exception)
            {

                filedLocked = true;
            }

            //hopefully file in use exception, recursivly retry 
            if (filedLocked)
            {
                if (retryCount < MaxRetry) 
                {
                    Thread.Sleep(1000);
                    await CopyAsync(source, destination, retryCount + 1);        
                }
                else
                {
                    throw new IOException("Could not perform copy operation");
                }
            }
                   
            
        }
    }
}