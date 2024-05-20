// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal abstract partial class TypeRef
{
    private sealed class DefaultResolver : ITypeRefResolver
    {
        public static readonly DefaultResolver Instance = new();

        private DefaultResolver()
        {
        }

        public Type Resolve(TypeRef typeRef)
            => Type.GetType(typeRef.TypeName)
            ?? throw new InvalidOperationException($"Could not load type: '{typeRef.TypeName}'");
    }
}
