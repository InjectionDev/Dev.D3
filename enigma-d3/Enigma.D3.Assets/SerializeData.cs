﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enigma.D3.Assets
{
	public struct SerializeData
	{
		public int Offset;
		public int Length;

		public int GetRelativeOffset(SerializeMemoryObject obj)
		{
			return (obj.SerializeBaseAddress + Offset) - obj.Address;
		}
	}
}
