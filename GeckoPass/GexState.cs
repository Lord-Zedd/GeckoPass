using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GeckoPass
{
	public enum PasswordType
	{
		Default,
		PALPSX,
		Saturn,
		PSXProto
	}

	public enum GexWorlds
	{
		Grave,
		Toon,
		Jungle,
		KungFu,
		Rez,
		PlanetX,
		Dome,
		WorldInvalid,
	}

	[Flags]
	public enum UnlockFlags
	{
		WorldMapToon   = 1,
		WorldMapJungle = 2,
		WorldMapKungFu = 4,
		WorldMapRez    = 8,

		BonusCompleteGrave  = 0x10,
		BonusCompleteToon   = 0x20,
		BonusCompleteJungle = 0x40,
		BonusCompleteKungFu = 0x80,
		BonusCompleteRez    = 0x100,
	}

	public enum KnockState
	{
		Locked,
		Complete,
		Unlocked,
		LockedInvalid
	}

	public enum PasswordErrorType
	{
		None,
		InvalidCharacters,
		InvalidLength
	}

	/* Release Password Bits
	 * 0-2   : starting world (integer)
	 * 3-7   : levels complete (integer)
	 * 8-11  : planet x levels complete (integer)
	 * 12    : toon unlocked
	 * 13    : jungle unlocked
	 * 14    : kungfu unlocked
	 * 15    : rez unlocked
	 * 16    : grave bonus complete
	 * 17    : toon bonus complete
	 * 18    : jungle bonus complete
	 * 19    : kungfu bonus complete
	 * 20    : rez bonus complete
	 * 21-22 : knock knock level state (integer)
	 * 23    : unused (that i can tell)
	 */

	/* PSX Proto Password Bits - the password system was still being worked on since 3DO had a save system so theres bugs. bits 8-15 are inverted
	 * 0-2   : starting world (integer)
	 * 3-7   : levels complete (integer) - written by the game as flags apparently as vcr levels?
	 * 8-10  : planet x levels complete (integer) (inverse) - doesnt seem written at all by the game until unlocking rezopolis which then unsets all 3 bits
	 * 11    : toon unlocked (inverse) - due to a game bug this also locks grave from the hub, but gives you the remote to re-enable
	 * 12    : jungle unlocked (inverse)
	 * 13    : kungfu unlocked (inverse)
	 * 14    : rez unlocked (inverse)
	 * 15    : grave bonus (inverse)
	 * 16    : rez bonus
	 * 17    : kungfu bonus
	 * 18    : jungle bonus
	 * 19    : toon bonus
	 * 20    : unknown - gets set alongside any knock knock write but doesnt appear to matter
	 * 21    : unused (that i can tell)
	 * 22-23 : knock knock level state (integer)
	 * 24-31 : unknown/unused?
	 */


	public class GexState
	{
		private static List<char> _key = new List<char>()
		{
			'B',
			'C',
			'D',
			'F',
			'G',
			'H',
			'K',
			'L',
			'P',
			'R',
			'S',
			'T',
			'V',
			'X',
			'Y',
			'Z'
		};

		public GexWorlds StartingWorld { get; set; }
		public int LevelsComplete { get; set; }
		public int XLevelsComplete { get; set; }
		public UnlockFlags Unlockables { get; set; }
		public KnockState KnockKnockState { get; set; }
		public bool UnusedBit { get; set; }
		public bool ProtoBit { get; set; }
		public int ProtoUnk { get; set; }


		public bool LoadPassword(string password, PasswordType type)
		{
			if (type == PasswordType.PSXProto)
				return LoadProtoPassword(password, type);
			else
				return LoadNormalPassword(password, type);
		}

		public string WritePassword(PasswordType type)
		{
			if (type == PasswordType.PSXProto)
				return WriteProtoPassword(type);
			else
				return WriteNormalPassword(type);
		}

		public static PasswordErrorType PasswordSanityCheck(string password, PasswordType type)
		{
			int count = GetPasswordContentLength(type) + 2;

			if (password.Length != count)
				return PasswordErrorType.InvalidLength;
			for (int i = 0; i < count; i++)
			{
				if (!_key.Contains(password[i]))
					return PasswordErrorType.InvalidCharacters;
			}
			return PasswordErrorType.None;
		}

		public static int GetPasswordContentLength(PasswordType type)
		{
			//not including checksum (add 2)
			if (type == PasswordType.PSXProto)
				return 8;
			else
				return 6;
		}


		private static int GetTypeMagic(PasswordType type)
		{
			//Some ports use a nonzero starting number to offset the checksum, I assume to stop you from sharing passwords?
			switch (type)
			{
				default:
				case PasswordType.Default:
				case PasswordType.PSXProto:
					return 0;
				case PasswordType.PALPSX:
					return 0xB;
				case PasswordType.Saturn:
					return 0x11;
			}	
		}

		private bool LoadNormalPassword(string password, PasswordType type)
		{
			string nochecksum = password.Substring(2).ToUpper();

			bool validChecksum = password == GenerateChecksum(nochecksum, type);

			byte[] decoded = new byte[6];
			for (int i = 0; i < 6; i++)
				decoded[i] = (byte)_key.IndexOf(nochecksum[i]);

			decoded = FlipState(decoded);

			decoded = XOR_Constants(decoded);

			int state =
				((byte)((decoded[0] << 4) + decoded[1]) << 16) |
				((byte)((decoded[2] << 4) + decoded[3]) << 8) |
				((byte)((decoded[4] << 4) + decoded[5]));

			StartingWorld = (GexWorlds)((state & 0xE00000) >> 21);

			LevelsComplete = (state & 0x1F0000) >> 16;

			XLevelsComplete = (state & 0xF000) >> 12;

			Unlockables = 0;
			if ((state & 0x800) > 0)
				Unlockables |= UnlockFlags.WorldMapToon;
			if ((state & 0x400) > 0)
				Unlockables |= UnlockFlags.WorldMapJungle;
			if ((state & 0x200) > 0)
				Unlockables |= UnlockFlags.WorldMapKungFu;
			if ((state & 0x100) > 0)
				Unlockables |= UnlockFlags.WorldMapRez;

			if ((state & 0x80) > 0)
				Unlockables |= UnlockFlags.BonusCompleteGrave;
			if ((state & 0x40) > 0)
				Unlockables |= UnlockFlags.BonusCompleteToon;
			if ((state & 0x20) > 0)
				Unlockables |= UnlockFlags.BonusCompleteJungle;
			if ((state & 0x10) > 0)
				Unlockables |= UnlockFlags.BonusCompleteKungFu;
			if ((state & 0x8) > 0)
				Unlockables |= UnlockFlags.BonusCompleteRez;

			KnockKnockState = (KnockState)((state & 6) >> 1);

			UnusedBit = (state & 1) > 0;

			return validChecksum;
		}

		private bool LoadProtoPassword(string password, PasswordType type)
		{
			string nochecksum = password.Substring(2).ToUpper();

			bool validChecksum = password == GenerateChecksum(nochecksum, type);

			byte[] decoded = new byte[8];
			for (int i = 0; i < 8; i++)
				decoded[i] = (byte)_key.IndexOf(nochecksum[i]);

			decoded = FlipStateProto(decoded);

			decoded = XOR_ConstantsProto(decoded);

			uint state =
				(uint)((byte)((decoded[0] << 4) + decoded[1]) << 24) |
				(uint)((byte)((decoded[2] << 4) + decoded[3]) << 16) |
				(uint)((byte)((decoded[4] << 4) + decoded[5]) << 8) |
				(uint)(byte)((decoded[6] << 4) + decoded[7]);

			StartingWorld = (GexWorlds)((state & 0xE0000000) >> 29);

			LevelsComplete = (int)((state & 0x1F000000) >> 24);

			XLevelsComplete = (int)((~state & 0xE00000) >> 21);

			Unlockables = 0;
			if ((~state & 0x100000) > 0)
				Unlockables |= UnlockFlags.WorldMapToon;
			if ((~state & 0x80000) > 0)
				Unlockables |= UnlockFlags.WorldMapJungle;
			if ((~state & 0x40000) > 0)
				Unlockables |= UnlockFlags.WorldMapKungFu;
			if ((~state & 0x20000) > 0)
				Unlockables |= UnlockFlags.WorldMapRez;

			if ((~state & 0x10000) > 0)
				Unlockables |= UnlockFlags.BonusCompleteGrave;
			if ((state & 0x1000) > 0)
				Unlockables |= UnlockFlags.BonusCompleteToon;
			if ((state & 0x2000) > 0)
				Unlockables |= UnlockFlags.BonusCompleteJungle;
			if ((state & 0x4000) > 0)
				Unlockables |= UnlockFlags.BonusCompleteKungFu;
			if ((state & 0x8000) > 0)
				Unlockables |= UnlockFlags.BonusCompleteRez;

			ProtoBit = (state & 0x800) > 0;
			UnusedBit = (state & 0x400) > 0;

			if ((state & 0x100) > 0)
			{
				if ((state & 0x200) > 0)
					KnockKnockState = KnockState.Complete;
				else
					KnockKnockState = KnockState.Unlocked;
			}

			ProtoUnk = (int)(state & 0xFF);

			return validChecksum;
		}

		private string WriteNormalPassword(PasswordType type)
		{
			int state = 0;

			int cappedWorld = Math.Min((int)StartingWorld, 7);
			int cappedLevels = Math.Min(LevelsComplete, 0x1F);
			int cappedXLevels = Math.Min(XLevelsComplete, 0xF);

			state |= cappedWorld << 21;
			state |= cappedLevels << 16;
			state |= cappedXLevels << 12;

			if (Unlockables.HasFlag(UnlockFlags.WorldMapToon))
				state |= 0x800;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapJungle))
				state |= 0x400;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapKungFu))
				state |= 0x200;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapRez))
				state |= 0x100;

			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteGrave))
				state |= 0x80;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteToon))
				state |= 0x40;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteJungle))
				state |= 0x20;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteKungFu))
				state |= 0x10;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteRez))
				state |= 0x8;

			state |= (int)KnockKnockState << 1;

			if (UnusedBit)
				state |= 1;

			byte[] bytes = BitConverter.GetBytes(state);

			byte[] simplified = new byte[6];
			simplified[0] = (byte)((bytes[2] & 0xF0) >> 4);
			simplified[1] = (byte)(bytes[2] & 0xF);
			simplified[2] = (byte)((bytes[1] & 0xF0) >> 4);
			simplified[3] = (byte)(bytes[1] & 0xF);
			simplified[4] = (byte)((bytes[0] & 0xF0) >> 4);
			simplified[5] = (byte)(bytes[0] & 0xF);

			simplified = XOR_Constants(simplified);

			simplified = FlipState(simplified);

			string result = "";

			result += _key[simplified[0]];
			result += _key[simplified[1]];
			result += _key[simplified[2]];
			result += _key[simplified[3]];
			result += _key[simplified[4]];
			result += _key[simplified[5]];

			result = GenerateChecksum(result, type);

			return result;
		}

		private string WriteProtoPassword(PasswordType type)
		{
			int state = 0x00FF0000;

			int cappedWorld = Math.Min((int)StartingWorld, 7);
			int cappedLevels = Math.Min(LevelsComplete, 0x1F);
			int cappedXLevels = Math.Min(XLevelsComplete, 7);
			int cappedProto = Math.Min(ProtoUnk, 0xFF);

			state |= (cappedWorld & 7) << 29;
			state |= (cappedLevels & 0x1F) << 24;
			state &= ~((cappedXLevels & 7) << 21);

			if (Unlockables.HasFlag(UnlockFlags.WorldMapToon))
				state &= ~0x100000;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapJungle))
				state &= ~0x80000;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapKungFu))
				state &= ~0x40000;
			if (Unlockables.HasFlag(UnlockFlags.WorldMapRez))
				state &= ~0x20000;

			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteGrave))
				state &= ~0x10000;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteToon))
				state |= 0x1000;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteJungle))
				state |= 0x2000;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteKungFu))
				state |= 0x4000;
			if (Unlockables.HasFlag(UnlockFlags.BonusCompleteRez))
				state |= 0x8000;

			if (ProtoBit)
				state |= 0x800;

			if (UnusedBit)
				state |= 0x400;

			if (KnockKnockState == KnockState.Unlocked)
				state |= 0x100;
			else if (KnockKnockState == KnockState.Complete)
				state |= 0x300;

			state |= cappedProto;

			byte[] bytes = BitConverter.GetBytes(state);

			byte[] simplified = new byte[8];
			simplified[0] = (byte)((bytes[3] & 0xF0) >> 4);
			simplified[1] = (byte)(bytes[3] & 0xF);
			simplified[2] = (byte)((bytes[2] & 0xF0) >> 4);
			simplified[3] = (byte)(bytes[2] & 0xF);
			simplified[4] = (byte)((bytes[1] & 0xF0) >> 4);
			simplified[5] = (byte)(bytes[1] & 0xF);
			simplified[6] = (byte)((bytes[0] & 0xF0) >> 4);
			simplified[7] = (byte)(bytes[0] & 0xF);

			simplified = XOR_ConstantsProto(simplified);

			simplified = FlipStateProto(simplified);

			string result = "";

			result += _key[simplified[0]];
			result += _key[simplified[1]];
			result += _key[simplified[2]];
			result += _key[simplified[3]];
			result += _key[simplified[4]];
			result += _key[simplified[5]];
			result += _key[simplified[6]];
			result += _key[simplified[7]];

			result = GenerateChecksum(result, type);

			return result;
		}

		private static string GenerateChecksum(string password, PasswordType type)
		{
			int sum = GetTypeMagic(type);
			int count = GetPasswordContentLength(type);

			for (int i = 0; i < count; i++)
				sum += password[i];

			int firstInd = sum & 0xF;
			int secondInd = (sum & 0xF0) >> 4;

			string newPass = "";
			newPass += _key[firstInd];
			newPass += _key[secondInd];

			return newPass += password;
		}

		private static byte[] XOR_Constants(byte[] input)
		{
			input[0] ^= 0x3;
			input[1] ^= 0xC;
			input[2] ^= 0x5;
			input[3] ^= 0x6;
			input[4] ^= 0x8;
			input[5] ^= 0x4;
			return input;
		}

		private static byte[] XOR_ConstantsProto(byte[] input)
		{
			input[0] ^= 0x5;
			input[1] ^= 0x6;
			input[2] ^= 0xC;
			input[3] ^= 0x3;
			input[4] ^= 0xC;
			input[5] ^= 0x7;
			input[6] ^= 0x8;
			input[7] ^= 0x4;
			return input;
		}

		private static byte[] FlipState(byte[] input)
		{
			Array.Reverse(input, 0, 2);
			Array.Reverse(input, 2, 2);
			Array.Reverse(input, 4, 2);
			return input;
		}

		private static byte[] FlipStateProto(byte[] input)
		{
			Array.Reverse(input, 0, 2);
			Array.Reverse(input, 2, 2);
			Array.Reverse(input, 4, 2);
			Array.Reverse(input, 6, 2);
			return input;
		}

	}
}
