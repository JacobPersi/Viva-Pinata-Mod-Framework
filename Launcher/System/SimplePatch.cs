using System;

namespace PinataParty {
    public class SimplePatch {
        public IntPtr Address;
        public byte[] TargetBytes;
        public byte[] ReplacementBytes;
        public bool IsValid = true;

        public SimplePatch(uint address, byte[] target, byte[] replacement) {
            if (address == uint.MinValue || target.Length != replacement.Length) { 
                IsValid = false;
            }
            Address = new IntPtr(address);
            TargetBytes = target;
            ReplacementBytes = replacement;
        }

        public bool ApplyPatch(int processId) {
            if (IsValid) {
                return Win32.PatchMemory(processId, Address, ReplacementBytes);
            }
            else {
                return false;
            }
        }
    }
}
