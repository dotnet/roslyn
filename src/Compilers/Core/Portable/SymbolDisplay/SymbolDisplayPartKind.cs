// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the kinds of a piece of classified text (SymbolDisplayPart).
    /// </summary>
    public enum SymbolDisplayPartKind
    {
        /// <summary>The name of an alias.</summary>
        AliasName = 0,
        /// <summary>The name of an assembly.</summary>
        AssemblyName = 1,
        /// <summary>The name of a class.</summary>
        ClassName = 2,
        /// <summary>The name of a delegate.</summary>
        DelegateName = 3,
        /// <summary>The name of an enum.</summary>
        EnumName = 4,
        /// <summary>The name of an error type.</summary>
        /// <seealso cref="IErrorTypeSymbol"/>
        ErrorTypeName = 5,
        /// <summary>The name of an event.</summary>
        EventName = 6,
        /// <summary>The name of a field.</summary>
        FieldName = 7,
        /// <summary>The name of an interface.</summary>
        InterfaceName = 8,
        /// <summary>A language keyword.</summary>
        Keyword = 9,
        /// <summary>The name of a label.</summary>
        LabelName = 10,
        /// <summary>A line-break (i.e. whitespace).</summary>
        LineBreak = 11,
        /// <summary>A numeric literal.</summary>
        /// <remarks>Typically for the default values of parameters and the constant values of fields.</remarks>
        NumericLiteral = 12,
        /// <summary>A string literal.</summary>
        /// <remarks>Typically for the default values of parameters and the constant values of fields.</remarks>
        StringLiteral = 13,
        /// <summary>The name of a local.</summary>
        LocalName = 14,
        /// <summary>The name of a method.</summary>
        MethodName = 15,
        /// <summary>The name of a module.</summary>
        ModuleName = 16,
        /// <summary>The name of a namespace.</summary>
        NamespaceName = 17,
        /// <summary>The symbol of an operator (e.g. "+").</summary>
        Operator = 18,
        /// <summary>The name of a parameter.</summary>
        ParameterName = 19,
        /// <summary>The name of a property.</summary>
        PropertyName = 20,
        /// <summary>A punctuation character (e.g. "(", ".", ",") other than an <see cref="Operator"/>.</summary>
        Punctuation = 21,
        /// <summary>A single space character.</summary>
        Space = 22,
        /// <summary>The name of a struct (structure in Visual Basic).</summary>
        StructName = 23,
        /// <summary>A keyword-like part for anonymous types (not actually a keyword).</summary>
        AnonymousTypeIndicator = 24,
        /// <summary>An unclassified part.</summary>
        /// <remarks>Never returned - only set in user-constructed parts.</remarks>
        Text = 25,
        /// <summary>The name of a type parameter.</summary>
        TypeParameterName = 26,
        /// <summary>The name of a query range variable.</summary>
        RangeVariableName = 27,
        /// <summary>The name of an enum member.</summary>
        EnumMemberName = 28,
        /// <summary>The name of a reduced extension method.</summary>
        /// <remarks>
        /// When an extension method is in it's non-reduced form it will be will be marked as MethodName.
        /// </remarks>
        ExtensionMethodName = 29,
        /// <summary>The name of a field or local constant.</summary>
        ConstantName = 30,
    }

    internal static class InternalSymbolDisplayPartKind
    {
        private const SymbolDisplayPartKind @base = SymbolDisplayPartKind.ConstantName + 1;
        public const SymbolDisplayPartKind Arity = @base + 0;
        public const SymbolDisplayPartKind Other = @base + 1;
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this SymbolDisplayPartKind value)
        {
            return (value >= SymbolDisplayPartKind.AliasName && value <= SymbolDisplayPartKind.ConstantName) ||
                (value >= InternalSymbolDisplayPartKind.Arity && value <= InternalSymbolDisplayPartKind.Other);
        }
    }
}
