namespace PinataParty {
    public class WindowedMode : PatchCollection  {
        public WindowedMode() {
            Name = "Force Windowed Mode";
            Description = "Forces the game into windowed mode by altering DirectX's PresentationParams and the WindowsClass definition.";
            Patches = new IPatch[] {
                new SimpleMemoryPatch(0x7D348D,   new byte[] {0xC7, 0x46, 0x20, 0x00, 0x00, 0x00, 0x00},
                                            new byte[] {0xC7, 0x46, 0x20, 0x01, 0x00, 0x00, 0x00}),
                new SimpleMemoryPatch(0x8C1700,   new byte[] {0x68, 0x00, 0x00, 0x00, 0x80},
                                            new byte[] {0x68, 0x00, 0x00, 0x8E, 0x00})
            };
        }
    }
}

