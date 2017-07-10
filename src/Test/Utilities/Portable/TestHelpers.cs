// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public static class TestHelpers
    {
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


        /// <summary>
        /// <see cref="System.Xml.Linq.XComment.Value"/> is serialized with "--" replaced by "- -"
        /// </summary>
        public static string AsXmlCommentText(string text)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if ((c == '-') && (i > 0) && (text[i - 1] == '-'))
                {
                    builder.Append(' ');
                }
                builder.Append(c);
            }
            var result = builder.ToString();
            Debug.Assert(!result.Contains("--"));
            return result;
        }

    }
}
