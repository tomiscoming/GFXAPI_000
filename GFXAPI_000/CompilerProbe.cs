using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Distech.Gpl.Model.Compilation;
using Distech.Gpl.Model.Platforms.IP;
using Distech.Network.Data;

namespace GfxApi
{
    public static class CompilerProbe
    {
        // ============================================================
        // Main entry point
        // ============================================================

        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("COMPILER API PROBE");
            Console.WriteLine("========================================");

            LoadDistechAssemblies();

            // --------------------------------------------------------
            // Compiler types
            // --------------------------------------------------------

            ProbeType("IProjectCompiler");
            ProbeType("IPProjectCodeCompiler");
            ProbeType("ProjectCodeCompiler");

            // --------------------------------------------------------
            // Build task types
            // --------------------------------------------------------

            ProbeType("BuildTask");
            ProbeType("ShapeRequirementsBuildTask");
            ProbeType("ConfigurationValidationBuildTask");
            ProbeType("PersistenceCheckBuildTask");
            ProbeType("ResourceReferencesBuildTask");

            // --------------------------------------------------------
            // Actual offline compiler construction test
            // --------------------------------------------------------

            TestOfflineCompiler();
        }

        // ============================================================
        // Type reflection probe
        // ============================================================

        private static void ProbeType(
            string shortName)
        {
            Type type =
                AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Name,
                            shortName,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(shortName);
            Console.WriteLine("----------------------------------------");

            if (type == null)
            {
                Console.WriteLine("NOT FOUND");
                return;
            }

            Console.WriteLine(
                "Full Name: " +
                type.FullName
            );

            Console.WriteLine(
                "Assembly: " +
                type.Assembly.GetName().Name
            );

            Console.WriteLine(
                "Base Type: " +
                (
                    type.BaseType != null
                        ? type.BaseType.FullName
                        : "<none>"
                )
            );

            Console.WriteLine(
                "Interface: " +
                type.IsInterface
            );

            Console.WriteLine(
                "Abstract: " +
                type.IsAbstract
            );

            Console.WriteLine();

            PrintConstructors(type);

            PrintProperties(type);

            PrintMethods(type);
        }

        // ============================================================
        // Constructors
        // ============================================================

        private static void PrintConstructors(
            Type type)
        {
            Console.WriteLine("Constructors:");

            ConstructorInfo[] constructors =
                type.GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

            if (constructors.Length == 0)
            {
                Console.WriteLine("  <none>");
                return;
            }

            foreach (
                ConstructorInfo constructor
                in constructors)
            {
                Console.WriteLine(
                    "  " +
                    GetVisibility(constructor) +
                    " " +
                    type.Name +
                    "(" +
                    FormatParameters(
                        constructor.GetParameters()
                    ) +
                    ")"
                );
            }
        }

        // ============================================================
        // Properties
        // ============================================================

        private static void PrintProperties(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine("Properties:");

            PropertyInfo[] properties =
                type.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static
                );

            if (properties.Length == 0)
            {
                Console.WriteLine("  <none>");
                return;
            }

