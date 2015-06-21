using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
    public static class Extensions
    {
        // disable EmptyGeneralCatchClause

        public static String[] SplitString(string ObjectNames)
        {
            
            //rotators.Clear();
            String[] nameList = ObjectNames.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);

            return nameList;
        }
        
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
        //tr.name.StartsWith(rotatorsName, StringComparison.Ordinal

		/// <summary>Gets a battery.  Names it "Jennifer" and gives it a good home.</summary>
		/// <remarks>My father once played a game where he had to choose a mascot.  He grabbed a dead battery and named it "Jennifer" and the story never gets old. - Gaalidas</remarks>
        public static float GetBattery(Part part)
        {
            PartResourceDefinition resourceDefinitions = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
			var resources = new List<PartResource>();
            //List<PartResource> resources = new List<PartResource>();
            part.GetConnectedResources(resourceDefinitions.id, resourceDefinitions.resourceFlowMode, resources);
			var ratio = (float)resources.Sum(r => r.amount) / (float)resources.Sum(r => r.maxAmount);
            return ratio;
        }

		/// <summary>Converts the textual axis to an integer index.</summary>
        public static int SetAxisIndex(string axisString)
        {
			int index = 1; // Default to Y
			switch (axisString)
			{
				case "x":
				case "X":
                	index = 0;
					break;
				case "y": 
				case "Y":
                	index = 1;
					break;
				case "z":
				case "Z":
                	index = 2;
					break;
				default: // Supposedly it's a good idea to always provide a default in these switches. - Gaalidas
					index = 1;
					break;
			}
            return index;
        }

		// I had to rename "parta" to "thePart" so it wouldn't look so much like a typo.  I always had issues when looking over it before. - Gaalidas
		public static void PlaySound(Part thePart, string effectName, float effectPower)
        {
			thePart.Effect(effectName, effectPower);
        }

        public static void DisableAnimateButton(Part part)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (ModuleAnimateGeneric ma in part.FindModulesImplementing<ModuleAnimateGeneric>())
                {
                    try
                    {
                        ma.Actions["ToggleAction"].active = false;
                    }
                    catch
                    {
                        //do nothing
                    }
                    try
                    {
                        ma.Events["Toggle"].guiActive = false;
                    }
                    catch
                    {
                        //do nothing
                    }
                }
            }
        }

        public static string GetLast(this string source, int tail_length)
        {
			return tail_length >= source.Length ? source : source.Substring(source.Length - tail_length);
        }
    }
}
