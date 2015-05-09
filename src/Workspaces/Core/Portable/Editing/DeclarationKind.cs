// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editing
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
