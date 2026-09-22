namespace GFX_BLOCK_CATALOGUE
{
    public class TargetDefinition
    {
        public string Key { get; set; }

        public string DisplayName { get; set; }

        public string DeviceModelName { get; set; }

        public string DeviceModelType { get; set; }

        public string PlatformFamily { get; set; }

        public bool IsDefault { get; set; }
    }

    public enum TargetSupportState
    {
        Unknown,
        Supported,
        Unsupported
    }

    public class PlatformSupportDefinition
    {
        public string TargetKey { get; set; }

        public TargetSupportState State { get; set; }

        public string Source { get; set; }

        public string Notes { get; set; }
    }
}