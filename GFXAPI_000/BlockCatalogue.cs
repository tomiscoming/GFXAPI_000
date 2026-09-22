using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

using Distech.Gpl.Model.Shapes.Blocks;

namespace GFX_BLOCK_CATALOGUE
{
    public class BlockCatalogue
    {
        private readonly List<BlockDefinition> _blocks;

        private readonly Dictionary<string, BlockDefinition>
            _fullNameIndex;

        private readonly Dictionary<string, BlockDefinition>
            _shortNameIndex;

        private readonly Dictionary<string, BlockDefinition>
            _programmaticNameIndex;

        private readonly HashSet<string>
            _ambiguousShortNames;

        private readonly HashSet<string>
            _ambiguousProgrammaticNames;

        public IEnumerable<BlockDefinition> Blocks
        {
            get
            {
                return _blocks;
            }
        }

        // ============================================================
        // Constructor
        // ============================================================

        public BlockCatalogue(
            IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(
                    "assemblies"
                );
            }

            _blocks =
                new List<BlockDefinition>();

            _fullNameIndex =
                new Dictionary<string, BlockDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            _shortNameIndex =
                new Dictionary<string, BlockDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            _programmaticNameIndex =
                new Dictionary<string, BlockDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            _ambiguousShortNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            _ambiguousProgrammaticNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            Discover(
                assemblies
            );

            BuildIndexes();

            LoadDefaultTargetSupport();
        }

        // ============================================================
        // Resolution
        // ============================================================

        public BlockDefinition Resolve(
            string name)
        {
            if (
                string.IsNullOrWhiteSpace(
                    name
                ))
            {
                return null;
            }

            BlockDefinition definition;

            // --------------------------------------------------------
            // Full CLR name
            // --------------------------------------------------------

            if (
                _fullNameIndex.TryGetValue(
                    name,
                    out definition
                ))
            {
                return definition;
            }

            // --------------------------------------------------------
            // Programmatic name
            // --------------------------------------------------------

            if (
                !_ambiguousProgrammaticNames.Contains(
                    name
                ) &&
                _programmaticNameIndex.TryGetValue(
                    name,
                    out definition
                ))
            {
                return definition;
            }

            // --------------------------------------------------------
            // Short CLR name
            // --------------------------------------------------------

            if (
                !_ambiguousShortNames.Contains(
                    name
                ) &&
                _shortNameIndex.TryGetValue(
                    name,
                    out definition
                ))
            {
                return definition;
            }

            return null;
        }

        public Type ResolveType(
            string name)
        {
            BlockDefinition definition =
                Resolve(
                    name
                );

            if (definition == null)
            {
                return null;
            }

            return definition.Type;
        }

        // ============================================================
        // Discovery
        // ============================================================

        private void Discover(
            IEnumerable<Assembly> assemblies)
        {
            foreach (
                Assembly assembly
                in assemblies)
            {
                foreach (
                    Type type
                    in SafeGetTypes(
                        assembly
                    ))
                {
                    if (
                        !IsUsableBlockType(
                            type
                        ))
                    {
                        continue;
                    }

                    BlockDefinition definition =
                        CreateDefinition(
                            type
                        );

                    _blocks.Add(
                        definition
                    );
                }
            }

            _blocks.Sort(
                delegate (
                    BlockDefinition a,
                    BlockDefinition b)
                {
                    return string.Compare(
                        a.ProgrammaticName ??
                        a.ClrName,

                        b.ProgrammaticName ??
                        b.ClrName,

                        StringComparison.OrdinalIgnoreCase
                    );
                }
            );
        }

        private bool IsUsableBlockType(
            Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsAbstract)
            {
                return false;
            }

            if (
                !typeof(Block)
                .IsAssignableFrom(
                    type
                ))
            {
                return false;
            }

            if (
                string.IsNullOrEmpty(
                    type.Namespace
                ))
            {
                return false;
            }

            if (
                !type.Namespace.StartsWith(
                    "Distech.Gpl.Model.Shapes.Blocks",
                    StringComparison.Ordinal
                ))
            {
                return false;
            }

