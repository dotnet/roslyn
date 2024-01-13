// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the compiler's analysis of whether an expression may be null
    /// </summary>
    // Review docs: https://github.com/dotnet/roslyn/issues/35046
    public enum NullableFlowState : byte
    {
        /// <summary>
        /// Syntax is not an expression, or was not analyzed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Expression is not null.
        /// </summary>
        NotNull,
        /// <summary>
        /// Expression may be null.
        /// </summary>
        MaybeNull
    }

    internal static class NullableFlowStateExtensions
    {
        /// <summary>
        /// This method directly converts a <see cref="NullableFlowState"/> to a <see cref="NullableAnnotation"/>,
        /// ignoring the <see cref="ITypeSymbol"/> to which it is attached. It should only be used when converting
        /// an RValue flow state to an RValue annotation for returning via the public API. For general use, please
        /// use Microsoft.CodeAnalysis.CSharp.Symbols.TypeWithState.ToTypeWithAnnotations.
        /// </summary>
        public static NullableAnnotation ToAnnotation(this NullableFlowState nullableFlowState)
        {
            switch (nullableFlowState)
            {
                case CodeAnalysis.NullableFlowState.MaybeNull:
                    return CodeAnalysis.NullableAnnotation.Annotated;
                case CodeAnalysis.NullableFlowState.NotNull:
                    return CodeAnalysis.NullableAnnotation.NotAnnotated;
                default:
                    return CodeAnalysis.NullableAnnotation.None;
            }
        }
    }
}
