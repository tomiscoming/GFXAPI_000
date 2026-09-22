using Distech.Gpl.Model;
using Distech.Gpl.Model.Compilation;
using Distech.Gpl.Model.Platforms.Common.DeviceTemplates;
using Distech.Gpl.Model.Platforms.IP;
using Distech.Gpl.Model.Shapes;
using Distech.Gpl.Model.Shapes.Blocks;
using Distech.Network.Data;
using Distech.Network.Data.Services.Virtuals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Distech.Gpl.Model.Platforms.Common.DeviceTemplates;
using Distech.Network.Data.Services.Virtuals;

namespace GFX_BLOCK_CATALOGUE
{
    public class GfxRuntime
    {
        private readonly List<Assembly> _assemblies;
        private readonly BlockCatalogue _catalogue;
        public Project Project { get; private set; }

        public DrawingDocument Document { get; private set; }

        public BlockCatalogue Catalogue
        {
            get
            {
                return _catalogue;
            }
        }
        private readonly TargetCatalogue _targets;

        public TargetCatalogue Targets
        {
            get
            {
                return _targets;
            }
        }
        // ============================================================
        // Constructor
        // ============================================================

        public GfxRuntime()
        {
            _assemblies =
                LoadAssemblies();

            _targets =
                new TargetCatalogue();

            _catalogue =
                new BlockCatalogue(
                    _assemblies
                );

            Project =
                new Project();

            Document =
                new DrawingDocument(
                    "Main"
                );

            Project.Documents.Add(
                Document
            );
        }

        // ============================================================
        // Block creation
        // ============================================================

        public Block CreateBlock(
            string typeName,
            int x,
            int y)
        {
            Type type =
                ResolveBlockType(
                    typeName
                );

            if (type == null)
            {
                throw new InvalidOperationException(
                    "Block type not found: " +
                    typeName
                );
            }

            if (
                !typeof(Block)
                .IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    "Type is not a Block: " +
                    type.FullName
                );
            }

            if (type.IsAbstract)
            {
                throw new InvalidOperationException(
                    "Block type is abstract: " +
                    type.FullName
                );
            }

