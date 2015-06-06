using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalFoundries
{
	public class KFBiomeDustColorInfo : IPersistenceLoad
	{
		/// <summary>Prefix the logs with this to identify it.</summary>
		public string logprefix = "[DustFX - BiomeDustColorInfo]: ";
		
		/// <summary>Persistent "name" identification.</summary>
		[Persistent]
		public string Name;
		
		/// <summary>Persistent "color" identification.</summary>
		[Persistent]
		public Color Color;
		
		/// <summary>Persistent default color definition in case no other color settings can be located.</summary>
		[Persistent]
		public Color DefaultColor;
        
		/// <summary>Persistent biome color list.</summary>
		[Persistent]
		public List<KFBiomeDustColorInfo> BiomeColors = new List<KFBiomeDustColorInfo> ();
		
		/// <summary>Dictionary containing all of our color configurations.</summary>
		Dictionary<string, Color> _colorDictionary = new Dictionary<string, Color> ();
 		
		/// <summary>Persistence loader for our color configurations.</summary>
		public void PersistenceLoad ()
		{
			_colorDictionary = BiomeColors.ToDictionary(ci => ci.Name, ci => ci.Color);
		}
		
		/// <summary>The function called by other classes which need to discover the color pertaining to their discovered body or biome.</summary>
		/// <param name="name">Name of the location we are attempting to compare with the configs.</param>
		public Color GetDustColor ( string name )
		{
			Color color;
			return _colorDictionary.TryGetValue(name, out color) ? color : DefaultColor;
		}
	}
}
