using PowerArgs;

namespace IdeaSoftware.EventStore.WindowsService.BackUp
{
    [TabCompletion]
    public class BackupArgs
    {

        [ArgShortcut("s")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("es data directory")]
        [ArgExistingDirectory]
        public string Source { get; set; }



        [ArgShortcut("d")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("backup directory")]
        [ArgExistingDirectory]
        public string Destination { get; set; }


        [ArgShortcut("sn")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("eventstore service name")]
        [ArgExistingService]
        public string ServiceName { get; set; }

    }
}