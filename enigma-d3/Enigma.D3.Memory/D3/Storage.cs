using Enigma.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enigma.D3.Memory;
using Enigma.D3.Collections;

namespace Enigma.D3
{
	public partial class Storage : MemoryObject
	{
		public const int SizeOf = 0x1A4; // 420 // Unsure about full size

		public int x000 { get { return Read<int>(0x000); } }
		public int x004_GameDifficulty_Handicap { get { return Read<int>(0x004); } }
		public int x008 { get { return Read<int>(0x008); } }
		public int x00C { get { return Read<int>(0x00C); } }
		public int x010 { get { return Read<int>(0x010); } }
		public int x014 { get { return Read<int>(0x014); } }
		public int _x018 { get { return Read<int>(0x018); } }
		public int x01C { get { return Read<int>(0x01C); } }
		public int _x020 { get { return Read<int>(0x020); } }
		public int x024 { get { return Read<int>(0x024); } }
		public int x028_Flags_StructStart_144Bytes { get { return Read<int>(0x028); } }
		public int x02C_ActId_400Lobby_0InGame { get { return Read<int>(0x02C); } }
		public int x030_GameLevel { get { return Read<int>(0x030); } }
		public int _x034 { get { return Read<int>(0x034); } }
		public int _x038 { get { return Read<int>(0x038); } }
		public int _x03C { get { return Read<int>(0x03C); } }
		public int _x040 { get { return Read<int>(0x040); } }
		public int _x044 { get { return Read<int>(0x044); } }
		public int _x048 { get { return Read<int>(0x048); } }
		public int _x04C { get { return Read<int>(0x04C); } }
		public int _x050 { get { return Read<int>(0x050); } }
		public int _x054 { get { return Read<int>(0x054); } }
		public int _x058 { get { return Read<int>(0x058); } }
		public int _x05C { get { return Read<int>(0x05C); } }
		public int _x060 { get { return Read<int>(0x060); } }
		public int _x064 { get { return Read<int>(0x064); } }
		public int _x068 { get { return Read<int>(0x068); } }
		public int _x06C { get { return Read<int>(0x06C); } }
		public int _x070 { get { return Read<int>(0x070); } }
		public int _x074 { get { return Read<int>(0x074); } }
		public int _x078 { get { return Read<int>(0x078); } }
		public int _x07C { get { return Read<int>(0x07C); } }
		public int _x080 { get { return Read<int>(0x080); } }
		public int _x084 { get { return Read<int>(0x084); } }
		public int _x088 { get { return Read<int>(0x088); } }
		public int _x08C { get { return Read<int>(0x08C); } }
		public int _x090 { get { return Read<int>(0x090); } }
		public int _x094 { get { return Read<int>(0x094); } }
		public int _x098 { get { return Read<int>(0x098); } }
		public int _x09C { get { return Read<int>(0x09C); } }
		public int _x0A0 { get { return Read<int>(0x0A0); } }
		public int _x0A4 { get { return Read<int>(0x0A4); } }
		public int _x0A8 { get { return Read<int>(0x0A8); } }
		public int _x0AC { get { return Read<int>(0x0AC); } }
		public int _x0B0 { get { return Read<int>(0x0B0); } }
		public int _x0B4 { get { return Read<int>(0x0B4); } }
		public int _x0B8 { get { return Read<int>(0x0B8); } }
		public int _x0BC { get { return Read<int>(0x0BC); } }
		public int _x0C0 { get { return Read<int>(0x0C0); } }
		public int _x0C4 { get { return Read<int>(0x0C4); } }
		public int _x0C8 { get { return Read<int>(0x0C8); } }
		public int _x0CC { get { return Read<int>(0x0CC); } }
		public int _x0D0 { get { return Read<int>(0x0D0); } }
		public int _x0D4 { get { return Read<int>(0x0D4); } }
		public int _x0D8 { get { return Read<int>(0x0D8); } }
		public int _x0DC { get { return Read<int>(0x0DC); } }
		public int _x0E0 { get { return Read<int>(0x0C0); } }
		public int _x0E4 { get { return Read<int>(0x0C4); } }
		public int _x0E8 { get { return Read<int>(0x0C8); } }
		public int _x0EC { get { return Read<int>(0x0CC); } }
		public int _x0F0 { get { return Read<int>(0x0D0); } }
		public int _x0F4 { get { return Read<int>(0x0D4); } }
		public int _x0F8 { get { return Read<int>(0x0D8); } }
		public int _x0FC { get { return Read<int>(0x0DC); } }
		public int _x100 { get { return Read<int>(0x100); } }
		public int _x104 { get { return Read<int>(0x104); } }
		public int _x108 { get { return Read<int>(0x108); } }
		public int _x10C { get { return Read<int>(0x10C); } }
		public int _x110 { get { return Read<int>(0x110); } }
		public int _x114 { get { return Read<int>(0x114); } }
		public int x118_GameTick { get { return Read<int>(0x118); } }
		public int x11C { get { return Read<int>(0x11C); } }
		public int x120 { get { return Read<int>(0x120); } }
		public Ptr<MarkerManager> x124_Ptr_12Bytes_Markers { get { return ReadPointer<MarkerManager>(0x124); } }
		public Ptr<InactiveMarkerManager> x128_Ptr_368Bytes_InactiveMarkers { get { return ReadPointer<InactiveMarkerManager>(0x128); } }
		public Ptr<PlayerDataManager> x12C_Ptr_PlayerDataManager { get { return ReadPointer<PlayerDataManager>(0x12C); } }
		public Ptr<SceneAllocators> x130_Ptr_116Bytes_SceneAllocators { get { return ReadPointer<SceneAllocators>(0x130); } }
		public Ptr<Map> x134_Ptr_112Bytes_Items { get { return ReadPointer<Map>(0x134); } }
		public Ptr<X138> x138_Ptr_88Bytes_WorldRelated { get { return ReadPointer<X138>(0x138); } }
		public Ptr<Allocator> x13C_Ptr_Allocator_20x28Bytes_WorldRelated { get { return ReadPointer<Allocator>(0x13C); } }
		public int _x140 { get { return Read<int>(0x140); } }
		public int _x144 { get { return Read<int>(0x144); } }
		public int _x148 { get { return Read<int>(0x148); } }
		public Ptr<FastAttrib> x14C_Ptr_92Bytes_FastAttrib { get { return ReadPointer<FastAttrib>(0x14C); } }
		public Ptr<Teams> x150_Ptr_368Bytes_Teams { get { return ReadPointer<Teams>(0x150); } }
		public int _x154 { get { return Read<int>(0x154); } }
		public Ptr<ActorCommonDataManager> x158_Ptr_232Bytes_ActorCommonDataManager { get { return ReadPointer<ActorCommonDataManager>(0x158); } }
		public Ptr<Allocator> x15C_Ptr_Allocator_1000x64Bytes_WorldRelated { get { return ReadPointer<Allocator>(0x15C); } }
		public Ptr<Allocator> x160_Ptr_Allocator_8x64Bytes_WorldRelated { get { return ReadPointer<Allocator>(0x160); } }
		public Ptr<Allocator> x164_Ptr_Allocator_1024x16Bytes_GameBalance { get { return ReadPointer<Allocator>(0x164); } }
		public Ptr<QuestManager> x168_Ptr_1104Bytes_Quests { get { return ReadPointer<QuestManager>(0x168); } }
		public Ptr<Allocator> x16C_Ptr_Allocator_256x220Bytes_AcdAnimation { get { return ReadPointer<Allocator>(0x16C); } }
		public int _x170 { get { return Read<int>(0x170); } }
		public Ptr<NavCellPath> x174_Ptr_16Bytes_NavCellPath { get { return ReadPointer<NavCellPath>(0x174); } }
		public Ptr<Allocator> x178_Ptr_Allocator_96Bytes_ActorInventory { get { return ReadPointer<Allocator>(0x178); } }
		public Ptr<Allocator> x17C_Ptr_Allocator_16x256Bytes_VisualInventory { get { return ReadPointer<Allocator>(0x17C); } }
		public Ptr<Allocator> x180_Ptr_Allocator_64x12Bytes_Portals { get { return ReadPointer<Allocator>(0x180); } }
		public int x184 { get { return Read<int>(0x184); } }
		public int x188_Ptr_320Bytes { get { return Read<int>(0x188); } }
		public int x18C_Ptr_288Bytes { get { return Read<int>(0x18C); } }
		public int _x190 { get { return Read<int>(0x190); } }
		public Ptr<ActManager> x194_Ptr_56Bytes_Acts { get { return ReadPointer<ActManager>(0x194); } }
		public int x198 { get { return Read<int>(0x198); } }
		public int _x19C { get { return Read<int>(0x19C); } }
		public int x1A0_GameType { get { return Read<int>(0x1A0); } }


		public class X138 : MemoryObject
		{
			public const int SizeOf = 0x58;

			public Allocator x00_Allocator_1000x20Bytes { get { return Read<Allocator>(0x00); } }
			public Allocator x1C_Allocator_1000x16Bytes { get { return Read<Allocator>(0x1C); } }
			public Allocator x38_Allocator_1000x20Bytes { get { return Read<Allocator>(0x38); } }
			public int x54_PathingRollingOctree { get { return Read<int>(0x54); } }
		}
	}

	public partial class Storage
	{
		public static Storage Instance { get { return ObjectManager.Instance.IfNotNull(a => a.x798_Storage); } }

		public int GetGameTick()
		{
			return Instance.x118_GameTick;
		}
	}
}
