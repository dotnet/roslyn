// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an element of a property subpattern, which identifies a member to be matched and the
    /// pattern to match it against.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPropertySubpatternOperation : IOperation
    {
        /// <summary>
        /// The member being matched in a property subpattern.  This can be a <see cref="IMemberReferenceOperation"/>
        /// in non-error cases, or an <see cref="IInvalidOperation"/> in error cases.
        /// </summary>
        // The symbol should be exposed for error cases somehow:
        // https://github.com/dotnet/roslyn/issues/33175
        IOperation Member { get; }

        /// <summary>
        /// The pattern to which the member is matched in a property subpattern.
        /// </summary>
        IPatternOperation Pattern { get; }
    }
}

