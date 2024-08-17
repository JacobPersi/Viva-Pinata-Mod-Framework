using PinataParty.Internal;

Console.WriteLine("Hello, World!");


using var fs = new FileStream(@"D:\Viva Pinata\Vanilla\bundles\english.bnl", FileMode.Open, FileAccess.Read);
using var br = new BinaryReader(fs);

var header = CaffHeader.Read(br);

Console.ReadLine();

enum HeaderByteOrder : byte
{
	Little = 0,
	Big = 1,
};

struct CaffHeader
{
	string Magic;
	string Version;
	uint Chunk1Offset;
	uint Checksum;
	uint Count1;
	uint Count2;
	uint Unknown2;
	uint Unknown3;
	uint Count3;
	uint CountEx;
	uint Unknown4;
	uint Unknown5;
	uint Unknown6;
	uint Unknown7;
	uint Unknown8;
	HeaderByteOrder ByteOrder;
	byte Flag2;
	byte Flag3;
	byte Flag4;
	uint Unknown10;
	uint Chunk1Size;
	uint Unknown12;
	uint Unknown13;
	uint Unknown14;
	uint Chunk1ZSize;
	uint Chunk2Size;
	uint Unknown17;
	uint Unknown18;
	uint Unknown19;
	uint Chunk2ZSize;

	public static CaffHeader Read(BinaryReader br)
	{
		var checksum = CalculateChecksum(br);

		var header = new CaffHeader
		{
			Magic = new string(br.ReadChars(4)),
			Version = new string(br.ReadChars(16)),
			Chunk1Offset = br.ReadUInt32(),
			Checksum = br.ReadUInt32(),
			Count1 = br.ReadUInt32(),
			Count2 = br.ReadUInt32(),
			Unknown2 = br.ReadUInt32(),
			Unknown3 = br.ReadUInt32(),
			Count3 = br.ReadUInt32(),
			CountEx = br.ReadUInt32(),
			Unknown4 = br.ReadUInt32(),
			Unknown5 = br.ReadUInt32(),
			Unknown6 = br.ReadUInt32(),
			Unknown7 = br.ReadUInt32(),
			Unknown8 = br.ReadUInt32(),
			ByteOrder = (HeaderByteOrder)br.ReadUInt32(),
			Flag2 = br.ReadByte(),
			Flag3 = br.ReadByte(),
			Flag4 = br.ReadByte(),
			Unknown10 = br.ReadUInt32(),
			Chunk1Size = br.ReadUInt32(),
			Unknown12 = br.ReadUInt32(),
			Unknown13 = br.ReadUInt32(),
			Unknown14 = br.ReadUInt32(),
			Chunk1ZSize = br.ReadUInt32(),
			Chunk2Size = br.ReadUInt32(),
			Unknown17 = br.ReadUInt32(),
			Unknown18 = br.ReadUInt32(),
			Unknown19 = br.ReadUInt32(),
			Chunk2ZSize = br.ReadUInt32()
		};

		if (header.Checksum != checksum)
			Logger.Warn("Checksum mismatch!");

		return header;

	}

	// Todo: Validate checksum logic to make sure it matches that of game...
	private static uint CalculateChecksum(BinaryReader br)
	{
		// Cache current read position:
		var originalPosition = br.BaseStream.Position;
		// Reset reader to head:
		br.BaseStream.Seek(0, SeekOrigin.Begin);
		// Read the header:
		var headerBytes = br.ReadBytes(120); // Header is 120 bytes. 

		uint checksum = 0;
		for (int index = 0; index < headerBytes.Length; index++)
		{
			// Skip existing checksum:
			if (index >= 24 && index < 28)
				ChecksumByte(ref checksum, 0);
			else 
				ChecksumByte(ref checksum, headerBytes[index]);
		}
		// Reset reader to original:
		br.BaseStream.Seek(originalPosition, SeekOrigin.Begin);

		return checksum;
	}

	private static void ChecksumByte(ref uint checksum, byte currentByte)
	{
		uint val = (checksum << 4) + currentByte;
		uint valMask = val & 0xF0000000;
		if (valMask != 0)
		{
			valMask |= valMask >> 24;
			val ^= valMask;
		}
		checksum = val;
	}
}