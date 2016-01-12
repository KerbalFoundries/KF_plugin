using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>A set of extension methods used frequently in the rest of the project.</summary>
	public static class KFExtensions
	{
		// disable EmptyGeneralCatchClause
		// disable UnusedParameter
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		static readonly KFLogUtil KFLog = new KFLogUtil("KFExtensions");
		
		public static void DebugLine(Vector3 position, Vector3 rotation)
		{
			var lineDebugX = new GameObject("lineDebug");
			var lineDebugY = new GameObject("lineDebug");
			var lineDebugZ = new GameObject("lineDebug");
			
			lineDebugX.transform.position = position;
			lineDebugY.transform.position = position;
			lineDebugZ.transform.position = position;
			
			LineRenderer lineX = lineDebugX.AddComponent<LineRenderer>();
			LineRenderer lineY = lineDebugY.AddComponent<LineRenderer>();
			LineRenderer lineZ = lineDebugZ.AddComponent<LineRenderer>();
			
			lineX.useWorldSpace = false;
			lineX.material = new Material(Shader.Find("Particles/Additive"));
			lineX.SetColors(Color.red, Color.white);
			lineX.SetWidth(0.1f, 0.1f);
			lineX.SetVertexCount(2);
			lineX.SetPosition(0, Vector3.zero);
			lineX.SetPosition(1, Vector3.right * 10);
			
			lineY.useWorldSpace = false;
			lineY.material = new Material(Shader.Find("Particles/Additive"));
			lineY.SetColors(Color.green, Color.white);
			lineY.SetWidth(0.1f, 0.1f);
			lineY.SetVertexCount(2);
			lineY.SetPosition(0, Vector3.zero);
			lineY.SetPosition(1, Vector3.up * 10);
			
			lineZ.useWorldSpace = false;
			lineZ.material = new Material(Shader.Find("Particles/Additive"));
			lineZ.SetColors(Color.blue, Color.white);
			lineZ.SetWidth(0.1f, 0.1f);
			lineZ.SetVertexCount(2);
			lineZ.SetPosition(0, Vector3.zero);
			lineZ.SetPosition(1, Vector3.forward * 10);
		}
		
		/// <summary>Splits strings.</summary>
		/// <param name="ObjectNames">Names of the objects to split.</param>
		/// <remarks>Kinda like splitting hairs... but not really.</remarks>
		/// <returns>The names in a list format.</returns>
		public static String[] SplitString(this string ObjectNames)
		{
			String[] nameList = ObjectNames.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
			return nameList;
		}
        
		/// <summary>Seeks out the names of transforms/objects within the model, under the specified transform, and matches them against a common naming convention.</summary>
		/// <param name="target">The target transform.</param>
		/// <param name="name">The name to match other transforms/objects with.</param>
		/// <returns>The resulting transform/object if found, null otherwise.</returns>
		public static Transform Search(this Transform target, string name)
		{
			if (Equals(target.name, name))
				return target;
			
			for (int i = 0; i < target.childCount; ++i)
			{
				var result = Search(target.GetChild(i), name);
				if (!Equals(result, null))
					return result;
			}
			return null;
		}
		
		/// <summary>A transform name search specifically made to use with the Texture Anumation module.</summary>
		/// <param name="target">A target transform in which to begin the search.</param>
		/// <param name="name">The name to match to the child objects.</param>
		/// <returns>The resulting transform/object if found, null otherwise.</returns>
		public static Transform TexAnimSearch(this Transform target, string name)
		{
			Transform result;
			if (Equals(target.name, name))
				result = target;
			else
			{
				for (int i = 0; i < target.childCount; i++)
				{
					Transform transform = target.GetChild(i).Search(name);
					if (!Equals(transform, null))
					{
						result = transform;
						return result;
					}
				}
			}
			return null;
		}
		
		/// <summary>Seeks out the names of transforms/objects within the model, under the specified transform, and matches them against a common naming convention.</summary>
		/// <param name="target">The target transform.</param>
		/// <param name="name">The name to match other transforms/objects with.</param>
		/// <returns>The resulting transform/object if found, null otherwise.</returns>
		public static Transform SearchStartsWith(this Transform target, string name)
		{
			if (target.name.StartsWith(name, StringComparison.Ordinal))
				return target;
			
			for (int i = 0; i < target.childCount; ++i)
			{
				var result = SearchStartsWith(target.GetChild(i), name);
				if (!Equals(result, null))
					return result;
			}
			return null;
		}
		
		/// <summary>Gets a battery to check its power supply.</summary>
		public static float GetBattery(this Part part)
		{
			PartResourceDefinition resourceDefinitions = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
			var resources = new List<PartResource>();
			
			part.GetConnectedResources(resourceDefinitions.id, resourceDefinitions.resourceFlowMode, resources);
			var ratio = (float)resources.Sum(r => r.amount) / (float)resources.Sum(r => r.maxAmount);
			return ratio;
		}
		
		/// <summary>Converts the textual axis to an integer index axis.</summary>
		/// <param name="axisString">Text axis.</param>
		/// <returns>Integer axis index.</returns>
		public static int SetAxisIndex(this string axisString)
		{
			switch (axisString)
			{
				case "X":
				case "x":
					return 0;
				case "Y":
				case "y":
					return 1;
				case "Z":
				case "z":
					return 2;
				default:
					return 0;
			}
		}
		
		#region Overloads for SetAxisString
		
		public static int SetAxisString(this char axisChar)
		{
			return axisChar.ToString().SetAxisIndex();
		}
		
		#endregion
		
		/// <summary>Disables the "animate" button in the part context menu.</summary>
		/// <param name="part">Part reference to affect.</param>
		public static void DisableAnimateButton(this Part part)
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
				{
					ma.Actions["ToggleAction"].active = false;
					ma.Events["Toggle"].guiActive = false;
				}
			}
		}
		
		/// <summary>Shortcut to rounding a value to the nearest number.</summary>
		/// <param name="input">Value to be rounded.</param>
		/// <param name="roundto">Rounds the value to the nearest "roundto".  If value is between 0 and 1 (non-inclusive) then we will round to that decimal, otherwise we round to nearest while interval of "roundto".</param>
		/// <returns>Nearest "roundto" in relation to "value"</returns>
		public static float RoundToNearestValue(this float input, float roundto)
		{
			float output;
			if (roundto < 1f && roundto > 0f)
			{
				roundto *= 10f;
				output = (float)((Math.Round(((input * 10f) / roundto), MidpointRounding.AwayFromZero) * roundto) / 10f);
			}
			else if (roundto >= 1f)
				output = (float)(Math.Round(input / roundto, MidpointRounding.AwayFromZero) * roundto);
			else
			{
				output = (float)(Math.Round(input, MidpointRounding.AwayFromZero));
				KFLog.Error("Invalid float entered for rounding value.  Defaulting to nearest whole integer.");
			}
			return output;
		}
		
		#region Overloads for RoundToNearestValue
		
		public static float RoundToNearestValue(this double input, double roundto)
		{
			float input2 = (float)input;
			float roundto2 = (float)roundto;
			return input2.RoundToNearestValue(roundto2);
		}
		
		public static float RoundToNearestValue(this int input, int roundto)
		{
			float input2 = (float)input;
			float roundto2 = (float)roundto;
			return input2.RoundToNearestValue(roundto2);
		}
		
		public static float RoundToNearestValue(this decimal input, decimal roundto)
		{
			float input2 = (float)input;
			float roundto2 = (float)roundto;
			return input2.RoundToNearestValue(roundto2);
		}
		
		#endregion
	}
}
