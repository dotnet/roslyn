// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal readonly partial record struct TypeRef
{
    private sealed class DefaultResolverImpl : ITypeRefResolver
    {
        private static readonly Dictionary<string, Type> s_typeNameToTypeMap = [];

        public Type? Resolve(TypeRef? typeRef)
        {
            var typeRefValue = typeRef.GetValueOrDefault();
            if (typeRefValue.IsDefault)
            {
                return null;
            }

            var typeName = typeRefValue.TypeName;

            lock (s_typeNameToTypeMap)
            {
                if (!s_typeNameToTypeMap.TryGetValue(typeName, out var result))
                {
                    result = LoadType(typeName);
                    s_typeNameToTypeMap.Add(typeName, result);
                }

                return result;
            }
        }

        private static Type LoadType(string typeName)
            => Type.GetType(typeName)
            ?? throw new InvalidOperationException($"Could not load type: '{typeName}'");
    }
}
