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

namespace KerbalFoundries
{
	/// <summary>A utility class for handling log calls.</summary>
	public class KFLogUtil
	{		
		enum LogType
		{
			Log,
			Warning,
			Error
		}

		public static string strModName = "Kerbal Foundries";
		string strObjName = string.Empty;

		/// <summary>Creates an instance of KFLogUtil.</summary>
		public KFLogUtil()
		{
		}

		/// <summary>Creates an instance of KFLogUtil.</summary>
		/// <param name="obj">Name of calling class which will be included automatically in log messages.</param>
		public KFLogUtil(string obj)
		{
			strObjName = obj;
		}

		/// <summary>Creates an instance of KFLogUtil.</summary>
		/// <param name="obj">Calling class which type name will be included automatically in log messages.</param>
		public KFLogUtil(object obj)
			: this(nameof(obj))
		{
		}

		/// <summary>A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from. Does not show if not specified.</param>
		/// <remarks>Standard log entry.</remarks>
		public static void Log(string strText, string strClassName)
		{
			CreateLog(LogType.Log, strText, strClassName);
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
			CreateLog(LogType.Log, strText, strObjName);
		}

		/// <summary>A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Warning log entry.</remarks>
		public static void Warning(string strText, string strClassName)
		{
			CreateLog(LogType.Warning, strText, strClassName);
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
			CreateLog(LogType.Warning, strText, strObjName);
		}

		/// <summary>An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Error log entry.</remarks>
		public static void Error(string strText, string strClassName)
		{
			CreateLog(LogType.Error, strText, strClassName);
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
			CreateLog(LogType.Error, strText, strObjName);
		}

		/// <summary>Builds the log entry and passes it to the UnityEngine log utility.</summary>
		/// <param name="logType">what kind of log entry</param>
		/// <param name="strText">message</param>
		/// <param name="strObjName">name of calling class</param>
		static void CreateLog(LogType logType, string strText, string strObjName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!string.IsNullOrEmpty(strObjName))
				strOutput += string.Format(" - {0}", strObjName);
			strOutput += string.Format("]: {0}", strText);

			switch (logType)
			{
				case LogType.Error:
					UnityEngine.Debug.LogError(strOutput);
					break;
				case LogType.Warning:
					UnityEngine.Debug.LogWarning(strOutput);
					break;
				default:
					UnityEngine.Debug.Log(strOutput);
					break;
			}
		}

		/// <summary>Returns the type name of the specified object</summary>
		/// <param name="obj">Object you want the name of.</param>
		/// <returns>type name</returns>
		static string nameof(object obj)
		{
			return Equals(obj, null) ? string.Empty : obj.GetType().ToString();
		}
	}
}
