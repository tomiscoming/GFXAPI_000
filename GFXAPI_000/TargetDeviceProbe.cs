using System;
using System.Reflection;

using Distech.Gpl.Model.Compilation;
using Distech.Gpl.Model.Platforms.IP;
using Distech.Network.Data;
using Distech.Network.Data.Services.Virtuals;

namespace GFX_BLOCK_CATALOGUE
{
    public static class TargetDeviceProbe
    {
        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "TARGET DEVICE PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            DynamicDevice device =
                new DynamicDevice();

            device.Name =
                "ECY-S1000 Test";

            // --------------------------------------------------------
            // Attach actual device identity service
            // --------------------------------------------------------

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

            Console.WriteLine();
            Console.WriteLine(
                "Device information service attached."
            );

            Console.WriteLine(
                "DeviceName = " +
                info.DeviceName
            );

            Console.WriteLine(
                "ModelName  = " +
                info.ModelName
            );

            Console.WriteLine(
                "ModelId    = " +
                info.ModelId
            );

            // --------------------------------------------------------
            // Verify DynamicDevice can retrieve it
            // --------------------------------------------------------

            object recovered =
                device.GetService(
                    typeof(
                        VirtualDeviceInformationService
                    )
                );

            Console.WriteLine();

            Console.WriteLine(
                "Recovered service = " +
                (
                    recovered != null
                    ? recovered.GetType().FullName
                    : "<null>"
                )
            );

            // --------------------------------------------------------
            // Create Distech IP platform using target-aware device
            // --------------------------------------------------------

            IPPlatform platform =
                null;

            try
            {
                platform =
                    new IPPlatform(
                        device,
                        null
                    );

                Console.WriteLine();
                Console.WriteLine(
                    "IPPlatform created."
                );

                IProjectCompiler compiler =
                    platform.CreateCompiler();

                Console.WriteLine(
                    "Compiler created."
                );

                Console.WriteLine(
                    "Compiler type:"
                );

                Console.WriteLine(
                    "  " +
                    compiler
                        .GetType()
                        .FullName
                );

                // ----------------------------------------------------
                // Inspect compiler limits again
                // ----------------------------------------------------

                DumpProperty(
                    compiler,
                    "MaximumCodeSize"
                );

                DumpProperty(
                    compiler,
                    "MaximumRamSize"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "FAILED:"
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

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "TARGET DEVICE PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }

        private static void DumpProperty(
            object instance,
            string propertyName)
        {
            PropertyInfo property =
                instance
                    .GetType()
                    .GetProperty(
                        propertyName,
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            if (property == null)
            {
                Console.WriteLine(
                    propertyName +
                    " = <property not found>"
                );

                return;
            }

            try
            {
                object value =
                    property.GetValue(
                        instance,
                        null
                    );

                Console.WriteLine(
                    propertyName +
                    " = " +
                    (
                        value != null
                        ? value.ToString()
                        : "<null>"
                    )
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    propertyName +
                    " = <ERROR: " +
                    GetDeepestMessage(
                        ex
                    ) +
                    ">"
                );
            }
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

            return current.Message;
        }
    }
}