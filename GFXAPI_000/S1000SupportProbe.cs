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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace GFX_BLOCK_CATALOGUE
{
    public static class S1000SupportProbe
    {
        private const string S1000ModelName =
            "ECY-S1000";

        private const string S1000ModelType =
            "10016C000502040D";

        private const string TargetKey =
            "ECY-S1000";

        // ============================================================
        // Entry point
        // ============================================================

        public static void Run(
            GfxRuntime gfx)
        {
            if (gfx == null)
            {
                throw new ArgumentNullException(
                    "gfx"
                );
            }

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "S1000 SUPPORT PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            List<BlockDefinition> blocks =
                gfx.Catalogue
                    .Blocks
                    .OrderBy(
                        x => x.FullName
                    )
                    .ToList();

            Console.WriteLine();
            Console.WriteLine(
                "Blocks to test: " +
                blocks.Count
            );

            Console.WriteLine();

            List<SupportResult> results =
                new List<SupportResult>();

            int index =
                0;

            foreach (
                BlockDefinition definition
                in blocks)
            {
                index++;

                Console.WriteLine(
                    "[" +
                    index +
                    "/" +
                    blocks.Count +
                    "] " +
                    (
                        definition.ProgrammaticName ??
                        definition.ClrName
                    )
                );

                Console.WriteLine(
                    "    CLR       : " +
                    definition.FullName
                );

                SupportTestResult generic =
                    TestBlock(
                        definition,
                        false
                    );

                SupportTestResult s1000 =
                    TestBlock(
                        definition,
                        true
                    );

                SupportResult result =
                    new SupportResult();

                result.ClrName =
                    definition.ClrName;

                result.FullName =
                    definition.FullName;

                result.ProgrammaticName =
                    definition.ProgrammaticName;

                result.Namespace =
                    definition.Namespace;

                result.GenericIpSupported =
                    generic.Supported;

                result.GenericIpTested =
                    generic.Tested;

                result.S1000Supported =
                    s1000.Supported;

                result.S1000Tested =
                    s1000.Tested;

                result.GenericUnsupportedError =
                    generic.UnsupportedError;

                result.S1000UnsupportedError =
                    s1000.UnsupportedError;

                result.GenericOtherErrors =
                    generic.OtherErrors;

                result.S1000OtherErrors =
                    s1000.OtherErrors;

                result.Classification =
                    Classify(
                        generic,
                        s1000
                    );

                results.Add(
                    result
                );

                Console.WriteLine(
                    "    Generic IP: " +
                    FormatStatus(
                        generic
                    )
                );

                Console.WriteLine(
                    "    ECY-S1000 : " +
                    FormatStatus(
                        s1000
                    )
                );

                Console.WriteLine(
                    "    Class     : " +
                    result.Classification
                );
            }

            string csvOutputPath =
                @"C:\Temp\S1000.csv";

            string jsonOutputPath =
                @"C:\Temp\S1000.json";

            WriteCsv(
                csvOutputPath,
                results
            );

            WriteJson(
                jsonOutputPath,
                results
            );

            PrintSummary(
                results
            );

            Console.WriteLine();
            Console.WriteLine(
                "Saved CSV:"
            );

            Console.WriteLine(
                csvOutputPath
            );

            Console.WriteLine();
            Console.WriteLine(
                "Saved JSON:"
            );

            Console.WriteLine(
                jsonOutputPath
            );

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "S1000 SUPPORT PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }

        // ============================================================
        // Test one block
        // ============================================================

        private static SupportTestResult TestBlock(
            BlockDefinition definition,
            bool useS1000)
        {
            SupportTestResult result =
                new SupportTestResult();

            result.Tested =
                false;

            result.Supported =
                false;

            if (definition == null)
            {
                result.OtherErrors =
                    "Block definition is null.";

                return result;
            }

            if (definition.Type == null)
            {
                result.OtherErrors =
                    "Block CLR type is null.";

                return result;
            }

            if (
                !definition
                    .HasPublicParameterlessConstructor)
            {
                result.OtherErrors =
                    "No public parameterless constructor.";

                return result;
            }

            Project project =
                new Project();

            DrawingDocument document =
                new DrawingDocument(
                    "Main"
                );

            project.Documents.Add(
                document
            );

            Block block;

            try
            {
                block =
                    Activator.CreateInstance(
                        definition.Type
                    ) as Block;

                if (block == null)
                {
                    result.OtherErrors =
                        "Activator.CreateInstance returned null.";

                    return result;
                }

                document.Shapes.Add(
                    block
                );
            }
            catch (Exception ex)
            {
                result.OtherErrors =
                    "Block creation exception: " +
                    GetDeepestMessage(
                        ex
                    );

                return result;
            }

            DynamicDevice device =
                null;

            IPPlatform platform =
                null;

            try
            {
                if (useS1000)
                {
                    device =
                        CreateS1000Device();
                }
                else
                {
                    device =
                        new DynamicDevice();
                }

                platform =
                    new IPPlatform(
                        device,
                        null
                    );

                project.SetDevicePlatform(
                    platform
                );

                IProjectCompiler compiler =
                    platform.CreateCompiler();

                compiler.GenerateDebugMessages =
                    false;

                compiler.CompileProject(
                    project
                );

                result.Tested =
                    true;

                ReadCompilerResults(
                    compiler,
                    result
                );

                // ----------------------------------------------------
                // Supported means the compiler did NOT produce
                // the native "not supported by current device" error.
                //
                // Other compiler errors are preserved separately.
                // ----------------------------------------------------

                result.Supported =
                    string.IsNullOrWhiteSpace(
                        result.UnsupportedError
                    );
            }
            catch (Exception ex)
            {
                result.OtherErrors =
                    AppendError(
                        result.OtherErrors,
                        "Compiler exception: " +
                        GetDeepestMessage(
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

            return result;
        }

        // ============================================================
        // Create S1000 device
        // ============================================================

        private static DynamicDevice CreateS1000Device()
        {
            DynamicDevice device =
                new DynamicDevice();

            device.Name =
                "ECY-S1000 Probe";

            VirtualDeviceInformationService info =
                new VirtualDeviceInformationService(
                    device,
                    "ECY-S1000 Probe",
                    "OFFLINE",
                    S1000ModelName,
                    S1000ModelType,
                    new Version(
                        1,
                        0
                    )
                );

            device.AddService(
                info
            );

            IPDeviceTemplateManager manager =
                IPDeviceTemplateManager.Instance;

            DeviceTemplate template =
                manager.GetTemplateForModelType(
                    S1000ModelType
                );

            if (template == null)
            {
                throw new InvalidOperationException(
                    "ECY-S1000 device template not found: " +
                    S1000ModelType
                );
            }

            DeviceTemplateManager
                .ChangeDeviceTemplate(
                    device,
                    template
                );

            return device;
        }

        // ============================================================
        // Read compiler results
        // ============================================================

        private static void ReadCompilerResults(
            IProjectCompiler compiler,
            SupportTestResult result)
        {
            PropertyInfo errorsProperty =
                compiler
                    .GetType()
                    .GetProperty(
                        "Errors",
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            if (errorsProperty == null)
            {
                result.OtherErrors =
                    AppendError(
                        result.OtherErrors,
                        "Compiler Errors property not found."
                    );

                return;
            }

            IEnumerable compilerErrors =
                errorsProperty.GetValue(
                    compiler,
                    null
                ) as IEnumerable;

            if (compilerErrors == null)
            {
                return;
            }

            foreach (
                object error
                in compilerErrors)
            {
                if (error == null)
                {
                    continue;
                }

                string message =
                    GetErrorProperty(
                        error,
                        "Message"
                    );

                string formatted =
                    FormatCompilerError(
                        error
                    );

                if (
                    !string.IsNullOrWhiteSpace(
                        message
                    ) &&
                    message.IndexOf(
                        "not supported by the current device",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    result.UnsupportedError =
                        AppendError(
                            result.UnsupportedError,
                            formatted
                        );
                }
                else
                {
                    result.OtherErrors =
                        AppendError(
                            result.OtherErrors,
                            formatted
                        );
                }
            }
        }

        // ============================================================
        // Classification
        // ============================================================

        private static string Classify(
            SupportTestResult generic,
            SupportTestResult s1000)
        {
            if (
                !generic.Tested ||
                !s1000.Tested)
            {
                return
                    "TEST_FAILED";
            }

            if (
                generic.Supported &&
                s1000.Supported)
            {
                return
                    "SUPPORTED";
            }

            if (
                !generic.Supported &&
                !s1000.Supported)
            {
                return
                    "PLATFORM_UNSUPPORTED";
            }

            if (
                generic.Supported &&
                !s1000.Supported)
            {
                return
                    "S1000_UNSUPPORTED";
            }

            if (
                !generic.Supported &&
                s1000.Supported)
            {
                return
                    "S1000_SPECIFIC_SUPPORTED";
            }

            return
                "UNKNOWN";
        }

        private static string FormatStatus(
            SupportTestResult result)
        {
            if (!result.Tested)
            {
                return
                    "TEST FAILED";
            }

            if (result.Supported)
            {
                return
                    "SUPPORTED";
            }

            return
                "UNSUPPORTED";
        }

        // ============================================================
        // Summary
        // ============================================================

        private static void PrintSummary(
            List<SupportResult> results)
        {
            int supported =
                results.Count(
                    x =>
                        x.Classification ==
                        "SUPPORTED"
                );

            int platformUnsupported =
                results.Count(
                    x =>
                        x.Classification ==
                        "PLATFORM_UNSUPPORTED"
                );

            int s1000Unsupported =
                results.Count(
                    x =>
                        x.Classification ==
                        "S1000_UNSUPPORTED"
                );

            int s1000SpecificSupported =
                results.Count(
                    x =>
                        x.Classification ==
                        "S1000_SPECIFIC_SUPPORTED"
                );

            int testFailed =
                results.Count(
                    x =>
                        x.Classification ==
                        "TEST_FAILED"
                );

            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "SUMMARY"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "Supported by Generic + S1000 : " +
                supported
            );

            Console.WriteLine(
                "Unsupported by both          : " +
                platformUnsupported
            );

            Console.WriteLine(
                "S1000-specific unsupported   : " +
                s1000Unsupported
            );

            Console.WriteLine(
                "S1000-specific supported     : " +
                s1000SpecificSupported
            );

            Console.WriteLine(
                "Test failures                : " +
                testFailed
            );

            Console.WriteLine(
                "Total                        : " +
                results.Count
            );
        }

        // ============================================================
        // CSV
        // ============================================================

        private static void WriteCsv(
            string fileName,
            List<SupportResult> results)
        {
            string directory =
                Path.GetDirectoryName(
                    fileName
                );

            if (
                !string.IsNullOrWhiteSpace(
                    directory
                ) &&
                !Directory.Exists(
                    directory
                ))
            {
                Directory.CreateDirectory(
                    directory
                );
            }

            StringBuilder csv =
                new StringBuilder();

            csv.AppendLine(
                "ClrName," +
                "FullName," +
                "ProgrammaticName," +
                "Namespace," +
                "GenericIpTested," +
                "GenericIpSupported," +
                "S1000Tested," +
                "S1000Supported," +
                "Classification," +
                "GenericUnsupportedError," +
                "S1000UnsupportedError," +
                "GenericOtherErrors," +
                "S1000OtherErrors"
            );

            foreach (
                SupportResult result
                in results)
            {
                csv.Append(
                    Csv(
                        result.ClrName
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.FullName
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.ProgrammaticName
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.Namespace
                    )
                );

                csv.Append(",");

                csv.Append(
                    result.GenericIpTested
                        ? "true"
                        : "false"
                );

                csv.Append(",");

                csv.Append(
                    result.GenericIpSupported
                        ? "true"
                        : "false"
                );

                csv.Append(",");

                csv.Append(
                    result.S1000Tested
                        ? "true"
                        : "false"
                );

                csv.Append(",");

                csv.Append(
                    result.S1000Supported
                        ? "true"
                        : "false"
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.Classification
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.GenericUnsupportedError
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.S1000UnsupportedError
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.GenericOtherErrors
                    )
                );

                csv.Append(",");

                csv.Append(
                    Csv(
                        result.S1000OtherErrors
                    )
                );

                csv.AppendLine();
            }

            File.WriteAllText(
                fileName,
                csv.ToString(),
                Encoding.UTF8
            );
        }

        private static string Csv(
            string value)
        {
            if (value == null)
            {
                return
                    "\"\"";
            }

            string cleaned =
                value
                    .Replace(
                        "\r",
                        " "
                    )
                    .Replace(
                        "\n",
                        " "
                    )
                    .Replace(
                        "\"",
                        "\"\""
                    );

            return
                "\"" +
                cleaned +
                "\"";
        }

        // ============================================================
        // JSON
        // ============================================================

        private static void WriteJson(
            string fileName,
            List<SupportResult> results)
        {
            string directory =
                Path.GetDirectoryName(
                    fileName
                );

            if (
                !string.IsNullOrWhiteSpace(
                    directory
                ) &&
                !Directory.Exists(
                    directory
                ))
            {
                Directory.CreateDirectory(
                    directory
                );
            }

            TargetSupportCatalogueFile catalogue =
                new TargetSupportCatalogueFile();

            catalogue.Target =
                TargetKey;

            catalogue.DisplayName =
                "ECY-S1000";

            catalogue.DeviceModelName =
                S1000ModelName;

            catalogue.DeviceModelType =
                S1000ModelType;

            catalogue.PlatformFamily =
                "IP";

            catalogue.GeneratedUtc =
                DateTime.UtcNow.ToString(
                    "o"
                );

            catalogue.Source =
                "Native Distech compiler probe";

            catalogue.Blocks =
                new List<TargetBlockSupportEntry>();

            foreach (
                SupportResult result
                in results)
            {
                TargetBlockSupportEntry entry =
                    new TargetBlockSupportEntry();

                entry.FullName =
                    result.FullName;

                entry.ClrName =
                    result.ClrName;

                entry.ProgrammaticName =
                    result.ProgrammaticName;

                entry.Namespace =
                    result.Namespace;

                entry.State =
                    GetTargetSupportState(
                        result
                    );

                entry.Source =
                    "Native Distech compiler probe";

                entry.Notes =
                    BuildTargetSupportNotes(
                        result
                    );

                catalogue.Blocks.Add(
                    entry
                );
            }

            JavaScriptSerializer serializer =
                new JavaScriptSerializer();

            serializer.MaxJsonLength =
                int.MaxValue;

            string json =
                serializer.Serialize(
                    catalogue
                );

            string formattedJson =
                PrettyPrintJson(
                    json
                );

            File.WriteAllText(
                fileName,
                formattedJson,
                Encoding.UTF8
            );
        }

        private static string GetTargetSupportState(
            SupportResult result)
        {
            if (
                result == null ||
                !result.S1000Tested)
            {
                return
                    TargetSupportState
                        .Unknown
                        .ToString();
            }

            if (result.S1000Supported)
            {
                return
                    TargetSupportState
                        .Supported
                        .ToString();
            }

            return
                TargetSupportState
                    .Unsupported
                    .ToString();
        }

        private static string BuildTargetSupportNotes(
            SupportResult result)
        {
            List<string> parts =
                new List<string>();

            if (
                !string.IsNullOrWhiteSpace(
                    result.Classification
                ))
            {
                parts.Add(
                    "Probe classification: " +
                    result.Classification
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    result.S1000UnsupportedError
                ))
            {
                parts.Add(
                    result.S1000UnsupportedError
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    result.S1000OtherErrors
                ))
            {
                parts.Add(
                    "Other compiler messages: " +
                    result.S1000OtherErrors
                );
            }

            if (parts.Count == 0)
            {
                return null;
            }

            return
                string.Join(
                    " || ",
                    parts
                );
        }

        // ============================================================
        // Pretty JSON
        // ============================================================

        private static string PrettyPrintJson(
            string json)
        {
            if (
                string.IsNullOrWhiteSpace(
                    json
                ))
            {
                return json;
            }

            StringBuilder result =
                new StringBuilder();

            bool inString =
                false;

            bool escaped =
                false;

            int indent =
                0;

            for (
                int i = 0;
                i < json.Length;
                i++)
            {
                char c =
                    json[i];

                if (inString)
                {
                    result.Append(
                        c
                    );

                    if (escaped)
                    {
                        escaped =
                            false;
                    }
                    else if (c == '\\')
                    {
                        escaped =
                            true;
                    }
                    else if (c == '"')
                    {
                        inString =
                            false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString =
                            true;

                        result.Append(
                            c
                        );

                        break;

                    case '{':
                    case '[':
                        result.Append(
                            c
                        );

                        result.AppendLine();

                        indent++;

                        AppendIndent(
                            result,
                            indent
                        );

                        break;

                    case '}':
                    case ']':
                        result.AppendLine();

                        indent--;

                        AppendIndent(
                            result,
                            indent
                        );

                        result.Append(
                            c
                        );

                        break;

                    case ',':
                        result.Append(
                            c
                        );

                        result.AppendLine();

                        AppendIndent(
                            result,
                            indent
                        );

                        break;

                    case ':':
                        result.Append(
                            ": "
                        );

                        break;

                    default:
                        if (!char.IsWhiteSpace(c))
                        {
                            result.Append(
                                c
                            );
                        }

                        break;
                }
            }

            return
                result.ToString();
        }

        private static void AppendIndent(
            StringBuilder builder,
            int indent)
        {
            builder.Append(
                new string(
                    ' ',
                    indent * 2
                )
            );
        }

        // ============================================================
        // Compiler error helpers
        // ============================================================

        private static string GetErrorProperty(
            object error,
            string propertyName)
        {
            if (error == null)
            {
                return null;
            }

            PropertyInfo property =
                error
                    .GetType()
                    .GetProperty(
                        propertyName,
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            if (property == null)
            {
                return null;
            }

            try
            {
                object value =
                    property.GetValue(
                        error,
                        null
                    );

                if (value == null)
                {
                    return null;
                }

                return
                    value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string FormatCompilerError(
            object error)
        {
            Type type =
                error.GetType();

            List<string> parts =
                new List<string>();

            string[] propertyNames =
            {
                "Severity",
                "Message",
                "Description",
                "Block",
                "Port"
            };

            foreach (
                string propertyName
                in propertyNames)
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

                object value;

                try
                {
                    value =
                        property.GetValue(
                            error,
                            null
                        );
                }
                catch
                {
                    continue;
                }

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
                return
                    string.Join(
                        " | ",
                        parts
                    );
            }

            return
                error.ToString();
        }

        private static string AppendError(
            string existing,
            string value)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                ))
            {
                return existing;
            }

            if (
                string.IsNullOrWhiteSpace(
                    existing
                ))
            {
                return value;
            }

            return
                existing +
                " || " +
                value;
        }

        private static string GetDeepestMessage(
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

            return
                current.Message;
        }

        // ============================================================
        // Probe result models
        // ============================================================

        private class SupportTestResult
        {
            public bool Tested { get; set; }

            public bool Supported { get; set; }

            public string UnsupportedError { get; set; }

            public string OtherErrors { get; set; }
        }

        private class SupportResult
        {
            public string ClrName { get; set; }

            public string FullName { get; set; }

            public string ProgrammaticName { get; set; }

            public string Namespace { get; set; }

            public bool GenericIpTested { get; set; }

            public bool GenericIpSupported { get; set; }

            public bool S1000Tested { get; set; }

            public bool S1000Supported { get; set; }

            public string Classification { get; set; }

            public string GenericUnsupportedError
            {
                get;
                set;
            }

            public string S1000UnsupportedError
            {
                get;
                set;
            }

            public string GenericOtherErrors
            {
                get;
                set;
            }

            public string S1000OtherErrors
            {
                get;
                set;
            }
        }

        // ============================================================
        // JSON models
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
    }
}