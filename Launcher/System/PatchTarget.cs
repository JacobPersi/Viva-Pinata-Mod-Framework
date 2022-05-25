using System;

namespace PinataParty {
    public class PatchTarget {
        public IntPtr Address;
        public byte[] TargetBytes;
        public byte[] ReplacementBytes;
        public bool IsValid = true;

        public PatchTarget(uint address, byte[] target, byte[] replacement) {
            if (address == uint.MinValue || target.Length != replacement.Length)
                IsValid = false;
            Address = new IntPtr(address);
            TargetBytes = target;
            ReplacementBytes = replacement;
        }
    }
}
