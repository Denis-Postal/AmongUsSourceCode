using System;

namespace InnerNet
{
	[Flags]
	public enum GameKeywords : uint
	{
		All = 0u,
		AllLanguages = 0x1Fu,
		English = 1u,
		Spanish = 2u,
		Korean = 4u,
		Russian = 8u,
		Portuguese = 0x10u
	}
}
