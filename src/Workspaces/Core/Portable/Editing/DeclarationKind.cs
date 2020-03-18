// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Editing
#else
namespace Microsoft.CodeAnalysis.Editing
#endif
{
    public enum DeclarationKind
    {
        None,
        CompilationUnit,
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
        Method,
        Operator,
        ConversionOperator,
        Constructor,
        Destructor,
        Field,
        Property,
        Indexer,
        EnumMember,
        Event,
        CustomEvent,
        Namespace,
        NamespaceImport,
        Parameter,
        Variable,
        Attribute,
        LambdaExpression,
        GetAccessor,
        SetAccessor,
        AddAccessor,
        RemoveAccessor,
        RaiseAccessor
    }
}
