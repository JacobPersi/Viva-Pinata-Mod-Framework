using System;
namespace PinataParty {
    public class Patch {
        protected string Name { get; set; }
        protected string Description { get; set; }
        protected PatchTarget[] Patches { get; set; }

        public bool ApplyPatch(int processId) {
            bool result = true;
            foreach (PatchTarget target in Patches) {
                if (target.IsValid)
                    result &= Win32.PatchMemory(processId, target.Address, target.ReplacementBytes);
                else
                    result = false;
            }
            return result;
        }
    }
}
