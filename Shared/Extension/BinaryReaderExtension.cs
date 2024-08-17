using System.Text;

namespace PinataParty.Internal;

public static class BinaryReaderExtension
{
	public static string ReadString(this BinaryReader reader, int length, Encoding? encoding = null)
	{
		encoding ??= Encoding.ASCII; 
		byte[] bytes = reader.ReadBytes(length);
		return encoding.GetString(bytes);
	}
}
