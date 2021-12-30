// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection;

namespace Microsoft.DiaSymReader
{
    internal interface ISymWriterMetadataProvider
    {
        bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes);
        bool TryGetEnclosingType(int nestedTypeToken, out int enclosingTypeToken);
        bool TryGetMethodInfo(int methodDefinitionToken, out string methodName, out int declaringTypeToken);
    }
}
