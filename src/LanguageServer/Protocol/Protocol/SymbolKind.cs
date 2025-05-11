// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Enum which represents the various kinds of symbols.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#symbolKind">Language Server Protocol specification</see> for additional information.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Names are defined by the LSP")]
internal enum SymbolKind
{
    /// <summary>
    /// Symbol is a file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Symbol is a module.
    /// </summary>
    Module = 2,

    /// <summary>
    /// Symbol is a namespace.
    /// </summary>
    Namespace = 3,

    /// <summary>
    /// Symbol is a package.
    /// </summary>
    Package = 4,

    /// <summary>
    /// Symbol is a class.
    /// </summary>
    Class = 5,

    /// <summary>
    /// Symbol is a method.
    /// </summary>
    Method = 6,

    /// <summary>
    /// Symbol is a property.
    /// </summary>
    Property = 7,

    /// <summary>
    /// Symbol is a field.
    /// </summary>
    Field = 8,

    /// <summary>
    /// Symbol is a constructor.
    /// </summary>
    Constructor = 9,

    /// <summary>
    /// Symbol is an enum.
    /// </summary>
    Enum = 10,

    /// <summary>
    /// Symbol is an interface.
    /// </summary>
    Interface = 11,

    /// <summary>
    /// Symbol is a function.
    /// </summary>
    Function = 12,

    /// <summary>
    /// Symbol is a variable.
    /// </summary>
    Variable = 13,

    /// <summary>
    /// Symbol is a constant.
    /// </summary>
    Constant = 14,

    /// <summary>
    /// Symbol is a string.
    /// </summary>
    String = 15,

    /// <summary>
    /// Symbol is a number.
    /// </summary>
    Number = 16,

    /// <summary>
    /// Symbol is a boolean.
    /// </summary>
    Boolean = 17,

    /// <summary>
    /// Symbol is an array.
    /// </summary>
    Array = 18,

    /// <summary>
    /// Symbol is an object.
    /// </summary>
    Object = 19,

    /// <summary>
    /// Symbol is a key.
    /// </summary>
    Key = 20,

    /// <summary>
    /// Symbol is null.
    /// </summary>
    Null = 21,

    /// <summary>
    /// Symbol is an enum member.
    /// </summary>
    EnumMember = 22,

    /// <summary>
    /// Symbol is a struct.
    /// </summary>
    Struct = 23,

    /// <summary>
    /// Symbol is an event.
    /// </summary>
    Event = 24,

    /// <summary>
    /// Symbol is an operator.
    /// </summary>
    Operator = 25,

    /// <summary>
    /// Symbol is a type parameter.
    /// </summary>
    TypeParameter = 26,
}
