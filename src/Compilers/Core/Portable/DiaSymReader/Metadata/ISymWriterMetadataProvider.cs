// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
