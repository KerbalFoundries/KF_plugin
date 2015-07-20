using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalFoundries
{
	public static class KFDustFXUtils
	{
		/// <summary>Method used to get the color we need for a specific body or biome.</summary>
		/// <param name="body">The planetary body we are calling from.</param>
		/// <param name="col">The collider being referenced.</param>
		/// <param name="lat">Latitude of the colliding part.</param>
		/// <param name="lon">Longitude of the colliding part.</param>
		/// <returns>The color that corresponds to the body or biome in the configs.</returns>
		public static Color GetDustColor(CelestialBody body, Collider col, double lat, double lon)
		{
			if (!Equals(FlightGlobals.ActiveVessel, null))
			{
				if (IsColliding(col)) // are we touching the ground?
				{
					Dictionary<string, Color> biomeColors = KFPersistenceManager.DustColors[body.name];
					if (biomeColors.Count > 0) // do we have biome color definitions for that body?
					{
						string biomeName = GetCurrentBiomeName(lat, lon); // get biome name
						if (string.IsNullOrEmpty(biomeName)) // do we have a color for that biome?
                            biomeName = "default"; // no -> use default color of body
						return biomeColors[biomeName];
					}
				}
			}
			return KFPersistenceManager.DefaultDustColor; // no color found or conditions aren't met, use default color
		}
		
		/// <summary>Sets whether or not we are colliding with the world.</summary>
		/// <param name="col">The collider that is being checked.</param>
		/// <returns>True if we are colliding with the planet, false otherwise.</returns>
		static bool IsColliding(UnityEngine.Object col)
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
		static string GetCurrentBiomeName(double lat, double lon)
		{
			CBAttributeMapSO biomeMap = FlightGlobals.currentMainBody.BiomeMap;
			CBAttributeMapSO.MapAttribute mapAttribute = biomeMap.GetAtt(lat * Mathf.Deg2Rad, lon * Mathf.Deg2Rad);
			string result = mapAttribute.name.Replace(' ', '_');
			return result;
		}
	}
}
