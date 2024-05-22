// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ExtensionTypeRefResolver(IAssemblyLoader assemblyLoader) : AbstractTypeRefResolver
{
    protected override Type? ResolveCore(TypeRef typeRef)
    {
        return Type.GetType(
            typeRef.TypeName,
            assemblyResolver: assemblyLoader.LoadAssembly,
            typeResolver: null);
    }
}
