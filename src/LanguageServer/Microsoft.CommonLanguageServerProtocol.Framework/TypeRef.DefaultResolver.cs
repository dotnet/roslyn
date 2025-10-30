// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal sealed partial class TypeRef
{
    public sealed class DefaultResolver : AbstractTypeRefResolver
    {
        public static readonly DefaultResolver Instance = new();

        private DefaultResolver()
        {
        }

        protected override Type? ResolveCore(TypeRef typeRef)
            => LoadType(typeRef.AssemblyQualifiedName);

        private static Type LoadType(string typeName)
            => Type.GetType(typeName)
            ?? throw new InvalidOperationException($"Could not load type: '{typeName}'");
    }
}
