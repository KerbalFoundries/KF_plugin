using System.Collections.Generic;
using UnityEngine;

namespace KerbalFoundries
{
	/// <summary>
	/// A class built to handle the replacement of the 3D part icons in the editors with correctly positioned ones for skinned-mesh parts.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class KFIconFix : MonoBehaviour
	{
		/// <summary>Logging utility.</summary>
		/// <remarks>Call using "KFLog.log_type"</remarks>
		readonly KFLogUtil KFLog = new KFLogUtil("KFIconFix");
		
		void OnDestroy()// Last possible point before the loading scene switches to the Main Menu scene.
		{
			FindKFPartsToFix().ForEach(FixPartIcon);
		}
		
		/// <summary>Finds all KF parts which part icons need to be fixed</summary>
		/// <returns>List of parts with KFIconOverride configNode</returns>
		List<AvailablePart> FindKFPartsToFix()
		{
			List<AvailablePart> KFPartsList = PartLoader.LoadedPartsList.FindAll(IsAKFPart);
			
			#if DEBUG
			KFLog.Log("\nAll KF Parts:");
			KFPartsList.ForEach(part => KFLog.Log(string.Format("  {0}", part.name)));
			#endif
			
			List<AvailablePart> KFPartsToFixList = KFPartsList.FindAll(HasKFIconOverrideModule);
			
			#if DEBUG
			KFLog.Log("\nKF Parts which need an icon fix:");
			KFPartsToFixList.ForEach(part => KFLog.Log(string.Format("  {0}", part.name)));
			#endif
			
			return KFPartsToFixList;
		}
		
		/// <summary>Fixes incorrect part icon in the editor's parts list panel for every part which has a KFIconOverride node.
		/// The node can have several attributes.
		/// Example:
		/// KFIconOverride
		/// {
		///     Multiplier = 1.0      // for finetuning icon zoom
		///     Pivot = transformName // transform to rotate around; rotates around CoM if not specified
		///     Rotation = vector     // offset to rotation point
		/// }
		/// All parameters are optional. The existence of an KFIconOverride node is enough to fix the icon.
		/// Example:
		/// KFIconOverride {}
		/// </summary>
		/// <param name="partToFix">part to fix</param>
		/// <remarks>This method uses code from xEvilReepersx's PartIconFixer.
		/// See https://bitbucket.org/xEvilReeperx/ksp_particonfixer/src/7f2ac4094c19?at=master for original code and license.</remarks>
		void FixPartIcon(AvailablePart partToFix)
		{
			Vector3 rotation;
			float factor, max, multiplier;
			
			KFLog.Log(string.Format("Fixing icon of \"{0}\"", partToFix.name));
			
			// Preparations.
			GameObject partToFixIconPrefab = partToFix.iconPrefab;
			Bounds bounds = CalculateBounds(partToFixIconPrefab);
			
			// Retrieve icon fixes from cfg and calculate max part size.
			max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
			multiplier = 1f;
			
			if (HasKFIconOverrideMultiplier(partToFix))
				float.TryParse(partToFix.partConfig.GetNode("KFIconOverride").GetValue("Multiplier"), out multiplier);
			
			factor = 40f / max * multiplier;
			factor /= 40f;
			
			string pivot = string.Empty;
			if (HasKFIconOverridePivot(partToFix))
				pivot = partToFix.partConfig.GetNode("KFIconOverride").GetValue("Pivot");
			
			rotation = Vector3.zero;
			if (HasKFIconOverrideRotation(partToFix))
				rotation = KSPUtil.ParseVector3(partToFix.partConfig.GetNode("KFIconOverride").GetValue("Rotation"));
			
			// Apply icon fixes.
			partToFix.iconScale = max;
			
			// affects only the meter scale in the tooltip.
			partToFixIconPrefab.transform.GetChild(0).localScale *= factor;
			partToFixIconPrefab.transform.GetChild(0).Rotate(rotation, Space.Self);
			
			// After applying the fixes the part could be off-center, correct this now.
			if (string.IsNullOrEmpty(pivot))
			{
				Transform model, target;
				
				model = partToFixIconPrefab.transform.GetChild(0).Find("model");
				if (Equals(model, null))
					model = partToFixIconPrefab.transform.GetChild(0);
				
				target = model.Find(pivot);
				if (!Equals(target, null))
					partToFixIconPrefab.transform.GetChild(0).position -= target.position;
			}
			else
				partToFixIconPrefab.transform.GetChild(0).localPosition = Vector3.zero;
		}
		
		/// <summary>Checks if a part velongs to Kerbal Foundries.</summary>
		/// <param name="part">part to check</param>
		/// <returns>true if the part's name starts with "KF."</returns>
		/// <remarks>KSP converts underscores in part names to a dot ("_" -> ".").</remarks>
		bool IsAKFPart(AvailablePart part)
		{
			return part.name.StartsWith("KF.", System.StringComparison.Ordinal);
		}
		
		/// <summary>Checks if a part has an KFIconOverride node in it's config.</summary>
		/// <param name="part">Part to check.</param>
		/// <returns>True if a KFIconOverride node is there.</returns>
		bool HasKFIconOverrideModule(AvailablePart part)
		{
			return part.partConfig.HasNode("KFIconOverride");
		}
		
		/// <summary>Checks if there's a Multiplier node in KFIconOverride.</summary>
		/// <param name="part">part to check</param>
		/// <returns>True if IconOverride->Multiplier exists</returns>
		bool HasKFIconOverrideMultiplier(AvailablePart part)
		{
			return part.partConfig.GetNode("KFIconOverride").HasNode("Multiplier");
		}
		
		/// <summary>Checks if there's a Pivot node in KFIconOverride.</summary>
		/// <param name="part">Part to check.</param>
		/// <returns>True if KFIconOverride->Pivot exists.</returns>
		bool HasKFIconOverridePivot(AvailablePart part)
		{
			return part.partConfig.GetNode("KFIconOverride").HasNode("Pivot");
		}
		
		/// <summary>Checks if there's a Rotation node in KFIconOverride.</summary>
		/// <param name="part">Part to check.</param>
		/// <returns>True if KFIconOverride->Rotation exists.</returns>
		bool HasKFIconOverrideRotation(AvailablePart part)
		{
			return part.partConfig.GetNode("KFIconOverride").HasNode("Rotation");
		}
		
		/// <summary>Calculates the bounds of a game object.</summary>
		/// <param name="partGO">part which bounds have to be calculated</param>
		/// <returns>bounds</returns>
		/// <remarks>This code is copied from xEvilReepersx's PartIconFixer and is slightly modified.
		/// See https://bitbucket.org/xEvilReeperx/ksp_particonfixer/src/7f2ac4094c19?at=master for original code and license.</remarks>
		Bounds CalculateBounds(GameObject partGO)
		{
			var renderers = new List<Renderer>(partGO.GetComponentsInChildren<Renderer>(true));
			
			if (Equals(renderers.Count, 0))
				return default(Bounds);
			
			var boundsList = new List<Bounds>();
			
			renderers.ForEach(thisRenderer =>
			{
				// disable once CanBeReplacedWithTryCastAndCheckForNull
				if (thisRenderer is SkinnedMeshRenderer)
				{
					var skinnedMeshRenderer = (SkinnedMeshRenderer)thisRenderer;
					var thisMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(thisMesh);
					
					Matrix4x4 m = Matrix4x4.TRS(skinnedMeshRenderer.transform.position, skinnedMeshRenderer.transform.rotation, Vector3.one);
					
					var meshVertices = thisMesh.vertices;
					var skinnedMeshBounds = new Bounds(m.MultiplyPoint3x4(meshVertices[0]), Vector3.zero);
					
					for (int i = 1; i < meshVertices.Length; ++i)
						skinnedMeshBounds.Encapsulate(m.MultiplyPoint3x4(meshVertices[i]));
					Destroy(thisMesh);
					
					if (Equals(thisRenderer.tag, "Icon_Hidden"))
						Destroy(thisRenderer);
					
					boundsList.Add(skinnedMeshBounds);
				}
				else
				{
					var meshRenderer = (MeshRenderer)thisRenderer;
					if (!Equals(meshRenderer, null))
					{
						thisRenderer.gameObject.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
						boundsList.Add(thisRenderer.bounds);
					}
				}
			});
			
			Bounds partBounds = boundsList[0];
			boundsList.RemoveAt(0);
			
			// disable ConvertClosureToMethodGroup
			boundsList.ForEach(b => partBounds.Encapsulate(b));
			return partBounds;
		}
	}
}