/*
 * Created by SharpDevelop.
 * User: AndrewRWoods
 * Date: 5/9/2015
 * 
 * A set of utilities to make sending information to the log easier.
 * Options include prefixing the log entry with the mod name and/or class (or more) that the entry comes from.
 * Will eventually include the ability to define a separate log file to be created.
 */

using System;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFLogUtil
	{
		// disable MemberCanBeMadeStatic.Local
		
		public void testlog()
		{
			var frame = new System.Diagnostics.StackFrame(1);
			var method = frame.GetMethod();
        	var classname = method.DeclaringType;
        	var methodname = method.Name;
        	string logprefix = string.Format("[{0} - {1}]: ", classname, methodname);
		}
		
		/// <summary>
		/// A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.
		/// </summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strModName">(Optional) Name of the mod being logged for.  Defaults to "Kerbal Foundries" if not specified.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <param name="strMethodName">(Optional) "classname" parameter must be specified!  Name of a specific method being logged from.  Does not show if not specified.</param>
		/// <remarks>Standard log entry.</remarks>
		public void KFLog(string strText, string strModName = "Kerbal Foundries", string strClassName = "", string strMethodName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
			{
				strOutput += string.Format(" - {0}", strClassName);
				if (!Equals(strMethodName, ""))
					strOutput += string.Format("-{0}", strMethodName);
			}
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput, 0, true);
		}
		
		/// <summary>
		/// A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.
		/// </summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strModName">(Optional) Name of the mod being logged for.  Defaults to "Kerbal Foundries" if not specified.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <param name="strMethodName">(Optional) "classname" parameter must be specified!  Name of a specific method being logged from.  Does not show if not specified.</param>
		/// <remarks>Warning log entry.</remarks>
		public void KFLogWarning(string strText, string strModName = "Kerbal Foundries", string strClassName = "", string strMethodName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
			{
				strOutput += string.Format(" - {0}", strClassName);
				if (!Equals(strMethodName, ""))
					strOutput += string.Format("-{0}", strMethodName);
			}
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput, 1, true);
		}
		
		/// <summary>
		/// An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.
		/// </summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strModName">(Optional) Name of the mod being logged for.  Defaults to "Kerbal Foundries" if not specified.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <param name="strMethodName">(Optional) "classname" parameter must be specified!  Name of a specific method being logged from.  Does not show if not specified.</param>
		/// <remarks>Error log entry.</remarks>
		public void KFLogError(string strText, string strModName = "Kerbal Foundries", string strClassName = "", string strMethodName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
			{
				strOutput += string.Format(" - {0}", strClassName);
				if (!Equals(strMethodName, ""))
					strOutput += string.Format("-{0}", strMethodName);
			}
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput, 2, true);
		}
		
		/// <summary>
		/// This is the method that actually posts the logs.
		/// </summary>
		/// <param name="strText">The output from the formatting method, or a simple line of text with no extra formatting which will be formatted in a default manner..</param>
		/// <param name="iLogtype">(Optional) The type of log being posted.  Defaults to 0 (standard-level log entry).</param>
		/// <param name="bFormatted">(Optional) Boolean that tells this method if the text has already been formatted.  Leave empty if you don't know what to do with it.</param>
		/// <remarks>"logtype" can be an integer of 0, 1, or 2 which corresponds with standard, warning, or error logs respectively.</remarks>
		public void KFLogIt(string strText, int iLogtype = 0, bool bFormatted = false)
		{
			string strOutput;
			strOutput = !bFormatted ? string.Format("[Kerbal Foundries]: {0}", strText) : strText;
			switch (iLogtype)
			{
				case 0:
					Debug.Log(strOutput);
					break;
				case 1:
					Debug.LogWarning(strOutput);
					break;
				case 2:
					Debug.LogError(strOutput);
					break;
				default:
					Debug.Log(strOutput);
					break;
			}
		}
		
		/// <summary>
		/// This method posts the specified line of text to a separate file and has a lot of options to work with in relation to formatting.
		/// </summary>
		/// <param name="strText">Text to make the log entry for.</param>
		/// <param name="strFileName">File name to send this entry to.</param>
		/// <param name="strModName">Name of the mod to tag this entry for.</param>
		/// <param name="strClassName">Name of the class to tag this entry for.</param>
		/// <param name="strMethodName">Name of the method to tag this entry for.</param>
		/// <param name="bTimeStamp">Do we want a time stamp on this line?</param>
		/// <param name="bOverwriteOldLog">Are we wanting to overwrite any old logs that may be present?</param>
		public void KFLogToFile(string strText, string strFileName, string strModName = "Kerbal Foundries", string strClassName = "", string strMethodName = "", bool bTimeStamp = false, bool bOverwriteOldLog = false)
		{
			if (Equals(strFileName, null))
				strFileName = string.Format("Log-{0}", strModName);
			// To-Do: Add the functionality for logging to a specific file.
			// Placeholder variables to keep "unused parameter" errors from cropping up.
			string text = strText;
			string filename = strFileName;
			string modname = strModName;
			string classname = strClassName;
			string methodname = strMethodName;
			bool timestamp = bTimeStamp;
			bool overwrite = bOverwriteOldLog;
			
			strFileName += ".log";
			
        	// using (var file = new System.IO.StreamWriter(string.Format("{0}\\GameData\\{1}", KSPUtil.ApplicationRootPath, strFileName)))
            //	foreach (var sh in sorted)
        	//		file.WriteLine(string.Format("{0}", strText));
		}
	}
}