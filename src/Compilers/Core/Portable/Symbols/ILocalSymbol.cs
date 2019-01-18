// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a local variable in method body.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
        /// Returns true if this local is a ref local or a ref readonly local.
        /// Use <see cref="RefKind"/> to get more detailed information.
        /// </summary>
        bool IsRef { get; }

        /// <summary>
        /// Whether the variable is a ref or ref readonly local.
        /// </summary>
        RefKind RefKind { get; }

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

        /// <summary>
        /// Returns true if the local variable is declared with fixed-pointer-initializer (in unsafe context).
        /// </summary>
        bool IsFixed { get; }
    }
}
