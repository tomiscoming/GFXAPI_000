using System;
using System.Collections.Generic;

namespace GFX_BLOCK_CATALOGUE
{
    public class BlockDefinition
    {
        public Type Type { get; set; }

        public string ClrName { get; set; }

        public string FullName { get; set; }

        public string ProgrammaticName { get; set; }

        public string Namespace { get; set; }

        public bool HasPublicParameterlessConstructor { get; set; }

        public Dictionary<string, PropertyDefinition> Properties
        {
            get;
            set;
        }

        public List<PortDefinition> Inputs
        {
            get;
            set;
        }

        public List<PortDefinition> Outputs
        {
            get;
            set;
        }

        public GeometryDefinition Geometry
        {
            get;
            set;
        }

        public Dictionary<string, PlatformSupportDefinition> PlatformSupport
        {
            get;
            set;
        }

        public BlockDefinition()
        {
            Properties =
                new Dictionary<string, PropertyDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            Inputs =
                new List<PortDefinition>();

            Outputs =
                new List<PortDefinition>();

            Geometry =
                new GeometryDefinition();

            PlatformSupport =
                new Dictionary<string, PlatformSupportDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );
        }
    }

    public class PropertyDefinition
    {
        public string Name { get; set; }

        public Type PropertyType { get; set; }

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        public object DefaultValue { get; set; }
    }

    public class PortDefinition
    {
        public string Name { get; set; }

        public string Direction { get; set; }

        public string PortTypeName { get; set; }

        public string FullPortTypeName { get; set; }

        public bool DefaultVisible { get; set; }
    }

    public class GeometryDefinition
    {
        // What the native constructor gives us automatically.

        public int ConstructorWidth { get; set; }

        public int ConstructorHeight { get; set; }

        public bool HasConstructorSize { get; set; }

        // Confirmed EC-gfx palette/editor default dimensions.

        public int DefaultWidth { get; set; }

        public int DefaultHeight { get; set; }

        public bool HasKnownDefaultSize { get; set; }

        // Useful for diagnostics/catalogue export.

        public string Source { get; set; }
    }
}