﻿using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Belogic
{
	[CoreAttributes("uzem", "David Etherton", true, false, "", "", false)]
	public class Uzem : WaterboxCore
	{
		private LibUzem _uze;
		private bool _mouseEnabled;

		[CoreConstructor("UZE")]
		public Uzem(CoreComm comm, byte[] rom)
			:base(comm, new Configuration
			{
				DefaultWidth = 720,
				DefaultHeight = 224,
				MaxWidth = 720,
				MaxHeight = 224,
				MaxSamples = 4096,
				SystemId = "UZE",
				DefaultFpsNumerator = 28618182,
				DefaultFpsDenominator = 476840
			})
		{
			_uze = PreInit<LibUzem>(new PeRunnerOptions
			{
				Filename = "uzem.wbx",
				SbrkHeapSizeKB = 4,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				MmapHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
			});

			_exe.AddReadonlyFile(rom, "romfile");
			if (!_uze.Init())
				throw new InvalidOperationException("Core rejected the rom!");
			_mouseEnabled = _uze.MouseEnabled();
			_exe.RemoveReadonlyFile("romfile");

			PostInit();
		}

		private static readonly ControllerDefinition TwoPads = new ControllerDefinition
		{
			Name = "SNES Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Left", "P1 Right", "P1 Down", "P1 Select", "P1 Start", "P1 X", "P1 A", "P1 B", "P1 Y", "P1 R", "P1 L",
				"P2 Up", "P2 Left", "P2 Right", "P2 Down", "P2 Select", "P2 Start", "P2 X", "P2 A", "P2 B", "P2 Y", "P2 R", "P2 L",
				"Power"
			}
		};

		private static readonly ControllerDefinition Mouse = new ControllerDefinition
		{
			Name = "SNES Controller",
			BoolButtons =
			{
				"P1 Mouse Left", "P1 Mouse Right", "Power"
			},
			FloatControls =
			{
				"P1 Mouse X", "P1 Mouse Y"
			},
			FloatRanges =
			{
				new[] { -127f, 0f, 127f },
				new[] { -127f, 0f, 127f }
			}
		};

		private static readonly string[] PadBits =
		{
			"B", "Y", "Select", "Start", "Up", "Down", "Left", "Right", "A", "X", "L", "R"
		};

		private static int EncodePad(IController c, int p)
		{
			int ret = 0;
			int val = 1;
			int idx = 0;
			foreach (var s in PadBits)
			{
				if (c.IsPressed("P" + p + " " + PadBits[idx++]))
					ret |= val;
				val <<= 1;
			}
			return ret;
		}

		private static int EncodeDelta(float value)
		{
			int v = (int)value;
			if (v > 127)
				v = 127;
			if (v < -127)
				v = -127;

			int ret = 0;
			if (v < 0)
			{
				ret |= 1;
				v = -v;
			}

			int mask = 64;
			int bit = 2;
			while (mask != 0)
			{
				if ((v & mask) != 0)
					ret |= bit;
				mask >>= 1;
				bit <<= 1;
			}
			return ret;
		}

		public override ControllerDefinition ControllerDefinition => _mouseEnabled ? Mouse : TwoPads;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var ret = new LibUzem.FrameInfo();
			if (_mouseEnabled)
			{
				ret.ButtonsP1 = EncodeDelta(controller.GetFloat("P1 X"))
					| EncodeDelta(controller.GetFloat("P1 Y"))
					| 0x8000;
				if (controller.IsPressed("P1 Mouse Left"))
					ret.ButtonsP1 |= 0x200;
				if (controller.IsPressed("P1 Mouse Right"))
					ret.ButtonsP1 |= 0x100;
			}
			else
			{
				ret.ButtonsP1 = EncodePad(controller, 1);
				ret.ButtonsP2 = EncodePad(controller, 2);
			}
			if (controller.IsPressed("Power"))
				ret.ButtonsConsole = 1;

			return ret;
		}

		public override int VirtualHeight => 448; // TODO: It's not quite this, NTSC and such
	}
}
