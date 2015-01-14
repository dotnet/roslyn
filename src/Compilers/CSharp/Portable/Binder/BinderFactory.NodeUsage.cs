// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BinderFactory
    {
        internal enum NodeUsage : byte
        {
            Normal = 0,
            MethodTypeParameters = 1 << 0,
            MethodBody = 1 << 1,

            ConstructorBodyOrInitializer = 1 << 0,
            AccessorBody = 1 << 0,
            OperatorBody = 1 << 0,

            NamedTypeBodyOrTypeParameters = 1 << 0,
            NamedTypeBaseList = 1 << 1,

            NamespaceBody = 1 << 0,
            NamespaceUsings = 1 << 1,

            CompilationUnitUsings = 1 << 0,
            CompilationUnitScript = 1 << 1,
            CompilationUnitScriptUsings = 1 << 2,

            DocumentationCommentParameter = 1 << 0,
            DocumentationCommentTypeParameter = 1 << 1,
            DocumentationCommentTypeParameterReference = 1 << 2,

            CrefParameterOrReturnType = 1 << 0,
        }
    }
}
