using PinataParty.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PinataParty;

public class Program
{
	private static bool IsDebuggerAttached = Debugger.IsAttached;

	public static void Main(string[] args)
	{
		int processId = Win32.CreateSuspended(@"D:\Viva Pinata\Modded\Viva Pinata.exe");

		if (IsDebuggerAttached)
		{
			Console.WriteLine("Please attach your debugger and press enter to continue: ");
			Console.ReadLine();
		}

		List<Type> patchTypes = GetAllSubclassesOf(typeof(PatchCollection));
		foreach (Type patchType in patchTypes)
		{
			var patchCollection = (PatchCollection)Activator.CreateInstance(patchType);

			Console.WriteLine($"\tApplying {patchCollection.Name}: {patchCollection.Description}");

			patchCollection.Apply(processId);
		}
		Console.WriteLine("\nStarting...");
		Win32.ResumeProcess(processId);
	}

	public static List<Type> GetAllSubclassesOf(Type baseType)
	{
		return Assembly.GetAssembly(baseType)
			.GetTypes()
			.Where(type => type.IsSubclassOf(baseType))
			.ToList();
	}
}
