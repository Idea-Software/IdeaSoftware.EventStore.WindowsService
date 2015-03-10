using System;
using System.Linq;
using System.ServiceProcess;
using PowerArgs;

namespace IdeaSoftware.EventStore.WindowsService.Restore
{
    public class ArgExistingService : ArgValidator
    {
        public override void Validate(string name, ref string arg)
        {
            var serviceName = arg;
            
            if(!ServiceController.GetServices().Any(controller => controller.ServiceName.Equals(serviceName)))
            {
                throw new ArgumentException(string.Format("service doest not exist {0}", serviceName));
            }
            base.Validate(name, ref arg);
        }
    }
}