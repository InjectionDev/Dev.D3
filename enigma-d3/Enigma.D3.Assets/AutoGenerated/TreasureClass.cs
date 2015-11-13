using System.Runtime.CompilerServices;
using Enigma.D3.DataTypes;
using Enigma.Memory;

namespace Enigma.D3.Assets
{
	[CompilerGenerated]
	public partial class TreasureClass : SerializeMemoryObject
	{
		public const int SizeOf = 0x28; // 40
		
		public SnoHeader x00_Header { get { return Read<SnoHeader>(0x00); } }
		public HighPrecisionPercent x0C_HighPrecisionPercent { get { return Read<HighPrecisionPercent>(0x0C); } }
		public int x10 { get { return Read<int>(0x10); } }
		public int x14 { get { return Read<int>(0x14); } }
		public LootDropModifier[] x18_LootDropModifiers { get { return Deserialize<LootDropModifier>(x20_SerializeData); } }
		public SerializeData x20_SerializeData { get { return Read<SerializeData>(0x20); } }
		
		[CompilerGenerated]
		public partial class LootDropModifier : MemoryObject
		{
			public const int SizeOf = 0x84; // 132
			
			public int x00 { get { return Read<int>(0x00); } }
			public Sno x04_TreasureClassSno { get { return Read<Sno>(0x04); } }
			public HighPrecisionPercent x08_HighPrecisionPercent { get { return Read<HighPrecisionPercent>(0x08); } }
			public int x0C { get { return Read<int>(0x0C); } }
			public GameBalanceId x10_QualityClassesGameBalanceId { get { return Read<GameBalanceId>(0x10); } }
			public int x14 { get { return Read<int>(0x14); } }
			public int x18 { get { return Read<int>(0x18); } }
			public Sno x1C_ConditionSno { get { return Read<Sno>(0x1C); } }
			public ItemSpecifierData x20_ItemSpecifierData { get { return Read<ItemSpecifierData>(0x20); } }
			public int x58 { get { return Read<int>(0x58); } }
			public int x5C { get { return Read<int>(0x5C); } }
			public int[] x60_int { get { return Read<int>(0x60, 5); } }
			public int x74 { get { return Read<int>(0x74); } }
			public int x78 { get { return Read<int>(0x78); } }
			public float x7C { get { return Read<float>(0x7C); } }
			public int x80 { get { return Read<int>(0x80); } }
		}
		
		[CompilerGenerated]
		public partial class ItemSpecifierData : MemoryObject
		{
			public const int SizeOf = 0x38; // 56
			
			public GameBalanceId x00_ItemsGameBalanceId { get { return Read<GameBalanceId>(0x00); } }
			public int x04 { get { return Read<int>(0x04); } }
			public GameBalanceId[] x08_GameBalanceIds { get { return Read<GameBalanceId>(0x08, 6); } }
			public int x20 { get { return Read<int>(0x20); } }
			public int x24 { get { return Read<int>(0x24); } }
			public int x28 { get { return Read<int>(0x28); } }
			public int x2C { get { return Read<int>(0x2C); } }
			public int x30 { get { return Read<int>(0x30); } }
			public int x34 { get { return Read<int>(0x34); } }
		}
	}
}
