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
		// disable MemberCanBeMadeStatic.Local
		
		public string strModName = "Kerbal Foundries";
		
		/// <summary>A standard-level log utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Standard log entry.</remarks>
		public void Log(string strText, string strClassName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
				strOutput += string.Format(" - {0}()", strClassName);
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput);
		}
		
		/// <summary>A warning-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Warning log entry.</remarks>
		public void Warning(string strText, string strClassName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
				strOutput += string.Format(" - {0}()", strClassName);
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput, 1);
		}
		
		/// <summary>An error-level logging utility that prefixes the logged text with the name of the mod it is being sent from.</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		/// <remarks>Error log entry.</remarks>
		public void Error(string strText, string strClassName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
			{
				strOutput += string.Format(" - {0}()", strClassName);
			}
			strOutput += string.Format("]: {0}", strText);
			KFLogIt(strOutput, 2);
		}
		
		/// <summary>A DEBUG logging utility that prefixes the logged text with the name of the mod it is being sent from and a suffix that labels it as a DEBUG message..</summary>
		/// <param name="strText">(Required) The text to be sent to the log.</param>
		/// <param name="strClassName">(Optional) Name of the specific class that is being logged from.  Does not show if not specified.</param>
		public void Debug(string strText, string strClassName = "")
		{
			string strOutput = string.Format("[{0}", strModName);
			if (!Equals(strClassName, ""))
			{
				strOutput += string.Format(" - {0}()", strClassName);
			}
			strOutput += string.Format(" : DEBUG]: {0}", strText);
			KFLogIt(strOutput);
		}
		
		/// <summary>
		/// This is the method that actually posts the logs.  Cannot be called from outside this class.</summary>
		/// <param name="strLogMessage">The message from the formatting method (Log, Error, Warning, or Debug).</param>
		/// <param name="iLogtype">(Optional) The type of log being posted.  Defaults to 0 (standard-level log entry).</param>
		/// <remarks>"logtype" can be an integer of 0, 1, or 2 which corresponds with standard, warning, or error logs respectively.</remarks>
		void KFLogIt(string strLogMessage, int iLogtype = 0)
		{
			switch (iLogtype)
			{
				case 0:
					UnityEngine.Debug.Log(strLogMessage);
					break;
				case 1:
					UnityEngine.Debug.LogWarning(strLogMessage);
					break;
				case 2:
					UnityEngine.Debug.LogError(strLogMessage);
					break;
				default:
					UnityEngine.Debug.Log(strLogMessage);
					break;
			}
		}
	}
}