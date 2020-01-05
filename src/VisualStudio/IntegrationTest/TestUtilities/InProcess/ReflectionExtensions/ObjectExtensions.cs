// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess.ReflectionExtensions
{
    internal static class ObjectExtensions
    {
        public static PropertyType GetPropertyValue<PropertyType>(this object instance, string propertyName)
        {
            return (PropertyType)GetPropertyValue(instance, propertyName);
        }

        public static object GetPropertyValue(this object instance, string propertyName)
        {
            Type type = instance.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                throw new ArgumentException("Property " + propertyName + " was not found on type " + type.ToString());
            }
            object result = propertyInfo.GetValue(instance, null);
            return result;
        }

        public static object GetFieldValue(this object instance, string fieldName)
        {
            Type type = instance.GetType();
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
                throw new FieldAccessException("Field " + fieldName + " was not found on type " + type.ToString());
            }
            object result = fieldInfo.GetValue(instance);
            return result; // you can place a breakpoint here (for debugging purposes)
        }

        public static FieldType GetFieldValue<FieldType>(this object instance, string fieldName)
        {
            return (FieldType)GetFieldValue(instance, fieldName);
        }
    }
}
