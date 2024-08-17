namespace PinataParty;

public class PatchCollection
{
	public string Name { get; set; }
	public string Description { get; set; }
	public IPatch[] Patches { get; set; }

	public bool Apply(int processId)
	{
		bool result = true;
		foreach (IPatch patch in Patches)
		{
			result &= patch.Apply(processId);
		}
		return result;
	}
}
