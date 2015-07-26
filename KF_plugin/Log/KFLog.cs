using System.Collections.Generic;
using System.Threading;

namespace KerbalFoundries.Log
{
    public class KFLog
    {
        /// <summary>helper object to synchronize queue access</summary>
        static object sync = null;
        /// <summary>holds all log messages</summary>
        static Queue<string> queue = null;
        /// <summary>instance of the log writer</summary>
        static KFLogWriter writer = null;
        /// <summary>the thread the log writer runs in</summary>
        static Thread thread = null;
        /// <summary>Check this to see if logging thread is ready for work.</summary>
        public static bool Ready = false;

        /// <summary>Prepares and creates the log writer thread.</summary>
        public static void StartWriter()
        {
            sync = new object();
            queue = new Queue<string>();
            writer = new KFLogWriter(KFPersistenceManager.logFile, queue, sync);
            thread = new Thread(writer.Loop);
            thread.Start();
            Ready = true;
        }

        /// <summary>Stops the thread.</summary>
        public static void StopWriter()
        {
            if (Ready && !Equals(thread, null))
            {
                if (thread.IsAlive)
                    writer.RequestStop(); // tell it to stop

                thread.Join(); // wait until thread terminates
                Ready = false;
            }            
        }

        /// <summary>Writes a message to the log.</summary>
        /// <param name="message">message to write</param>
        public static void WriteToFile(string message)
        {
            if (Ready)
            {
                Monitor.Enter(sync); // wait for lock to safely access the queue
                queue.Enqueue(message); // put message into the queue
                Monitor.Pulse(sync); // notify writer thread
                Monitor.Exit(sync); // release lock
            }
        }
    }
}
