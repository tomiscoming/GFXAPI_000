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
using System.Reflection;

namespace GFX_BLOCK_CATALOGUE
{
    public static class TargetCompilerProbe
    {
        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "TARGET COMPILER PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine();
            Console.WriteLine(
                "GENERIC DEVICE TEST"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            RunGeneric();

            Console.WriteLine();
            Console.WriteLine(
                "ECY-S1000 DEVICE TEST"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            RunS1000();

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "TARGET COMPILER PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }

        // ============================================================
        // Generic device
        // ============================================================

        private static void RunGeneric()
        {
            DynamicDevice device =
                new DynamicDevice();

            CompileBooleanConstant(
                device,
                false
            );
        }

        // ============================================================
        // ECY-S1000 device
        // ============================================================

        private static void RunS1000()
        {
            DynamicDevice device =
                new DynamicDevice();

            VirtualDeviceInformationService info =
                new VirtualDeviceInformationService(
                    device,
                    "ECY-S1000 Test",
                    "OFFLINE",
                    "ECY-S1000",
                    "10016C000502040D",
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
                    "10016C000502040D"
                );

            if (template == null)
            {
                Console.WriteLine(
                    "S1000 template not found."
                );

                return;
            }

            DeviceTemplateManager
                .ChangeDeviceTemplate(
                    device,
                    template
                );

            Console.WriteLine(
                "Applied template:"
            );

            Console.WriteLine(
                "  " +
                template.ModelName
            );

            Console.WriteLine(
                "  " +
                template.ModelType
            );

            Console.WriteLine();

            Console.WriteLine(
                "Device model type:"
            );

            Console.WriteLine(
                "  " +
                DeviceTemplateManager
                    .GetDeviceModelType(
                        device
                    )
            );

            Console.WriteLine();

            CompileBooleanConstant(
                device,
                true
            );
        }

        // ============================================================
        // Compile test
        // ============================================================

        private static void CompileBooleanConstant(
            DynamicDevice device,
            bool targetAware)
        {
            GfxRuntime gfx =
                new GfxRuntime();

            Block block;

            try
            {
                block =
                    gfx.CreateBlock(
                        "Boolean Constant",
                        50,
                        50
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "RESULT: BLOCK CREATION FAILED"
                );

                Console.WriteLine(
                    GetDeepestMessage(
                        ex
                    )
                );

                return;
            }

            block.Name =
                targetAware
                    ? "S1000_BOOLEAN_CONSTANT"
                    : "GENERIC_BOOLEAN_CONSTANT";

            Console.WriteLine(
                "Block resolved:"
            );

            Console.WriteLine(
                "  CLR type = " +
                block.GetType().FullName
            );

            try
            {
                Console.WriteLine(
                    "  Programmatic name = " +
                    block.ProgrammaticName
                );
            }
            catch
            {
            }

            Console.WriteLine();

            Project project =
                gfx.Project;

            IPPlatform platform =
                null;

            try
            {
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

                Console.WriteLine(
                    "Compiler type:"
                );

                Console.WriteLine(
                    "  " +
                    compiler
                        .GetType()
                        .FullName
                );

                Console.WriteLine();

                compiler.CompileProject(
                    project
                );

                List<string> errors =
                    ReadCompilerErrors(
                        compiler
                    );

                if (errors.Count == 0)
                {
                    Console.WriteLine(
                        "RESULT: PASSED"
                    );

                    Console.WriteLine(
                        "No compiler errors."
                    );
                }
                else
                {
                    Console.WriteLine(
                        "RESULT: FAILED"
                    );

                    Console.WriteLine(
                        errors.Count +
                        " compiler error(s):"
                    );

                    Console.WriteLine();

                    foreach (
                        string error
                        in errors)
                    {
                        Console.WriteLine(
                            "  " +
                            error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "RESULT: EXCEPTION"
                );

                Console.WriteLine(
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
        }

        // ============================================================
        // Compiler error reader
        // ============================================================

        private static List<string> ReadCompilerErrors(
            IProjectCompiler compiler)
        {
            List<string> errors =
                new List<string>();

            PropertyInfo property =
                compiler
                    .GetType()
                    .GetProperty(
                        "Errors",
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            if (property == null)
            {
                Console.WriteLine(
                    "WARNING: Compiler Errors property not found."
                );

                return errors;
            }

            IEnumerable values =
                property.GetValue(
                    compiler,
                    null
                ) as IEnumerable;

            if (values == null)
            {
                return errors;
            }

            foreach (
                object error
                in values)
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

            return errors;
        }

        // ============================================================
        // Compiler error formatting
        // ============================================================

        private static string FormatCompilerError(
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
                return string.Join(
                    " | ",
                    parts
                );
            }

            return error.ToString();
        }

        // ============================================================
        // Exception helper
        // ============================================================

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

            return current.Message;
        }
    }
}