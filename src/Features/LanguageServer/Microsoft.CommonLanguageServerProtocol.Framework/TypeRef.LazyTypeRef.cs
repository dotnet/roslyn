// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal partial class TypeRef
{
    private sealed class LazyTypeRef(string typeName) : TypeRef(typeName)
    {
        private readonly Lazy<Type> _lazyType = new(LoadType(typeName));

        public override Type GetResolvedType() => _lazyType.Value;

        private static Func<Type> LoadType(string typeName)
            => () => Type.GetType(typeName)
                  ?? throw new InvalidOperationException($"Could not load type: '{typeName}'");
    }
}
