// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal static class StackFrameExtensions
    {
        /// <summary>
        /// Creates an <see cref="ImmutableArray{StackFrameTrivia}"/> with a single value or empty 
        /// if the <paramref name="trivia"/> has no value
        /// </summary>
        public static ImmutableArray<StackFrameTrivia> ToImmutableArray(this StackFrameTrivia? trivia)
            => trivia.HasValue ? ImmutableArray.Create(trivia.Value) : ImmutableArray<StackFrameTrivia>.Empty;

        /// <summary>
        /// Creates an <see cref="ImmutableArray{StackFrameTrivia}"/> with a single trivia item in it
        /// </summary>
        /// <remarks>
        /// This is created for convenience so callers don't have to have different patterns between nullable and 
        /// non nullable calues
        /// </remarks>
        public static ImmutableArray<StackFrameTrivia> ToImmutableArray(this StackFrameTrivia trivia)
            => ImmutableArray.Create(trivia);
    }
}
