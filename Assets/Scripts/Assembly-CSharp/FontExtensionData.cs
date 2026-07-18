using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class FontExtensionData : ScriptableObject
{
	public string FontName;

	public List<KerningPair> kernings = new List<KerningPair>();

	public List<OffsetAdjustment> Offsets = new List<OffsetAdjustment>();

	public void AdjustKernings(FontData target)
	{
		for (int i = 0; i < kernings.Count; i++)
		{
			KerningPair kerningPair = kernings[i];
			if (target.kernings.TryGetValue(kerningPair.First, out var value))
			{
				if (value.TryGetValue(kerningPair.Second, out var value2))
				{
					value[kerningPair.Second] = value2 + (float)kerningPair.Pixels;
				}
				else
				{
					value[kerningPair.Second] = kerningPair.Pixels;
				}
			}
			else
			{
				Dictionary<int, float> dictionary = new Dictionary<int, float>();
				dictionary[kerningPair.Second] = kerningPair.Pixels;
				target.kernings[kerningPair.First] = dictionary;
			}
		}
	}

	public void AdjustOffsets(FontData target)
	{
		for (int i = 0; i < Offsets.Count; i++)
		{
			OffsetAdjustment offsetAdjustment = Offsets[i];
			if (target.charMap.TryGetValue(offsetAdjustment.Char, out var value))
			{
				Vector3 value2 = target.offsets[value];
				value2.x += offsetAdjustment.OffsetX;
				value2.y += offsetAdjustment.OffsetY;
				target.offsets[value] = value2;
			}
		}
	}
}
