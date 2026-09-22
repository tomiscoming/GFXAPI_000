using System;

using Distech.Gpl.Model.Platforms.IP;
using Distech.Gpl.Model.Platforms.Common.DeviceTemplates;
using Distech.Network.Data;
using Distech.Network.Data.Services.Virtuals;

namespace GFX_BLOCK_CATALOGUE
{
    public static class S1000TemplateProbe
    {
        private const string S1000ModelName =
            "ECY-S1000";

        private const string S1000ModelType =
            "10016C000502040D";

        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "S1000 TEMPLATE PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            // --------------------------------------------------------
            // Create offline dynamic device
            // --------------------------------------------------------

            DynamicDevice device =
                new DynamicDevice();

            device.Name =
                "ECY-S1000 Test";

            // --------------------------------------------------------
            // Attach device identity service
            // --------------------------------------------------------

            VirtualDeviceInformationService info =
                new VirtualDeviceInformationService(
                    device,
                    "ECY-S1000 Test",
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

            // --------------------------------------------------------
            // Get IP template manager
            // --------------------------------------------------------

            IPDeviceTemplateManager manager =
                IPDeviceTemplateManager.Instance;

            Console.WriteLine();
            Console.WriteLine(
                "Manager:"
            );

            Console.WriteLine(
                manager.GetType().FullName
            );

            Console.WriteLine();
            Console.WriteLine(
                "Supported model count:"
            );

            Console.WriteLine(
                IPDeviceTemplateManager
                    .SupportedModelTypes
                    .Count
            );

            // --------------------------------------------------------
            // Look up S1000 template
            // --------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine(
                "Looking up:"
            );

            Console.WriteLine(
                S1000ModelType
            );

            DeviceTemplate template =
                manager.GetTemplateForModelType(
                    S1000ModelType
                );

            if (template == null)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "TEMPLATE NOT FOUND"
                );

                Console.WriteLine();
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "S1000 TEMPLATE PROBE COMPLETE"
                );

                Console.WriteLine(
                    "========================================"
                );

                return;
            }

            // --------------------------------------------------------
            // Print template details
            // --------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine(
                "TEMPLATE FOUND"
            );

            Console.WriteLine(
                "ModelName = " +
                template.ModelName
            );

            Console.WriteLine(
                "ModelType = " +
                template.ModelType
            );

            Console.WriteLine(
                "Generation = " +
                template.Generation
            );

            Console.WriteLine(
                "Type = " +
                template.Type
            );

            Console.WriteLine(
                "IsDefault = " +
                template.IsDefault
            );

            Console.WriteLine(
                "Obsolete = " +
                template.Obsolete
            );

            Console.WriteLine(
                "Components = " +
                template.Components.Count
            );

            // --------------------------------------------------------
            // Apply template
            // --------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine(
                "Applying template to device..."
            );

            DeviceTemplateManager
                .ChangeDeviceTemplate(
                    device,
                    template
                );

            Console.WriteLine(
                "Template applied."
            );

            // --------------------------------------------------------
            // Confirm model type after application
            // --------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine(
                "Device model type after apply:"
            );

            Console.WriteLine(
                DeviceTemplateManager
                    .GetDeviceModelType(
                        device
                    )
            );

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "S1000 TEMPLATE PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }
    }
}