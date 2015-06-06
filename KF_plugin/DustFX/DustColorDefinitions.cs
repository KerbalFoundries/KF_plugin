using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFDustColorDefinitions : IPersistenceLoad, IPersistenceSave
	{
		// disable MemberCanBeMadeStatic.Local
		
		/// <summary>Prefix the logs so we can identify the class producing them.</summary>
		public string logprefix = "[DustFX - DustColorDefinitions]: ";
		
		/// <summary>Persistent list of BodyDustColors.</summary>
		[Persistent]
		List<KFBiomeDustColorInfo> BodyDustColors = new List<KFBiomeDustColorInfo> ();
		
		/// <summary>The default color to be used in case no other is defined.</summary>
		[Persistent]
		public Color DefaultColor;
		
		/// <summary>Dictionary containing all of our available color configurations.</summary>
		Dictionary<CelestialBody, KFBiomeDustColorInfo> _dustColors = new Dictionary<CelestialBody, KFBiomeDustColorInfo> ();
		
		/// <summary>Method used to get the color we need for a specific body or biome.</summary>
		/// <param name="body">The planetary body we are calling from.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="lat">Latitude of the colliding part.</param>
		/// <param name="lon">Longitude of the colliding part.</param>
		/// <returns>The color that corresponds to the body or biome in the configs.</returns>
		public Color GetDustColor ( CelestialBody body, Collider col, double lat, double lon )
		{
			KFBiomeDustColorInfo biomeColors;
			if (!_dustColors.TryGetValue(body, out biomeColors))
				return DefaultColor;
			string BodyOrBiome = string.Empty;
			if (!Equals(FlightGlobals.ActiveVessel, null))
				BodyOrBiome = IsPQS(col) ? GetCurrentBiomeName(lat, lon) : FlightGlobals.ActiveVessel.mainBody.name;
			return Equals(BodyOrBiome, string.Empty) ? DefaultColor : biomeColors.GetDustColor(BodyOrBiome);
		}
		
		/// <summary>Sets whether or not we are colliding with the world.</summary>
		/// <param name="col">The collider that is being checked.</param>
		/// <returns>True if we are colliding with the planet, false otherwise.</returns>
		public bool IsPQS ( Collider col )
		{
			if (Equals(col, null))
				return false;
			// Test for PQS: Name in the form "Ab0123456789."
			Int64 n;
			bool result = Equals(col.name.Length, 12) && Int64.TryParse(col.name.Substring(2, 10), out n);
			return result;
		}
		
		/// <summary>Gets the biome of the current colliding part.</summary>
		/// <param name="lat">Latitude of the vessel.</param>
		/// <param name="lon">Longitude of the vessel.</param>
		/// <returns>The name of the biome it finds.</returns>
		public string GetCurrentBiomeName ( double lat, double lon )
		{
			CBAttributeMapSO biomeMap = FlightGlobals.currentMainBody.BiomeMap;
			CBAttributeMapSO.MapAttribute mapAttribute = biomeMap.GetAtt(lat * Mathf.Deg2Rad, lon * Mathf.Deg2Rad);
			string result = mapAttribute.name;
			return result;
		}
		
		/// <summary>Persistence loader for our color configurations.</summary>
		public void PersistenceLoad ()
		{
			_dustColors = BodyDustColors.Where(info => FlightGlobals.Bodies.Any(b => Equals(b.bodyName, info.Name))).ToDictionary(info => FlightGlobals.Bodies.Single(b => Equals(b.bodyName, info.Name)), c => c);
		}
		
		/// <summary>Persistence save method for our color configurations.</summary>
		public void PersistenceSave ()
		{
			if (!BodyDustColors.Any())
				BodyDustColors = FlightGlobals.Bodies.Where(cb => !Equals(cb.BiomeMap, null)).Select(b => new KFBiomeDustColorInfo { DefaultColor = DefaultColor, Name = b.bodyName, BiomeColors = b.BiomeMap.Attributes.GroupBy(attr => attr.name).Select(group => group.First()).Select(attr => new KFBiomeDustColorInfo { Color = attr.mapColor, Name = attr.name }).ToList() }).ToList();
		}
	}
}
