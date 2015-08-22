/*
 * Created by SharpDevelop.
 * User: AndrewRWoods (aka. Gaalidas)
 * Date Started: 5/9/2015
 * 
 * A set of utilities to make sending information to the log easier.
 * Options include prefixing the log entry with the mod name and/or
 * class (or more) that the entry comes from.
 */

using System;
using System.Linq;
using UnityEngine;
using KerbalFoundries.Log;

namespace KerbalFoundries
{
	/// <summary>A utility class for handling log calls.</summary>
	public class KFLogUtil
	{
		// disable MemberCanBeMadeStatic.Local
		
		enum LogType
		{
			Log,
			Warning,
			Error,
			DebugLog
		}

		public static string strModName = "Kerbal Foundries";
		// disable once RedundantDefaultFieldInitializer
		static KFLog KFLog = null; // In this case, the object complains about not being initialized, so I leave the redundant "null" intact.

		string strObjName = string.Empty;

		/// <summary>Creates an instance of KFLogUtil.</summary>
		public KFLogUtil()
		{
			// Empty, and proud of it!
		}

		/// <summary>Creates an instance of KFLogUtil.</summary>
		/// <param name="obj">Name of calling class which will be included automatically in log messages.</param>
		public KFLogUtil(string obj)
		{
			strObjName = obj;
		}

		/// <summary>Creates an instance of KFLogUtil.</summary>
		/// <param name="obj">Calling class which type name will be included automatically in log messages.</param>
		public KFLogUtil(object obj) : this(nameof(obj))
		{
			// Empty, and quite depressed to be.  "It's all my fault!"
		}

		/// <summary>A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from. Does not show if not specified.</param>
		/// <remarks>Standard log entry.</remarks>
		public static void Log(string strText, string strClassName)
		{
			CreateUnityEngineLog(LogType.Log, strText, strClassName);
		}

		/// <summary>A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="obj">(Optional) The specific class that is being logged from. Does not show if not specified.</param>
		/// <remarks>Standard log entry.</remarks>
		public static void Log(string strText, object obj)
		{
			Log(strText, nameof(obj));
		}

		/// <summary>A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <remarks>Standard log entry.</remarks>
		internal void Log(string strText)
		{
			CreateUnityEngineLog(LogType.Log, strText, strObjName);
		}

		/// <summary>A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Warning log entry.</remarks>
		public static void Warning(string strText, string strClassName)
		{
			CreateUnityEngineLog(LogType.Warning, strText, strClassName);
		}

		/// <summary>A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="obj">(Optional) The specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Warning log entry.</remarks>
		public static void Warning(string strText, object obj)
		{
			Warning(strText, nameof(obj));
		}

		/// <summary>A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <remarks>Warning log entry.</remarks>
		internal void Warning(string strText)
		{
			CreateUnityEngineLog(LogType.Warning, strText, strObjName);
		}

		/// <summary>An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Error log entry.</remarks>
		public static void Error(string strText, string strClassName)
		{
			CreateUnityEngineLog(LogType.Error, strText, strClassName);
		}

		/// <summary>An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="obj">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Error log entry.</remarks>
		public static void Error(string strText, object obj)
		{
			Error(strText, nameof(obj));
		}

		/// <summary>An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <remarks>Error log entry.</remarks>
		internal void Error(string strText)
		{
			CreateUnityEngineLog(LogType.Error, strText, strObjName);
		}

		/// <summary>A debug-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Debug log entry.</remarks>
		public static void DebugLog(string strText, string strClassName)
		{
			CreateUnityEngineLog(LogType.DebugLog, strText, strClassName);
		}

		/// <summary>A debug-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="obj">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Debug log entry.</remarks>
		public static void DebugLog(string strText, object obj)
		{
			DebugLog(strText, nameof(obj));
		}

		/// <summary>A debug-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <remarks>Debug log entry.</remarks>
		internal void DebugLog(string strText)
		{
			CreateUnityEngineLog(LogType.DebugLog, strText, strObjName);
		}

		/// <summary>Builds the log entry and passes it to the UnityEngine log utility.</summary>
		/// <param name="logType">what kind of log entry</param>
		/// <param name="strText">message</param>
		/// <param name="strObjName">name of calling class</param>
		static void CreateUnityEngineLog(LogType logType, string strText, string strObjName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!string.IsNullOrEmpty(strObjName))
				strOutput += string.Format(" - {0}", strObjName);
			if (Equals(logType, LogType.DebugLog))
				strOutput += " - DEBUG";
			strOutput += string.Format("]: {0}", strText);

			switch (logType)
			{
				case LogType.Error:
					Debug.LogError(strOutput);
					break;
				case LogType.Warning:
					Debug.LogWarning(strOutput);
					break;
				case LogType.DebugLog:
					PrintDebugLog(strOutput);
					break;
				// disable once RedundantCaseLabel
				case LogType.Log:
				default:
					Debug.Log(strOutput);
					break;
			}

			if (KFPersistenceManager.writeToLogFile)
				CreateKFLog(logType, strText, strObjName);
		}

		/// <summary>Formats the message and sends it to the log writer thread.</summary>
		/// <param name="logType">Log, Warning, Error</param>
		/// <param name="strText">message</param>
		/// <param name="strObjName">calling object</param>
		static void CreateKFLog(LogType logType, string strText, string strObjName)
		{
			if (KFLog.Ready)
			{
				string strOutput = string.Format("{0} {1,-7} {2}: {3}", DateTime.Now.ToString("hh:mm:ss.fff"), logType.ToString().ToUpper(), strObjName, strText);
				KFLog.WriteToFile(strOutput);
			}
		}

		/// <summary>Returns the type name of the specified object</summary>
		/// <param name="obj">Object you want the name of.</param>
		/// <returns>type name</returns>
		static string nameof(object obj)
		{
			string strOutput;
			if (Equals(obj, null))
			{
				strOutput = string.Empty;
				return strOutput;
			}
			return obj.GetType().ToString();
		}
		
		static void PrintDebugLog(string input)
		{
			#if DEBUG
			Debug.Log(input);
			#endif
		}
	}
}