            ConstructorInfo constructor =
                type.GetConstructor(
                    Type.EmptyTypes
                );

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    "Block has no public parameterless constructor: " +
                    type.FullName
                );
            }

            Block block =
                Activator.CreateInstance(
                    type    
                ) as Block;

            if (block == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate block: " +
                    type.FullName
                );
            }

            ApplyDefaultGeometry(
                block,
                x,
                y
            );

            Document.Shapes.Add(
                block
            );

            return block;
        }

        public T CreateBlock<T>(
            int x,
            int y)
            where T : Block, new()
        {
            T block =
                new T();

            ApplyDefaultGeometry(
                block,
                x,
                y
            );

            Document.Shapes.Add(
                block
            );

            return block;
        }
        // ============================================================
        // Default block geometry
        // ============================================================

        private void ApplyDefaultGeometry(
            Block block,
            int x,
            int y)
        {
            if (block == null)
            {
                throw new ArgumentNullException(
                    "block"
                );
            }

            // --------------------------------------------------------
            // Always apply requested position first.
            // --------------------------------------------------------

            block.Location =
                new Point(
                    x,
                    y
                );

            // --------------------------------------------------------
            // Look up native geometry from catalogue.
            // --------------------------------------------------------

            BlockDefinition definition =
                _catalogue.Resolve(
                    block.GetType().FullName
                );

            if (
                definition == null ||
                definition.Geometry == null ||
                !definition.Geometry.HasKnownDefaultSize)
            {
                // Unknown geometry:
                // retain native constructor dimensions.
                return;
            }

            // --------------------------------------------------------
            // Apply confirmed EC-gfx native dimensions.
            // --------------------------------------------------------

            block.Bounds =
                new Rectangle(
                    x,
                    y,
                    definition.Geometry.DefaultWidth,
                    definition.Geometry.DefaultHeight
                );
        }

        // ============================================================
        // Property setting
        // ============================================================

        public void SetProperty(
            Block block,
            string propertyName,
            object value)
        {
            if (block == null)
            {
                throw new ArgumentNullException(
                    "block"
                );
            }

            PropertyInfo property =
                block
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance
                );

            if (property == null)
            {
                throw new InvalidOperationException(
                    "Property '" +
                    propertyName +
                    "' not found on " +
                    block.GetType().FullName
                );
            }

            if (!property.CanWrite)
            {
                throw new InvalidOperationException(
                    "Property '" +
                    propertyName +
                    "' is read-only on " +
                    block.GetType().FullName
                );
            }

            object converted =
                ConvertValue(
                    value,
                    property.PropertyType
                );

            property.SetValue(
                block,
                converted,
                null
            );
        }

        // ============================================================
        // Port visibility
        // ============================================================

        public void SetInputVisible(
            Block block,
            string portName,
            bool visible)
        {
            if (block == null)
            {
                throw new ArgumentNullException(
                    "block"
                );
            }

            IPort port =
                block.InputPorts[
                    portName
                ];

            if (port == null)
            {
                throw new InvalidOperationException(
                    "Input port '" +
                    portName +
                    "' not found on " +
                    block.GetType().Name
                );
            }

            port.Visible =
                visible;
        }

        public void SetOutputVisible(
            Block block,
            string portName,
            bool visible)
        {
            if (block == null)
            {
                throw new ArgumentNullException(
                    "block"
                );
            }

            IPort port =
                block.OutputPorts[
                    portName
                ];

            if (port == null)
            {
                throw new InvalidOperationException(
                    "Output port '" +
                    portName +
                    "' not found on " +
                    block.GetType().Name
                );
            }

            port.Visible =
                visible;
        }

        // ============================================================
        // Connections
        // ============================================================

        public void Connect(
            Block fromBlock,
            string outputName,
            Block toBlock,
            string inputName)
        {
            if (fromBlock == null)
            {
                throw new ArgumentNullException(
                    "fromBlock"
                );
            }

            if (toBlock == null)
            {
                throw new ArgumentNullException(
                    "toBlock"
                );
            }

            IOutputPort output =
                fromBlock.OutputPorts[
                    outputName
                ] as IOutputPort;

            if (output == null)
            {
                throw new InvalidOperationException(
                    "Output port '" +
                    outputName +
                    "' not found on " +
                    fromBlock.GetType().Name
                );
            }

            IInputPort input =
                toBlock.InputPorts[
                    inputName
                ] as IInputPort;

            if (input == null)
            {
                throw new InvalidOperationException(
                    "Input port '" +
                    inputName +
                    "' not found on " +
                    toBlock.GetType().Name
                );
            }

            bool linked =
                Block.CreateLink(
                    output,
                    input
                );

            if (!linked)
            {
                throw new InvalidOperationException(
                    "Distech rejected link: " +
                    fromBlock.GetType().Name +
                    "." +
                    outputName +
                    " -> " +
                    toBlock.GetType().Name +
                    "." +
                    inputName
                );
            }
        }

        // ============================================================
        // Native Distech compilation
        // ============================================================

        public List<string> CompileWithDistech()
        {
            DynamicDevice device =
                new DynamicDevice();

            return CompileWithDistechDevice(
                device
            );
        }

        public List<string> CompileWithDistech(
            string targetKey)
        {
            Console.WriteLine(
                "Compiling for target: " +
                (
                    targetKey ??
                    "<null>"
                )
            );

            TargetDefinition target =
                ResolveTarget(
                    targetKey
                );

            if (target == null)
            {
                return new List<string>
        {
            "Unknown compilation target '" +
            targetKey +
            "'."
        };
            }

            if (
                !string.Equals(
                    target.PlatformFamily,
                    "IP",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return new List<string>
        {
            "Target '" +
            target.Key +
            "' uses unsupported platform family '" +
            target.PlatformFamily +
            "'."
        };
            }

            DynamicDevice device =
                new DynamicDevice();

            try
            {
                // ----------------------------------------------------
                // Attach target identity to offline device
                // ----------------------------------------------------

                VirtualDeviceInformationService info =
                    new VirtualDeviceInformationService(
                        device,
                        target.DisplayName,
                        "OFFLINE",
                        target.DeviceModelName,
                        target.DeviceModelType,
                        new Version(
                            1,
                            0
                        )
                    );

                device.AddService(
                    info
                );

                Console.WriteLine(
                    "Attached device identity: " +
                    target.DeviceModelName +
                    " [" +
                    target.DeviceModelType +
                    "]"
                );

                // ----------------------------------------------------
                // Resolve native Distech device template
                // ----------------------------------------------------

                IPDeviceTemplateManager manager =
                    IPDeviceTemplateManager.Instance;

                DeviceTemplate template =
                    manager.GetTemplateForModelType(
                        target.DeviceModelType
                    );

                if (template == null)
                {
                    return new List<string>
            {
                "No Distech device template found for target '" +
                target.Key +
                "' with model type '" +
                target.DeviceModelType +
                "'."
            };
                }

                Console.WriteLine(
                    "Resolved device template: " +
                    template.ModelName +
                    " [" +
                    template.ModelType +
                    "]"
                );

                // ----------------------------------------------------
                // Apply controller template to offline device
                // ----------------------------------------------------

                DeviceTemplateManager
                    .ChangeDeviceTemplate(
                        device,
                        template
                    );

                Console.WriteLine(
                    "Applied device template: " +
                    template.ModelName +
                    " [" +
                    template.ModelType +
                    "]"
                );

                // ----------------------------------------------------
                // Compile against target-aware device
                // ----------------------------------------------------

                return CompileWithDistechDevice(
                    device
                );
            }
            catch (Exception ex)
            {
                return new List<string>
        {
            "Distech target setup exception: " +
            GetDeepestExceptionMessage(
                ex
            )
        };
            }
        }

        private List<string> CompileWithDistechDevice(
            DynamicDevice device)
        {
            List<string> errors =
                new List<string>();

            IPPlatform platform =
                null;

            try
            {
                // ----------------------------------------------------
                // Create offline IP platform
                // ----------------------------------------------------

                platform =
                    new IPPlatform(
                        device,
                        null
                    );

                // ----------------------------------------------------
                // Attach platform to project
                // ----------------------------------------------------

                Project.SetDevicePlatform(
                    platform
                );

                // ----------------------------------------------------
                // Create native compiler
                // ----------------------------------------------------

                IProjectCompiler compiler =
                    platform.CreateCompiler();

                compiler.GenerateDebugMessages =
                    false;

                Console.WriteLine(
                    "Native compiler type: " +
                    compiler
                        .GetType()
                        .FullName
                );

                // ----------------------------------------------------
                // Compile project
                // ----------------------------------------------------

                compiler.CompileProject(
                    Project
                );

                // ----------------------------------------------------
                // Read native compiler error collection
                // ----------------------------------------------------

                PropertyInfo errorsProperty =
                    compiler
                    .GetType()
                    .GetProperty(
                        "Errors",
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

                if (errorsProperty != null)
                {
                    IEnumerable compilerErrors =
                        errorsProperty
                        .GetValue(
                            compiler,
                            null
                        ) as IEnumerable;

                    if (compilerErrors != null)
                    {
                        foreach (
                            object error
                            in compilerErrors)
                        {
                            if (error == null)
                            {
                                continue;
                            }

                            errors.Add(
                                FormatCompilerError(
                                    error
                                )
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(
                    "Distech compiler exception: " +
                    GetDeepestExceptionMessage(
                        ex
                    )
                );
            }
            finally
            {
                if (platform != null)
                {
                    platform.Dispose();
                }
            }

            return errors;
        }

        // ============================================================
        // Save
        // ============================================================

        public void Save(
            string fileName)
        {
            Project.SaveToFile(
                fileName
            );
        }

        // ============================================================
        // Block lookup
        // ============================================================

        public Type ResolveBlockType(
            string typeName)
        {
            if (
                string.IsNullOrWhiteSpace(
                    typeName
                ))
            {
                return null;
            }

            return
                _catalogue.ResolveType(
                    typeName
                );
        }

        public IEnumerable<Type> GetAllBlockTypes()
        {
            return
                _catalogue
                .Blocks
                .Select(
                    x => x.Type
                );
        }

        // ============================================================
        // Target lookup
        // ============================================================

        public TargetDefinition ResolveTarget(
            string targetKey)
        {
            if (
                string.IsNullOrWhiteSpace(
                    targetKey
                ))
            {
                return null;
            }

            return
                _targets.Resolve(
                    targetKey
                );
        }

        public PlatformSupportDefinition GetBlockTargetSupport(
            string blockTypeName,
            string targetKey)
        {
            if (
                string.IsNullOrWhiteSpace(
                    blockTypeName
                ) ||
                string.IsNullOrWhiteSpace(
                    targetKey
                ))
            {
                return null;
            }

            BlockDefinition block =
                _catalogue.Resolve(
                    blockTypeName
                );

            if (block == null)
            {
                return null;
            }

            PlatformSupportDefinition support;

            if (
                block.PlatformSupport.TryGetValue(
                    targetKey,
                    out support
                ))
            {
                return support;
            }

            return null;
        }

        public TargetSupportState GetBlockTargetSupportState(
            string blockTypeName,
            string targetKey)
        {
            PlatformSupportDefinition support =
                GetBlockTargetSupport(
                    blockTypeName,
                    targetKey
                );

            if (support == null)
            {
                return TargetSupportState.Unknown;
            }

            return support.State;
        }

        public bool IsBlockSupportedForTarget(
            string blockTypeName,
            string targetKey)
        {
            return
                GetBlockTargetSupportState(
                    blockTypeName,
                    targetKey
                ) ==
                TargetSupportState.Supported;
        }

        public bool IsBlockUnsupportedForTarget(
            string blockTypeName,
            string targetKey)
        {
            return
                GetBlockTargetSupportState(
                    blockTypeName,
                    targetKey
                ) ==
                TargetSupportState.Unsupported;
        }
        // ============================================================
        // Compiler formatting helpers
        // ============================================================

        private string FormatCompilerError(
            object error)
        {
            Type type =
                error.GetType();

            List<string> parts =
                new List<string>();

            string[] usefulProperties =
            {
                "Severity",
                "Message",
                "Description",
                "Block",
                "Port"
            };

            foreach (
                string propertyName
                in usefulProperties)
            {
                PropertyInfo property =
                    type.GetProperty(
                        propertyName,
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

                if (property == null)
                {
                    continue;
                }

                object value =
                    property.GetValue(
                        error,
                        null
                    );

                if (value == null)
                {
                    continue;
                }

                parts.Add(
                    propertyName +
                    "=" +
                    value
                );
            }

            if (parts.Count > 0)
            {
                return string.Join(
                    " | ",
                    parts
                );
            }

            return error.ToString();
        }

        private string GetDeepestExceptionMessage(
            Exception ex)
        {
            Exception current =
                ex;

            while (
                current.InnerException
                != null)
            {
                current =
                    current.InnerException;
            }

            return current.Message;
        }

        // ============================================================
        // Value conversion
        // ============================================================
        private object ConvertValue(
            object value,
            Type targetType)
        {
            if (value == null)
            {
                if (
                    targetType.IsValueType &&
                    Nullable.GetUnderlyingType(
                        targetType
                    ) == null)
                {
                    throw new InvalidOperationException(
                        "Cannot assign null to " +
                        targetType.FullName
                    );
                }

                return null;
            }

            Type actualTarget =
                Nullable.GetUnderlyingType(
                    targetType
                )
                ?? targetType;

            if (
                actualTarget
                .IsAssignableFrom(
                    value.GetType()
                ))
            {
                return value;
            }

            if (actualTarget.IsEnum)
            {
                if (value is string)
                {
                    return Enum.Parse(
                        actualTarget,
                        value.ToString(),
                        true
                    );
                }

                return Enum.ToObject(
                    actualTarget,
                    value
                );
            }

            return Convert.ChangeType(
                value,
                actualTarget
            );
        }

        // ============================================================
        // Assembly loading
        // ============================================================

        private List<Assembly> LoadAssemblies()
        {
            List<Assembly> result =
                new List<Assembly>();

            Assembly core =
                typeof(Block).Assembly;

            result.Add(
                core
            );

            string directory =
                System.IO.Path
                .GetDirectoryName(
                    core.Location
                );

            string[] files =
                System.IO.Directory
                .GetFiles(
                    directory,
                    "DC.Gpl.Model*.dll"
                );

            foreach (
                string file
                in files)
            {
                try
                {
                    AssemblyName name =
                        AssemblyName
                        .GetAssemblyName(
                            file
                        );

                    Assembly existing =
                        AppDomain
                        .CurrentDomain
                        .GetAssemblies()
                        .FirstOrDefault(
                            a =>
                                string.Equals(
                                    a.GetName().Name,
                                    name.Name,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        );

                    if (existing != null)
                    {
                        if (
                            !result.Contains(
                                existing
                            ))
                        {
                            result.Add(
                                existing
                            );
                        }

                        continue;
                    }

                    Assembly loaded =
                        Assembly.LoadFrom(
                            file
                        );

                    if (
                        !result.Contains(
                            loaded
                        ))
                    {
                        result.Add(
                            loaded
                        );
                    }
                }
                catch
                {
                }
            }

            return result;
        }

        // ============================================================
        // Safe reflection
        // ============================================================

        private IEnumerable<Type>
            SafeGetTypes(
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
                        t => t != null
                    );
            }
        }
    }
}