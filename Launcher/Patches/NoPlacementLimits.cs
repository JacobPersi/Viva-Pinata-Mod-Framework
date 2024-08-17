namespace PinataParty;

public class NoPlacementLimits : PatchCollection
{
	public NoPlacementLimits()
	{
		Name = "Disable placement Limits";
		Description = "Disables the item budgets associated with different item types.";
		Patches = new IPatch[] {
			new SimpleMemoryPatch(0x7785D0, target: new byte[] { 0x53, 0x56, 0x8B, 0x74, 0x24, 0x10 },
									   replacement: new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 })
		};
	}
}
