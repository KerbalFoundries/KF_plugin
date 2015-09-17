using System;
using System.Linq;

namespace KerbalFoundries
{
	/// <summary>Sets a string that contains the Mod-name and the version number for use in Logs.</summary>
	/// <remarks>Rarely actually updated to reflect the mod version.</remarks>
	public static class Version
	{
		/// <summary>The release version number.</summary>
		public static double versionNumber = 1.9;
		
		/// <summary>The specific sub-release version letter.</summary>
		public static string versionLetter = "h";
		
		/// <summary>The completed string containing the release version number and the sub-release letter.</summary>
		/// <remarks>Format is "N.nl" where "N" is the major version, "n" is the minor version, and "l" is the lowercase sub-release letter.</remarks>
		public static string versionString = string.Format("{0}{1}", versionNumber, versionLetter);
	}
}
