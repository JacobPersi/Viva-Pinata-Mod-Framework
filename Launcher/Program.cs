using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinataParty {
    public class Program {
        public static void Main(string[] args) {
            int ProcessId = Win32.CreateSuspended("E:/Games/Viva Pinata/Viva Pinata.exe");

            Console.Write("Please attach your debugger and press enter to continue: ");
            Console.ReadLine();

            List<Type> patchTypes = GetAllSubclassesOf(typeof(SimplePatch));
            foreach (Type patchType in patchTypes) {
                ((SimplePatch)Activator.CreateInstance(patchType)).ApplyPatch(ProcessId);
            }

            Console.Write("Patching complete! Please press enter to run the game!");
            Console.ReadLine();

            Win32.ResumeProcess(ProcessId);
        }

        public static List<Type> GetAllSubclassesOf(Type baseType) {
            return Assembly.GetAssembly(baseType).GetTypes().Where(type => type.IsSubclassOf(baseType)).ToList();
        }
    }
}
