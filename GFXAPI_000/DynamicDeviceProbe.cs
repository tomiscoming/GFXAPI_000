using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Distech.Network.Data;

namespace GFX_BLOCK_CATALOGUE
{
    public static class DynamicDeviceProbe
    {
        public static void Run()
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "DYNAMIC DEVICE PROBE"
            );

            Console.WriteLine(
                "========================================"
            );

            Type type =
                typeof(DynamicDevice);

            Console.WriteLine();
            Console.WriteLine(
                "Type:"
            );

            Console.WriteLine(
                type.FullName
            );

            Console.WriteLine();
            Console.WriteLine(
                "Assembly:"
            );

            Console.WriteLine(
                type.Assembly.FullName
            );

            DumpConstructors(
                type
            );

            DumpInterfaces(
                type
            );

            DumpProperties(
                type
            );

            DumpMethods(
                type
            );

            DumpAttributes(
                type
            );

            ProbeInstance(
                type
            );

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "DYNAMIC DEVICE PROBE COMPLETE"
            );

            Console.WriteLine(
                "========================================"
            );
        }

        // ============================================================
        // Constructors
        // ============================================================

        private static void DumpConstructors(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "CONSTRUCTORS"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

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
                    FormatMethodBase(
                        constructor
                    )
                );
            }
        }

        // ============================================================
        // Interfaces
        // ============================================================

        private static void DumpInterfaces(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "INTERFACES"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            foreach (
                Type interfaceType
                in type.GetInterfaces()
                .OrderBy(
                    x => x.FullName
                ))
            {
                Console.WriteLine(
                    interfaceType.FullName
                );
            }
        }

        // ============================================================
        // Properties
        // ============================================================

        private static void DumpProperties(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "PROPERTIES"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

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
                string access =
                    "";

                if (property.CanRead)
                {
                    access += "get;";
                }

                if (property.CanWrite)
                {
                    access += " set;";
                }

                Console.WriteLine(
                    property.PropertyType.FullName +
                    " " +
                    property.Name +
                    " { " +
                    access +
                    " }"
                );
            }
        }

        // ============================================================
        // Methods
        // ============================================================

        private static void DumpMethods(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "METHODS"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

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
                        x.DeclaringType !=
                        typeof(object)
                )
                .OrderBy(
                    x => x.Name
                ))
            {
                Console.WriteLine(
                    FormatMethodBase(
                        method
                    )
                );
            }
        }

        // ============================================================
        // Attributes
        // ============================================================

        private static void DumpAttributes(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "ATTRIBUTES"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            object[] attributes =
                type.GetCustomAttributes(
                    true
                );

            foreach (
                object attribute
                in attributes)
            {
                Console.WriteLine(
                    attribute
                        .GetType()
                        .FullName
                );
            }
        }

        // ============================================================
        // Live instance inspection
        // ============================================================

        private static void ProbeInstance(
            Type type)
        {
            Console.WriteLine();
            Console.WriteLine(
                "----------------------------------------"
            );

            Console.WriteLine(
                "INSTANCE PROPERTY VALUES"
            );

            Console.WriteLine(
                "----------------------------------------"
            );

            object instance;

            try
            {
                instance =
                    Activator.CreateInstance(
                        type
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Could not create DynamicDevice:"
                );

                Console.WriteLine(
                    GetDeepestMessage(
                        ex
                    )
                );

                return;
            }

            PropertyInfo[] properties =
                type.GetProperties(
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
                if (
                    !property.CanRead ||
                    property
                        .GetIndexParameters()
                        .Length != 0)
                {
                    continue;
                }

                try
                {
                    object value =
                        property.GetValue(
                            instance,
                            null
                        );

                    Console.WriteLine(
                        property.Name +
                        " = " +
                        FormatValue(
                            value
                        )
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        property.Name +
                        " = <ERROR: " +
                        GetDeepestMessage(
                            ex
                        ) +
                        ">"
                    );
                }
            }
        }

        // ============================================================
        // Formatting
        // ============================================================

        private static string FormatMethodBase(
            MethodBase method)
        {
            List<string> parameters =
                new List<string>();

            foreach (
                ParameterInfo parameter
                in method.GetParameters())
            {
                parameters.Add(
                    parameter.ParameterType.FullName +
                    " " +
                    parameter.Name
                );
            }

            MethodInfo methodInfo =
                method as MethodInfo;

            string returnType =
                "";

            if (methodInfo != null)
            {
                returnType =
                    methodInfo.ReturnType.FullName +
                    " ";
            }

            return
                returnType +
                method.Name +
                "(" +
                string.Join(
                    ", ",
                    parameters
                ) +
                ")";
        }

        private static string FormatValue(
            object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            Type type =
                value.GetType();

            if (
                type.IsPrimitive ||
                value is string ||
                value is decimal ||
                value is Guid)
            {
                return value.ToString();
            }

            return
                value.ToString() +
                " [" +
                type.FullName +
                "]";
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