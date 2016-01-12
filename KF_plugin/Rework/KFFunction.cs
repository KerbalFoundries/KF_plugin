using System;

namespace KerbalFoundries.Rework
{
	/// <summary>
	/// A definition for a function of a specific KF modules.
	/// </summary>
	public class KFFunction
	{
		string Name
		{
			get;
			set;
		}

		int Func
		{
			get;
			set;
		}

		public KFFunction(string name, int func)
		{
			Name = name;
			Func = func;
		}
		
		public string GetFuncName()
		{
			return Name;
		}
		
		public int GetIndex()
		{
			return Func;
		}
	}
}