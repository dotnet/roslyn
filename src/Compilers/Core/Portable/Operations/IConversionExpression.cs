// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConversionExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IOperation Operand { get; }
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        // These crefs come from conversions defined in the C# and VB specific projects
        /// <summary>
        /// Gets the underlying conversion. This will be either <see cref="Microsoft.CodeAnalysis.CSharp.Conversion"/>
        /// or <see cref="Microsoft.CodeAnalysis.VisualBasic.Conversion"/>.
        /// </summary>
        /// <remarks>
        /// This is a boxing operation: if you need conversion information that is language specific, use either
        /// <see cref="Microsoft.CodeAnalysis.CSharp.IConversionExpressionExtensions.GetCSharpConversion(IConversionExpression)"/> or
        /// <see cref="Microsoft.CodeAnalysis.VisualBasic.GetVisualBasicConversion(IConversionExpression)"/>, which do not allocate memory.
        /// </remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        IConversion Conversion { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
        /// </summary>
        bool IsExplicitInCode { get; }
        /// <summary>
        /// True if the conversion will fail with an exception at runtime if the cast fails. This is false for C#'s <code>as</code>
        /// operator and for VB's <code>TryCast</code> operator.
        /// </summary>
        bool ThrowsExceptionOnFailure { get; }
        /// <summary>
        /// The language that defined this conversion. Possible values are <see cref="LanguageNames.CSharp"/> and
        /// <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>
        string LanguageName { get; }
    }
}

