using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Distech.Network.Data.Services;

namespace GFX_BLOCK_CATALOGUE
{
    public static class DeviceServiceProbe
    {
        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "DEVICE SERVICE PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            List<Assembly> assemblies =
                AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(
                    x =>
                        x.GetName()
                            .Name
                            .StartsWith(
                                "DC.",
                                StringComparison.OrdinalIgnoreCase
                            )
                )
                .OrderBy(
                    x => x.GetName().Name
                )
                .ToList();

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
                    if (type == null)
                    {
                        continue;
                    }

                    if (
                        !typeof(IService)
                            .IsAssignableFrom(
                                type
                            ))
                    {
                        continue;
                    }

                    if (type.IsInterface)
                    {
                        continue;
                    }

                    Console.WriteLine();
                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        type.FullName
                    );

                    Console.WriteLine(
                        "Assembly: " +
                        assembly.GetName().Name
                    );

                    Console.WriteLine(
                        "Abstract: " +
                        type.IsAbstract
                    );

                    DumpConstructors(
                        type
                    );

                    DumpInterestingProperties(
                        type
                    );
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "DEVICE SERVICE PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }

        private static void DumpConstructors(
            Type type)
        {
            ConstructorInfo[] constructors =
                type.GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

            foreach (
                ConstructorInfo constructor
                in constructors)
            {
                string parameters =
                    string.Join(
                        ", ",
                        constructor
                            .GetParameters()
                            .Select(
                                x =>
                                    x.ParameterType.Name +
                                    " " +
                                    x.Name
                            )
                            .ToArray()
                    );

                Console.WriteLine(
                    "  CTOR: " +
                    type.Name +
                    "(" +
                    parameters +
                    ")"
                );
            }
        }

        private static void DumpInterestingProperties(
            Type type)
        {
            PropertyInfo[] properties =
                type.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

            foreach (
                PropertyInfo property
                in properties
                .OrderBy(
                    x => x.Name
                ))
            {
                string name =
                    property.Name;

                if (
                    name.IndexOf(
                        "Model",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    name.IndexOf(
                        "Device",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    name.IndexOf(
                        "Type",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    name.IndexOf(
                        "Template",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    name.IndexOf(
                        "Product",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0 &&
                    name.IndexOf(
                        "Hardware",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0)
                {
                    continue;
                }

                Console.WriteLine(
                    "  PROP: " +
                    property.PropertyType.FullName +
                    " " +
                    property.Name +
                    " get=" +
                    property.CanRead +
                    " set=" +
                    property.CanWrite
                );
            }
        }

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