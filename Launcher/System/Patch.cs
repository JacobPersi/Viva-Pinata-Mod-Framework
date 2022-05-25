namespace PinataParty {
    public class Patch {
        protected string Name { get; set; }
        protected string Description { get; set; }
        protected SimplePatch[] Patches { get; set; }

        public bool ApplyPatch(int processId) {
            bool result = true;
            foreach (SimplePatch patch in Patches) {
                result &= patch.ApplyPatch(processId);
            }
            return result;
        }
    }
}
