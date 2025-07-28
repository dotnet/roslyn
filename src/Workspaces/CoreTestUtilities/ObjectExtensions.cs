// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.UnitTests;

public static class ObjectExtensions
{
    extension(object instance)
    {
        public PropertyType GetPropertyValue<PropertyType>(string propertyName)
        {
            return (PropertyType)GetPropertyValue(instance, propertyName);
        }

        public object GetPropertyValue(string propertyName)
        {
            var type = instance.GetType();
            var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                throw new ArgumentException("Property " + propertyName + " was not found on type " + type.ToString());
            }

            var result = propertyInfo.GetValue(instance, null);
            return result;
        }

        public object GetFieldValue(string fieldName)
        {
            var type = instance.GetType();
            FieldInfo fieldInfo = null;
            while (type != null)
            {
                fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    break;
                }

                type = type.BaseType;
            }

            if (fieldInfo == null)
            {
                throw new FieldAccessException($"Field '{fieldName}' was not found on type '{instance.GetType()}'");
            }

            var result = fieldInfo.GetValue(instance);
            return result; // you can place a breakpoint here (for debugging purposes)
        }

        public FieldType GetFieldValue<FieldType>(string fieldName)
        {
            return (FieldType)GetFieldValue(instance, fieldName);
        }
    }
}
