using System;
using System.Threading;

namespace GeoTourney
{
    public class ReadConsole
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string? input;

        static ReadConsole()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            inputThread = new Thread(reader) {IsBackground = true};
            inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        public static string? ReadLine(TimeSpan timeout)
        {
            getInput.Set();
            var success = gotInput.WaitOne((int)timeout.TotalMilliseconds);
            return success ? input : null;
        }

        public static void QueueCommand(string command)
        {
            input = command;
            gotInput.Set();
        }
    }
}