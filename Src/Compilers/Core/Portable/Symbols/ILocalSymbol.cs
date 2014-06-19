// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a local variable in method body.
    /// </summary>
    public interface ILocalSymbol : ISymbol
    {
        /// <summary>
        /// Gets the type of this local variable.
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// Returns true if this local variable was declared as "const" (i.e. is a constant declaration).
        /// Also returns true for an enum member.
        /// </summary>
        bool IsConst { get; }

        /// <summary>
        /// Returns false if the local variable wasn't declared as "const", or constant value was omitted or erroneous.
        /// True otherwise.
        /// </summary>
        bool HasConstantValue { get; }

        /// <summary>
        /// Gets the constant value of this local variable.
        /// </summary>
        object ConstantValue { get; }

        // TODO: Add XML doc comment.
        bool IsFunctionValue { get; }
    }
}