            foreach (
                PropertyInfo property
                in properties
                .OrderBy(x => x.Name))
            {
                Console.WriteLine(
                    "  " +
                    property.PropertyType.Name +
                    " " +
                    property.Name +
                    " [" +
                    (
                        property.CanRead
                            ? "get"
                            : ""
                    ) +
                    (
                        property.CanWrite
                            ? " set"
                            : ""
                    ) +
                    "]"
                );
            }
        }

        // ============================================================
        // Methods
        // ============================================================

        private static void PrintMethods(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine("Methods:");

            MethodInfo[] methods =
                type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static
                );

            foreach (
                MethodInfo method
                in methods
                .Where(
                    x =>
                        x.DeclaringType == type
                )
                .OrderBy(x => x.Name))
            {
                Console.WriteLine(
                    "  " +
                    GetVisibility(method) +
                    " " +
                    method.ReturnType.Name +
                    " " +
                    method.Name +
                    "(" +
                    FormatParameters(
                        method.GetParameters()
                    ) +
                    ")"
                );
            }
        }

        // ============================================================
        // Offline compiler test
        // ============================================================

        public static void TestOfflineCompiler()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("OFFLINE COMPILER TEST");
            Console.WriteLine("========================================");

            try
            {
                // ----------------------------------------------------
                // Blank offline Distech device
                // ----------------------------------------------------

                DynamicDevice device =
                    new DynamicDevice();

                Console.WriteLine(
                    "DynamicDevice created."
                );

                Console.WriteLine(
                    "Device type:"
                );

                Console.WriteLine(
                    "  " +
                    device.GetType().FullName
                );

                Console.WriteLine(
                    "Device path:"
                );

                Console.WriteLine(
                    "  " +
                    device.Path
                );

                // ----------------------------------------------------
                // IP platform
                // ----------------------------------------------------

                IPPlatform platform =
                    new IPPlatform(
                        device,
                        null
                    );

                Console.WriteLine();
                Console.WriteLine(
                    "IPPlatform created."
                );

                Console.WriteLine(
                    "Platform type:"
                );

                Console.WriteLine(
                    "  " +
                    platform.GetType().FullName
                );

                // ----------------------------------------------------
                // Compiler
                // ----------------------------------------------------

                IProjectCompiler compiler =
                    platform.CreateCompiler();

                Console.WriteLine();
                Console.WriteLine(
                    "Compiler created."
                );

                Console.WriteLine(
                    "Compiler type:"
                );

                Console.WriteLine(
                    "  " +
                    compiler.GetType().FullName
                );

                // ----------------------------------------------------
                // Compiler limits
                // ----------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "Compiler limits:"
                );

                Console.WriteLine(
                    "  MaximumCodeSize = " +
                    compiler.MaximumCodeSize
                );

                Console.WriteLine(
                    "  MaximumRamSize = " +
                    compiler.MaximumRamSize
                );

                // ----------------------------------------------------
                // Pre-build tasks
                // ----------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "Pre-build tasks:"
                );

                if (
                    compiler.PreBuildTasks == null ||
                    compiler.PreBuildTasks.Count == 0)
                {
                    Console.WriteLine(
                        "  <none>"
                    );
                }
                else
                {
                    foreach (
                        BuildTask task
                        in compiler.PreBuildTasks)
                    {
                        Console.WriteLine(
                            "  " +
                            task.GetType().FullName
                        );
                    }
                }

                // ----------------------------------------------------
                // Post-build tasks
                // ----------------------------------------------------

                Console.WriteLine();
                Console.WriteLine(
                    "Post-build tasks:"
                );

                if (
                    compiler.PostBuildTasks == null ||
                    compiler.PostBuildTasks.Count == 0)
                {
                    Console.WriteLine(
                        "  <none>"
                    );
                }
                else
                {
                    foreach (
                        BuildTask task
                        in compiler.PostBuildTasks)
                    {
                        Console.WriteLine(
                            "  " +
                            task.GetType().FullName
                        );
                    }
                }

                Console.WriteLine();
                Console.WriteLine(
                    "OFFLINE COMPILER TEST PASSED"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "OFFLINE COMPILER TEST FAILED"
                );

                Console.WriteLine();

                PrintExceptionChain(
                    ex
                );
            }
        }

        // ============================================================
        // Exception output
        // ============================================================

        private static void PrintExceptionChain(
            Exception ex)
        {
            int level = 0;

            while (ex != null)
            {
                Console.WriteLine(
                    new string(
                        ' ',
                        level * 2
                    ) +
                    ex.GetType().FullName +
                    ": " +
                    ex.Message
                );

                ex =
                    ex.InnerException;

                level++;
            }
        }

        // ============================================================
        // Reflection helpers
        // ============================================================

        private static string FormatParameters(
            ParameterInfo[] parameters)
        {
            return string.Join(
                ", ",
                parameters.Select(
                    x =>
                        x.ParameterType.Name +
                        " " +
                        x.Name
                )
            );
        }

        private static string GetVisibility(
            MethodBase method)
        {
            if (method.IsPublic)
            {
                return "public";
            }

            if (method.IsFamily)
            {
                return "protected";
            }

            if (method.IsPrivate)
            {
                return "private";
            }

            if (method.IsAssembly)
            {
                return "internal";
            }

            if (method.IsFamilyOrAssembly)
            {
                return "protected internal";
            }

            return "non-public";
        }

        // ============================================================
        // DLL loading
        // ============================================================

        private static void LoadDistechAssemblies()
        {
            string directory =
                AppDomain.CurrentDomain.BaseDirectory;

            string[] files =
                Directory.GetFiles(
                    directory,
                    "DC.Gpl.Model*.dll"
                );

            foreach (
                string file
                in files)
            {
                try
                {
                    Assembly.LoadFrom(
                        file
                    );
                }
                catch
                {
                    // Already loaded or dependency issue.
                }
            }
        }

        // ============================================================
        // Safe reflection
        // ============================================================

        private static IEnumerable<Type> SafeGetTypes(
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