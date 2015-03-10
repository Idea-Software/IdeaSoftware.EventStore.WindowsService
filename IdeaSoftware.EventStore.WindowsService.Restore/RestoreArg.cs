using PowerArgs;

namespace IdeaSoftware.EventStore.WindowsService.Restore
{
    [TabCompletion]    
    public class RestoreArg
    {

        [ArgShortcut("s")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("directory where backup was saved to")]
        [ArgExistingDirectory]
        public string Source { get; set; }



        [ArgShortcut("d")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("eventstore data directory to restore to")]
        [ArgExistingDirectory]
        public string Destination { get; set; }

        
        [ArgShortcut("sn")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("eventstore service name")]
        [ArgExistingService]
        public string ServiceName { get; set; }

    }
}