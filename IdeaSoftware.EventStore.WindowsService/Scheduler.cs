using System;
using System.Threading;

namespace IdeaSoftware.EventStore.WindowsService
{
    /// <summary>
    /// Uses System.Threading.Timer specifically to make the host process fail when an exception occurs -
    /// A System.Timers.Timer will silently eat the exception and the host process would continue running
    /// </summary>
    public class Scheduler
    {

        private Timer _timer;




        public void DailyAt(int hour, int minute, Action action)
        {
            _timer = new Timer((s) => action());
            _timer.Change(GetNextHourInterval(hour, minute), (int)TimeSpan.FromDays(1).TotalMilliseconds);


        }

        public void EveryMinuteAt(int second, Action action)
        {
            _timer = new Timer((s) => action());
            _timer.Change(GetNextSecondInterval(second), (int)TimeSpan.FromMinutes(1).TotalMilliseconds);

        }


        private int GetNextSecondInterval(int runsAt)
        {
            var now = DateTime.Now;
            var nextDay = now.Second >= runsAt ? now.AddMinutes(1) : now;
            var nextRun = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, nextDay.Hour, nextDay.Minute, runsAt);
            return (int)(nextRun - now).TotalMilliseconds;
        }


        private int GetNextHourInterval(int hour, int minute)
        {
            var now = DateTime.Now;
            var nextDay = now;

            if (now.Hour > hour)
                nextDay = now.AddDays(1);
            if (now.Hour == hour && now.Minute >= minute )
                nextDay = now.AddDays(1);


            var nextRun = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, hour, minute, 0);
            return (int)(nextRun - now).TotalMilliseconds;
        }


        public void End()
        {
            _timer.Dispose();
        }




    }
}