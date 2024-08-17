using System;

using PinataParty.Internal;

namespace PinataParty;

public class SimpleMemoryPatch : IPatch
{
	public IntPtr Address;
	public byte[] TargetBytes;
	public byte[] ReplacementBytes;
	public bool IsValid = true;

	public SimpleMemoryPatch(uint address, byte[] target, byte[] replacement)
	{
		if (address == uint.MinValue || target.Length != replacement.Length)
			IsValid = false;

		Address = new IntPtr(address);
		TargetBytes = target;
		ReplacementBytes = replacement;
	}

	public bool Apply(int processId)
	{
		if (IsValid)
			return Win32.PatchMemory(processId, Address, ReplacementBytes);
	
		return false;
	}
}
