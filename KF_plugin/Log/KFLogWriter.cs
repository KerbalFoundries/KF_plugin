using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace KerbalFoundries.Log
{
	public class KFLogWriter
	{
		/// <summary>Helper object to synchronize queue access.</summary>
		object sync;
		/// <summary>Stream which writes into a file.</summary>
		FileStream outStream;
		/// <summary>Queue which holds log entries to be written to file.</summary>
		Queue<string> queue;
		/// <summary>Flag for stopping a running thread.</summary>
		volatile bool dontExit;

		/// <summary>Creates an instance of the log write thread, but does not start it yet.</summary>
		/// <param name="logFile">Where to write to.</param>
		/// <param name="queue">"log entries" queue.</param>
		/// <param name="sync">Helper object for synchronizing queue access.</param>
		public KFLogWriter(string logFile, Queue<string> queue, object sync)
		{
			outStream = CreateLogFile(logFile);
			this.queue = queue;
			this.sync = sync;
		}

		/// <summary>Main loop of the thread. Processes queue items until a stop flag is set.</summary>
		/// <remarks>Use RequestStop() to end the thread.</remarks>
		public void Loop()
		{
            System.AppDomain.CurrentDomain.DomainUnload += delegate { CloseFile(); }; // close log file when KSP shuts down            

			if (!outStream.CanWrite)
				return;

			string startMessage = string.Format("Logging started at {0:hh:mm:ss.fff}\n\nLoaded assemblies:\n", System.DateTime.Now);
			foreach (Assembly assembly in Thread.GetDomain().GetAssemblies())
				startMessage += string.Format("{0,-15} ({1}) ({2})", assembly.GetName().Name, assembly.GetName().Version, assembly.Location);
			startMessage += "\n\n------------\n\n";

			var encoder = new UTF8Encoding(true);
			byte[] buffer = encoder.GetBytes(startMessage);
			outStream.Write(buffer, 0, buffer.Length);
			outStream.Flush();

			Monitor.Enter(sync); // block other thread(s) until lock is aquired

			dontExit = true;
			while (dontExit) // thread main loop
			{
				while (queue.Count < 1 || !dontExit) 	// check if there's something to do
                    Monitor.Wait(sync); 				// release the lock, block this thread and
														// wait until the lock is aquired again
														// then the while-loop continues to work

				if (queue.Count > 0)
				{
					string queueItem = string.Format("{0}\n", queue.Dequeue()); // something is in the queue, quickly grab it!
					buffer = encoder.GetBytes(queueItem); // convert string into UTF8 and returns the result as a byte array
					outStream.Write(buffer, 0, buffer.Length); // write to file
					outStream.Flush();

					Monitor.Pulse(sync); // wake up the other thread (during the next Monitor.Wait)
				}
			}
			Monitor.Exit(sync); // release lock
            CloseFile();
		}

		/// <summary>Creates a stream which can write into the specified file.</summary>
		/// <param name="logFile">Where to write to.</param>
		/// <returns>Stream for writing.</returns>
		/// <remarks>If the file already exists, it will be deleted and created again.</remarks>
		FileStream CreateLogFile(string logFile)
		{
			if (File.Exists(logFile))
				File.Delete(logFile);

			return File.Create(logFile);
		}

		/// <summary>End writing to file and release all resources.</summary>
		void CloseFile()
		{
			outStream.Flush(); // write everything still in buffer to disk
			outStream.Close(); // close the file access and release all resources
		}

		/// <summary>Requests the thread to stop.</summary>
		public void RequestStop()
		{
			dontExit = false;
		}
	}
}
