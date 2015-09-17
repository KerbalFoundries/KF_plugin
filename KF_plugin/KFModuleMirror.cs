using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>Handles mirroring of compatible parts where the model is not symmetrical.</summary>
	[KSPModule("ModuleMirror")]
	public class KFModuleMirror : PartModule
	{
		public string right = "right";
		public string left = "left";
		public string swap = "swap";
		
		[KSPField(isPersistant = true)]
		public string cloneSide;
		
		[KSPField(isPersistant = true)]
		public string flightSide;
		
		public KFModuleMirror clone;
		
		[KSPField]
		public string leftObjectName;
		[KSPField]
		public string rightObjectName;
		
		List<Transform> leftObject = new List<Transform>();
		List<Transform> rightObject = new List<Transform>();
		string[] rightList;
		string[] leftList;
		
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFModuleMirror");
		
		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			
			leftList = leftObjectName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries); //Thanks, Mihara!
			rightList = rightObjectName.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
			
			#if DEBUG
			KFLog.Log(string.Format("{0} {1}", leftList[0], leftList.Count()));
			KFLog.Log(string.Format("{0} {1}", rightList[0], rightList.Count()));
			#endif
			
			for (int i = 0; i < leftList.Count(); i++)
			{
				leftObject.Add(transform.Search(leftList[i]));
				
				#if DEBUG
				KFLog.Log(string.Format("Iterated left: {0}", i));
				#endif
			}
			for (int i = 0; i < rightList.Count(); i++)
			{
				rightObject.Add(transform.Search(rightList[i]));
				
				#if DEBUG
				KFLog.Log(string.Format("Iterated right: {0}", i));
				#endif
			}
			
			#if DEBUG
			KFLog.Log("Loaded scene is editor.");
			KFLog.Log(string.Format("\"flightside\" = {0}", flightSide));
			#endif
			
			FindClone();
			if (!Equals(clone, null))
			{
				#if DEBUG
				KFLog.Log("Part is clone.");
				#endif
				
				SetSide(clone.cloneSide);
			}

			if (Equals(flightSide, string.Empty))
			{
				#if DEBUG
				KFLog.Log(string.Format("No flightSide value in persistence. Sertting default: {0}", part.isClone));
				#endif
				
				LeftSide();
			}
			else
			{
				#if DEBUG
				KFLog.Log("Setting value from persistence.");
				#endif
				
				SetSide(flightSide);
			}
            
			if (HighLogic.LoadedSceneIsFlight) // do this last.
			{
				#if DEBUG 
				KFLog.Log("Loaded scene is flight.");
				#endif
				
				if (Equals(flightSide, left))
				{
					for (int i = 0; i < rightObject.Count(); i++)
					{
						#if DEBUG
						KFLog.Log(string.Format("Destroying Right object: {0}", rightList[i]));
						#endif
						
						leftObject[i].gameObject.SetActive(true);
						UnityEngine.Object.DestroyImmediate(rightObject[i].gameObject);
					}
				}
				if (Equals(flightSide, right))
				{
					
					for (int i = 0; i < leftObject.Count(); i++)
					{
						#if DEBUG
						KFLog.Log(string.Format("Destroying left object: {0}", leftList[i]));
						#endif
						
						rightObject[i].gameObject.SetActive(true);
						UnityEngine.Object.DestroyImmediate(leftObject[i].gameObject);
					}
				}
			}
		}
		
		/// <summary>Sets this side to left and clone to right.</summary>
		[KSPEvent(guiName = "Left", guiActive = false, guiActiveEditor = true)]
		public void LeftSide()
		{
			FindClone();
			SetSide(left);
			if (clone)
				clone.SetSide(right);
		}
		
		/// <summary>Sets this side to right and clone to left.</summary>
		[KSPEvent(guiName = "Right", guiActive = false, guiActiveEditor = true)]
		public void RightSide()
		{
			FindClone();
			SetSide(right);
			if (clone)
				clone.SetSide(left);
		}

		public void SetSide(string side)
		{
			if (Equals(side, left))
			{
				for (int i = 0; i < leftList.Count(); i++)
				{
					rightObject[i].gameObject.SetActive(false);
					leftObject[i].gameObject.SetActive(true);
				}
				cloneSide = right;
				flightSide = side;
				Events["LeftSide"].active = false;
				Events["RightSide"].active = true;
			}
			if (Equals(side, right))
			{
				for (int i = 0; i < leftList.Count(); i++)
				{
					rightObject[i].gameObject.SetActive(true);
					leftObject[i].gameObject.SetActive(false);
				}
				cloneSide = left;
				flightSide = side;
				Events["LeftSide"].active = true;
				Events["RightSide"].active = false;
			}
		}

		public void FindClone()
		{
			foreach (Part potentialMaster in this.part.symmetryCounterparts)
			{
				if (!Equals(potentialMaster, null))
				{
					clone = potentialMaster.Modules.OfType<KFModuleMirror>().FirstOrDefault();
					
					#if DEBUG
					KFLog.Log("Found my clone!");
					#endif
				}
			}
		}
	}
}
