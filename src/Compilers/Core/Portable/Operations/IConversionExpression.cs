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
                              /// <summary>
                              /// Gets the underlying conversion. This will be either <see cref="Microsoft.CodeAnalysis.CSharp.Conversion"/>
                              /// or <see cref="Microsoft.CodeAnalysis.VisualBasic.Conversion"/>. This is a boxing operation: if you need
                              /// conversion information that is language specific, use either TODO or TODO, which do not allocate memory.
                              /// </summary>
        IConversion Conversion { get; }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
                              /// <summary>
                              /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
                              /// </summary>
        bool IsExplicitInCode { get; }
        /// <summary>
        /// The language that defined this conversion. Possible values are <see cref="LanguageNames.CSharp"/> and
        /// <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>
        string LanguageName { get; }
    }
}

