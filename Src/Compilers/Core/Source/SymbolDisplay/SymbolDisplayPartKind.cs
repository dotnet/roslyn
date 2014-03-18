// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the kinds of a piece of classified text (SymbolDisplayPart).
    /// </summary>
    public enum SymbolDisplayPartKind
    {
        /// <summary>The name of an alias.</summary>
        AliasName,
        /// <summary>The name of an assembly.</summary>
        AssemblyName,
        /// <summary>The name of a class.</summary>
        ClassName,
        /// <summary>The name of a delegate.</summary>
        DelegateName,
        /// <summary>The name of an enum.</summary>
        EnumName,
        /// <summary>The name of an error type.</summary>
        /// <seealso cref="IErrorTypeSymbol"/>
        ErrorTypeName,
        /// <summary>The name of an event.</summary>
        EventName,
        /// <summary>The name of a field.</summary>
        FieldName,
        /// <summary>The name of an interface.</summary>
        InterfaceName,
        /// <summary>A language keyword.</summary>
        Keyword,
        /// <summary>The name of a label.</summary>
        LabelName,
        /// <summary>A line-break (i.e. whitespace).</summary>
        LineBreak,
        /// <summary>A numeric literal.</summary>
        /// <remarks>Typically for the default values of parameters and the constant values of fields.</remarks>
        NumericLiteral,
        /// <summary>A string literal.</summary>
        /// <remarks>Typically for the default values of parameters and the constant values of fields.</remarks>
        StringLiteral,
        /// <summary>The name of a local.</summary>
        LocalName,
        /// <summary>The name of a method.</summary>
        MethodName,
        /// <summary>The name of a module.</summary>
        ModuleName,
        /// <summary>The name of a namespace.</summary>
        NamespaceName,
        /// <summary>The symbol of an operator (e.g. "+").</summary>
        Operator,
        /// <summary>The name of a parameter.</summary>
        ParameterName,
        /// <summary>The name of a property.</summary>
        PropertyName,
        /// <summary>A punctuation character (e.g. "(", ".", ",") other than an <see cref="Operator"/>.</summary>
        Punctuation,
        /// <summary>A single space character.</summary>
        Space,
        /// <summary>The name of a struct (structure in Visual Basic).</summary>
        StructName,
        /// <summary>A keyword-like part for anonymous types (not actually a keyword).</summary>
        AnonymousTypeIndicator,
        /// <summary>An unclassified part.</summary>
        /// <remarks>Never returned - only set in user-constructed parts.</remarks>
        Text,
        /// <summary>The name of a type parameter.</summary>
        TypeParameterName,
        /// <summary>The name of a query range variable..</summary>
        RangeVariableName
    }

    internal static class InternalSymbolDisplayPartKind
    {
        private const SymbolDisplayPartKind @base = SymbolDisplayPartKind.RangeVariableName + 1;
        public const SymbolDisplayPartKind Arity = @base + 0;
        public const SymbolDisplayPartKind Other = @base + 1;
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this SymbolDisplayPartKind value)
        {
            return (value >= SymbolDisplayPartKind.AliasName && value <= SymbolDisplayPartKind.RangeVariableName) ||
                (value >= InternalSymbolDisplayPartKind.Arity && value <= InternalSymbolDisplayPartKind.Other);
        }
    }
}