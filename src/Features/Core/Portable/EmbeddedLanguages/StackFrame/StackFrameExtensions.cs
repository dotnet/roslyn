// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

internal static class StackFrameExtensions
{
    extension(StackFrameTrivia? trivia)
    {
        /// <summary>
        /// Creates an <see cref="ImmutableArray{StackFrameTrivia}"/> with a single value or empty 
        /// if the <paramref name="trivia"/> has no value
        /// </summary>
        public ImmutableArray<StackFrameTrivia> ToImmutableArray()
            => trivia.HasValue ? [trivia.Value] : [];
    }

    extension(StackFrameTrivia trivia)
    {
        /// <summary>
        /// Creates an <see cref="ImmutableArray{StackFrameTrivia}"/> with a single trivia item in it
        /// </summary>
        /// <remarks>
        /// This is created for convenience so callers don't have to have different patterns between nullable and 
        /// non nullable calues
        /// </remarks>
        public ImmutableArray<StackFrameTrivia> ToImmutableArray()
            => [trivia];
    }
}
