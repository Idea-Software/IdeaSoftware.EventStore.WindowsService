using System;

namespace IdeaSoftware.EventStore.WindowsService.BackUp.Core
{
    public delegate void StageCompletedHandler(object sender, EventArgs args);
}