// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Roslyn.Test.Utilities
{
    public static class TestHelpers
    {
        public static IEnumerable<Type> GetAllTypesImplementingGivenInterface(Assembly assembly, Type interfaceType)
        {
            if (assembly == null || interfaceType == null || !interfaceType.IsInterface)
            {
                throw new ArgumentException("interfaceType is not an interface.", nameof(interfaceType));
            }

            return assembly.GetTypes().Where((t) =>
            {
                // simplest way to get types that implement mef type
                // we might need to actually check whether type export the interface type later
                if (t.IsAbstract)
                {
                    return false;
                }

                var candidate = t.GetInterface(interfaceType.ToString());
                return candidate != null && candidate.Equals(interfaceType);
            }).ToList();
        }

        public static IEnumerable<Type> GetAllTypesSubclassingType(Assembly assembly, Type type)
        {
            if (assembly == null || type == null)
            {
                throw new ArgumentException("Invalid arguments");
            }

            return (from t in assembly.GetTypes()
                    where !t.IsAbstract
                    where type.IsAssignableFrom(t)
                    select t).ToList();
        }

        public static IEnumerable<Type> GetAllTypesWithStaticFieldsImplementingType(Assembly assembly, Type type)
        {
            return assembly.GetTypes().Where(t =>
            {
                return t.GetFields(BindingFlags.Public | BindingFlags.Static).Any(f => type.IsAssignableFrom(f.FieldType));
            }).ToList();
        }

        public static string GetCultureInvariantString(object value)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();
            if (valueType == typeof(string))
            {
                return value as string;
            }

            if (valueType == typeof(DateTime))
            {
                return ((DateTime)value).ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(float))
            {
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(decimal))
            {
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
