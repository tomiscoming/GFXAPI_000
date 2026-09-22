using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GFX_BLOCK_CATALOGUE
{
    public static class IPTemplateProbe
    {
        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "IP DEVICE TEMPLATE PROBE"
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

                    string name =
                        type.FullName ??
                        type.Name;

                    if (
                        name.IndexOf(
                            "Template",
                            StringComparison.OrdinalIgnoreCase
                        ) < 0 &&
                        name.IndexOf(
                            "DeviceModel",
                            StringComparison.OrdinalIgnoreCase
                        ) < 0)
                    {
                        continue;
                    }

                    Console.WriteLine();
                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        name
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

                    DumpProperties(
                        type
                    );

                    DumpMethods(
                        type
                    );
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "IP DEVICE TEMPLATE PROBE COMPLETE"
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
                Console.WriteLine(
                    "  CTOR: " +
                    constructor
                );
            }
        }

        private static void DumpProperties(
            Type type)
        {
            PropertyInfo[] properties =
                type.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static
                );

            foreach (
                PropertyInfo property
                in properties
                .OrderBy(
                    x => x.Name
                ))
            {
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

        private static void DumpMethods(
            Type type)
        {
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
                        x.DeclaringType ==
                        type
                )
                .OrderBy(
                    x => x.Name
                ))
            {
                Console.WriteLine(
                    "  METHOD: " +
                    method
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