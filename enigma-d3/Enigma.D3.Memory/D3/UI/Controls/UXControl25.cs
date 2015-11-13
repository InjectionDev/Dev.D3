using Enigma.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enigma.D3.UI.Controls
{
	public class UXControl25 : UXItemsControl
	{
		public new const int SizeOf = 0xE30; //3632
		public new const int VTable = 0x01829570;

		public UIReference xA48_UIRef_Label { get { return Read<UIReference>(0xA48); } }
		public UIReference xC50_UIRef_ScrollBar { get { return Read<UIReference>(0xC50); } }
	}
}
