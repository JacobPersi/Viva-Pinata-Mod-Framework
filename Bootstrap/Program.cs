using System;
using System.IO;
using System.Json;

namespace PinataParty.Bootstrap {
    class Program {
        static void Main(string[] args) {
            int ProcessId = Win32.CreateSuspended("E:/Games/Viva Pinata/Viva Pinata.exe");

            Console.Write("Please attach your debugger and press enter to continue: ");
            Console.ReadLine();

            JsonObject PatchManifest = JsonObject.Load(File.Open("Patch.json", FileMode.Open)) as JsonObject;
            JsonArray PatchArray = PatchManifest["PatchList"] as JsonArray;
            foreach (JsonObject Patch in PatchArray) {

                Console.WriteLine($"Patching {Patch["Label"]}: {Patch["Description"]}");
                JsonArray TargetArray = (Patch["Targets"]) as JsonArray;
                foreach (JsonObject Target in TargetArray) {
                    IntPtr Address = new IntPtr(Convert.ToInt32(Target["Address"], 16));
                    byte[] TargetHex = Hex.HexStringToByteArray(Target["Target"]);
                    byte[] ReplacementHex = Hex.HexStringToByteArray(Target["Replacement"]);

                    var startPoint = ReplacementHex.Length;
                    Array.Resize(ref ReplacementHex, TargetHex.Length);
                    for (int index = startPoint; index < TargetHex.Length; index++) {
                        ReplacementHex[index] = ReplacementHex[startPoint - 1];
                    }
                    if (!Win32.PatchMemory(ProcessId, Address, ReplacementHex)) {
                        ConsoleEx.ReportFailure();
                    }
                }
            }

            Console.Write("Patching complete! Please press enter to run the game!");
            Console.ReadLine();

            Win32.ResumeProcess(ProcessId);
        }
    }
}