            return true;
        }

        private BlockDefinition CreateDefinition(
            Type type)
        {
            BlockDefinition definition =
                new BlockDefinition();

            definition.Type =
                type;

            definition.ClrName =
                type.Name;

            definition.FullName =
                type.FullName;

            definition.Namespace =
                type.Namespace;

            ConstructorInfo constructor =
                type.GetConstructor(
                    Type.EmptyTypes
                );

            definition
                .HasPublicParameterlessConstructor =
                constructor != null;

            Block instance =
                null;

            if (constructor != null)
            {
                try
                {
                    instance =
                        Activator.CreateInstance(
                            type
                        ) as Block;
                }
                catch
                {
                    instance =
                        null;
                }
            }

            if (instance != null)
            {
                try
                {
                    definition.ProgrammaticName =
                        instance.ProgrammaticName;
                }
                catch
                {
                    definition.ProgrammaticName =
                        null;
                }

                CaptureConstructorGeometry(
                    definition,
                    instance
                );

                CapturePorts(
                    definition,
                    instance
                );
            }


            // --------------------------------------------------------
            // Capture reflected public properties/defaults
            // --------------------------------------------------------

            CaptureProperties(
                definition,
                instance
            );

            // --------------------------------------------------------
            // Apply known native geometry
            // --------------------------------------------------------

            ApplyGeometryOverrides(
                definition
            );

            return definition;
        }

        // ============================================================
        // Constructor geometry discovery
        // ============================================================

        private void CaptureConstructorGeometry(
            BlockDefinition definition,
            Block instance)
        {
            if (
                definition == null ||
                instance == null)
            {
                return;
            }

            try
            {
                int width =
                    instance.Bounds.Width;

                int height =
                    instance.Bounds.Height;

                if (
                    width > 0 &&
                    height > 0)
                {
                    definition.Geometry.ConstructorWidth =
                        width;

                    definition.Geometry.ConstructorHeight =
                        height;

                    definition.Geometry.HasConstructorSize =
                        true;
                }
            }
            catch
            {
                // Some weird block may object to Bounds access.
                // Catalogue discovery should survive it.
            }
        }

        // ============================================================
        // Port discovery
        // ============================================================

        private void CapturePorts(
            BlockDefinition definition,
            Block instance)
        {
            if (
                definition == null ||
                instance == null)
            {
                return;
            }

            // --------------------------------------------------------
            // Inputs
            // --------------------------------------------------------

            foreach (
                IPort port
                in instance.InputPorts)
            {
                if (port == null)
                {
                    continue;
                }

                PortDefinition portDefinition =
                    new PortDefinition();

                portDefinition.Name =
                    port.Name;

                portDefinition.Direction =
                    "Input";

                portDefinition.PortTypeName =
                    port.GetType().Name;

                portDefinition.FullPortTypeName =
                    port.GetType().FullName;

                portDefinition.DefaultVisible =
                    port.Visible;

                definition.Inputs.Add(
                    portDefinition
                );
            }

            // --------------------------------------------------------
            // Outputs
            // --------------------------------------------------------

            foreach (
                IPort port
                in instance.OutputPorts)
            {
                if (port == null)
                {
                    continue;
                }

                PortDefinition portDefinition =
                    new PortDefinition();

                portDefinition.Name =
                    port.Name;

                portDefinition.Direction =
                    "Output";

                portDefinition.PortTypeName =
                    port.GetType().Name;

                portDefinition.FullPortTypeName =
                    port.GetType().FullName;

                portDefinition.DefaultVisible =
                    port.Visible;

                definition.Outputs.Add(
                    portDefinition
                );
            }
        }

        // ============================================================
        // Property discovery
        // ============================================================

        private void CaptureProperties(
            BlockDefinition definition,
            Block instance)
        {
            PropertyInfo[] properties =
                definition.Type
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

            foreach (
                PropertyInfo property
                in properties
                .OrderBy(
                    x => x.Name
                ))
            {
                PropertyDefinition propertyDefinition =
                    new PropertyDefinition();

                propertyDefinition.Name =
                    property.Name;

                propertyDefinition.PropertyType =
                    property.PropertyType;

                propertyDefinition.CanRead =
                    property.CanRead;

                propertyDefinition.CanWrite =
                    property.CanWrite;

                if (
                    instance != null &&
                    property.CanRead &&
                    property
                        .GetIndexParameters()
                        .Length == 0)
                {
                    try
                    {
                        propertyDefinition.DefaultValue =
                            property.GetValue(
                                instance,
                                null
                            );
                    }
                    catch
                    {
                        propertyDefinition.DefaultValue =
                            null;
                    }
                }

                definition.Properties[
                    property.Name
                ] =
                    propertyDefinition;
            }
        }

        // ============================================================
        // Geometry overrides
        // ============================================================

        private void ApplyGeometryOverrides(
            BlockDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            // --------------------------------------------------------
            // Internal Constant
            // --------------------------------------------------------
            //
            // Confirmed from native EC-gfx XML:
            // width  = 96
            // height = 12
            // --------------------------------------------------------

            if (
                string.Equals(
                    definition.ClrName,
                    "InternalConstantNumeric",
                    StringComparison.Ordinal
                ))
            {
                definition.Geometry.DefaultWidth =
                    96;

                definition.Geometry.DefaultHeight =
                    12;

                definition.Geometry.HasKnownDefaultSize =
                    true;

                definition.Geometry.Source =
                    "Confirmed EC-gfx XML";

                return;
            }
        }

        // ============================================================
        // Index creation
        // ============================================================

        private void BuildIndexes()
        {
            foreach (
                BlockDefinition definition
                in _blocks)
            {
                // ----------------------------------------------------
                // Full CLR names
                // ----------------------------------------------------

                if (
                    !string.IsNullOrWhiteSpace(
                        definition.FullName
                    ))
                {
                    _fullNameIndex[
                        definition.FullName
                    ] =
                        definition;
                }

                // ----------------------------------------------------
                // Short CLR names
                // ----------------------------------------------------

                AddUniqueAlias(
                    definition.ClrName,
                    definition,
                    _shortNameIndex,
                    _ambiguousShortNames
                );

                // ----------------------------------------------------
                // Programmatic names
                // ----------------------------------------------------

                AddUniqueAlias(
                    definition.ProgrammaticName,
                    definition,
                    _programmaticNameIndex,
                    _ambiguousProgrammaticNames
                );
            }
        }

        // ============================================================
        // Target support loading
        // ============================================================

        private void LoadDefaultTargetSupport()
        {
            // --------------------------------------------------------
            // First preference:
            // TargetSupport\S1000.json beside the executable.
            //
            // This is where we eventually want generated support
            // catalogues to live permanently.
            // --------------------------------------------------------

            string localPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "TargetSupport",
                    "S1000.json"
                );

            if (File.Exists(localPath))
            {
                LoadTargetSupportCatalogue(
                    localPath
                );

                return;
            }

            // --------------------------------------------------------
            // Development fallback.
            //
            // S1000SupportProbe currently writes here.
            // --------------------------------------------------------

            string probePath =
                @"C:\Temp\S1000.json";

            if (File.Exists(probePath))
            {
                LoadTargetSupportCatalogue(
                    probePath
                );
            }
        }

        public void LoadTargetSupportCatalogue(
            string fileName)
        {
            if (
                string.IsNullOrWhiteSpace(
                    fileName
                ))
            {
                throw new ArgumentException(
                    "Target support catalogue file name is required.",
                    "fileName"
                );
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(
                    "Target support catalogue not found.",
                    fileName
                );
            }

            string json =
                File.ReadAllText(
                    fileName
                );

            if (
                string.IsNullOrWhiteSpace(
                    json
                ))
            {
                throw new InvalidOperationException(
                    "Target support catalogue is empty: " +
                    fileName
                );
            }

            JavaScriptSerializer serializer =
                new JavaScriptSerializer();

            serializer.MaxJsonLength =
                int.MaxValue;

            TargetSupportCatalogueFile catalogue =
                serializer.Deserialize<
                    TargetSupportCatalogueFile
                >(
                    json
                );

            if (catalogue == null)
            {
                throw new InvalidOperationException(
                    "Could not deserialize target support catalogue: " +
                    fileName
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    catalogue.Target
                ))
            {
                throw new InvalidOperationException(
                    "Target support catalogue has no Target value: " +
                    fileName
                );
            }

            if (catalogue.Blocks == null)
            {
                return;
            }

            foreach (
                TargetBlockSupportEntry entry
                in catalogue.Blocks)
            {
                ApplyTargetSupportEntry(
                    catalogue,
                    entry
                );
            }
        }

        private void ApplyTargetSupportEntry(
            TargetSupportCatalogueFile catalogue,
            TargetBlockSupportEntry entry)
        {
            if (
                catalogue == null ||
                entry == null ||
                string.IsNullOrWhiteSpace(
                    entry.FullName
                ))
            {
                return;
            }

            // --------------------------------------------------------
            // IMPORTANT:
            // Only resolve support information by exact CLR FullName.
            //
            // Do not use ProgrammaticName here because multiple
            // Distech block families reuse the same friendly names.
            // --------------------------------------------------------

            BlockDefinition definition;

            if (
                !_fullNameIndex.TryGetValue(
                    entry.FullName,
                    out definition
                ))
            {
                // ----------------------------------------------------
                // JSON may contain a block from a different DLL version.
                // That is not fatal. The local catalogue simply does
                // not know about that block.
                // ----------------------------------------------------

                return;
            }

            TargetSupportState state =
                ParseTargetSupportState(
                    entry.State
                );

            PlatformSupportDefinition support =
                new PlatformSupportDefinition();

            support.TargetKey =
                catalogue.Target;

            support.State =
                state;

            support.Source =
                !string.IsNullOrWhiteSpace(
                    entry.Source
                )
                    ? entry.Source
                    : catalogue.Source;

            support.Notes =
                entry.Notes;

            definition.PlatformSupport[
                catalogue.Target
            ] =
                support;
        }

        private TargetSupportState ParseTargetSupportState(
            string value)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                ))
            {
                return
                    TargetSupportState.Unknown;
            }

            TargetSupportState state;

            if (
                Enum.TryParse(
                    value,
                    true,
                    out state
                ))
            {
                return state;
            }

            return
                TargetSupportState.Unknown;
        }

        private void AddUniqueAlias(
            string alias,
            BlockDefinition definition,
            Dictionary<string, BlockDefinition> index,
            HashSet<string> ambiguous)
        {
            if (
                string.IsNullOrWhiteSpace(
                    alias
                ))
            {
                return;
            }

            if (
                ambiguous.Contains(
                    alias
                ))
            {
                return;
            }

            BlockDefinition existing;

            if (
                index.TryGetValue(
                    alias,
                    out existing
                ))
            {
                index.Remove(
                    alias
                );

                ambiguous.Add(
                    alias
                );

                return;
            }

            index.Add(
                alias,
                definition
            );
        }

        public void ExportGeometryReport(
    string fileName)
        {
            if (
                string.IsNullOrWhiteSpace(
                    fileName
                ))
            {
                throw new ArgumentException(
                    "File name is required.",
                    "fileName"
                );
            }

            List<string> lines =
                new List<string>();

            lines.Add(
                "ProgrammaticName," +
                "ClrName," +
                "FullName," +
                "ConstructorWidth," +
                "ConstructorHeight," +
                "HasConstructorSize," +
                "DefaultWidth," +
                "DefaultHeight," +
                "HasKnownDefaultSize," +
                "Source"
            );

            foreach (
                BlockDefinition definition
                in _blocks)
            {
                GeometryDefinition geometry =
                    definition.Geometry;

                lines.Add(
                    Csv(
                        definition.ProgrammaticName
                    ) + "," +
                    Csv(
                        definition.ClrName
                    ) + "," +
                    Csv(
                        definition.FullName
                    ) + "," +
                    geometry.ConstructorWidth + "," +
                    geometry.ConstructorHeight + "," +
                    geometry.HasConstructorSize + "," +
                    geometry.DefaultWidth + "," +
                    geometry.DefaultHeight + "," +
                    geometry.HasKnownDefaultSize + "," +
                    Csv(
                        geometry.Source
                    )
                );
            }

            System.IO.File.WriteAllLines(
                fileName,
                lines
            );
        }

        private string Csv(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            string escaped =
                value.Replace(
                    "\"",
                    "\"\""
                );

            return
                "\"" +
                escaped +
                "\"";
        }

        // ============================================================
        // Geometry sample selection
        // ============================================================

        public List<BlockDefinition> GetGeometrySamples()
        {
            return
                _blocks
                .Where(
                    x =>
                        x.Geometry != null &&
                        x.Geometry.HasConstructorSize
                )
                .GroupBy(
                    x =>
                        x.Geometry.ConstructorWidth
                        + "x" +
                        x.Geometry.ConstructorHeight
                )
                .Select(
                    x =>
                        x
                        .OrderBy(
                            b =>
                                b.ProgrammaticName ??
                                b.ClrName
                        )
                        .First()
                )
                .OrderBy(
                    x =>
                        x.Geometry.ConstructorWidth
                )
                .ThenBy(
                    x =>
                        x.Geometry.ConstructorHeight
                )
                .ToList();
        }

        // ============================================================
        // Target support JSON models
        // ============================================================

        private class TargetSupportCatalogueFile
        {
            public string Target { get; set; }

            public string DisplayName { get; set; }

            public string DeviceModelName { get; set; }

            public string DeviceModelType { get; set; }

            public string PlatformFamily { get; set; }

            public string GeneratedUtc { get; set; }

            public string Source { get; set; }

            public List<TargetBlockSupportEntry> Blocks
            {
                get;
                set;
            }
        }

        private class TargetBlockSupportEntry
        {
            public string FullName { get; set; }

            public string ClrName { get; set; }

            public string ProgrammaticName
            {
                get;
                set;
            }

            public string Namespace { get; set; }

            public string State { get; set; }

            public string Source { get; set; }

            public string Notes { get; set; }
        }

        // ============================================================
        // Reflection safety
        // ============================================================

        private IEnumerable<Type> SafeGetTypes(
            Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (
                ReflectionTypeLoadException ex)
            {
                return
                    ex.Types
                    .Where(
                        x => x != null
                    );
            }
            catch
            {
                return
                    new Type[0];
            }
        }
    }
}