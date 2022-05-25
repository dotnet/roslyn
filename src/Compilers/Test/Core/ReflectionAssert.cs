// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Reflection;

namespace Roslyn.Test.Utilities
{
    public static class ReflectionAssert
    {
        public static void AssertPublicAndInternalFieldsAndProperties(Type targetType, params string[] expectedFieldsAndProperties)
        {
            var fields = targetType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var properties = targetType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fieldsAndProps = fields.Where(f => !f.IsPrivate).Select(f => f.Name).Concat(
                                 properties.Where(p => p.GetMethod != null && !p.GetMethod.IsPrivate).Select(p => p.Name)).
                                 OrderBy(s => s);

            AssertEx.SetEqual(expectedFieldsAndProperties, fieldsAndProps, itemSeparator: "\r\n");
        }
    }
}
