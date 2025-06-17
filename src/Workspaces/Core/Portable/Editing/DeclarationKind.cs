// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.Editing;

/// <devdocs>
/// This should contain only language-agnostic declarations. Things like record struct should fall under struct, etc.
/// </devdocs>
public enum DeclarationKind
{
    None,
    CompilationUnit,

    /// <summary>
    /// Represents a class declaration, including record class declarations in C#.
    /// </summary>
    Class,

    /// <summary>
    /// Represents a struct declaration, including record struct declarations in C#.
    /// </summary>
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

    /// <summary>
    /// Represents set accessor declaration of a property, including init accessors in C#.
    /// </summary>
    SetAccessor,

    AddAccessor,
    RemoveAccessor,
    RaiseAccessor,

    [Obsolete($"This value is not used. Use {nameof(Class)} instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RecordClass,
}
