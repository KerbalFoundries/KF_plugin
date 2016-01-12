
using System;

namespace KerbalFoundries.Rework
{
	/// <summary>
	/// The top-level class for all future KF-based modules.
	/// </summary>
	public class KFModule
	{
		string Name { get; set; }
		KFFunction Func { get; set; }
		
		public KFModule(string name, KFFunction func)
		{
			Name = name;
			Func = func;
		}
		
		public string GetFuncName()
		{
			return Func.GetFuncName;
		}
		
		public int GetFuncIndex()
		{
			return Func.GetIndex;
		}
	}
}