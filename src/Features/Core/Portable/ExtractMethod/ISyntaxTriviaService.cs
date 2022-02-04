// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal enum TriviaLocation
    {
        BeforeBeginningOfSpan = 0,
        AfterBeginningOfSpan,
        BeforeEndOfSpan,
        AfterEndOfSpan
    }

    internal struct PreviousNextTokenPair
    {
        public SyntaxToken PreviousToken { get; set; }
        public SyntaxToken NextToken { get; set; }
    }

    internal struct LeadingTrailingTriviaPair
    {
        public IEnumerable<SyntaxTrivia> LeadingTrivia { get; set; }
        public IEnumerable<SyntaxTrivia> TrailingTrivia { get; set; }
    }

    internal delegate SyntaxToken AnnotationResolver(SyntaxNode root, TriviaLocation location, SyntaxAnnotation annotation);
    internal delegate IEnumerable<SyntaxTrivia> TriviaResolver(TriviaLocation location, PreviousNextTokenPair tokenPair, Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap);

    /// <summary>
    /// contains information to restore trivia later on to the annotated tree
    /// </summary>
    internal interface ITriviaSavedResult
    {
        /// <summary>
        /// root node of the annotated tree.
        /// </summary>
        SyntaxNode Root { get; }

        /// <summary>
        /// restore saved trivia to given tree
        /// </summary>
        /// <param name="root">root node to the annotated tree</param>
        /// <param name="annotationResolver">it provides a custom way of resolving annotations to retrieve right tokens to attach trivia</param>
        /// <param name="triviaResolver">it provides a custom way of creating trivia list between two tokens</param>
        /// <returns>root node to a trivia restored tree</returns>
        SyntaxNode RestoreTrivia(SyntaxNode root, AnnotationResolver annotationResolver = null, TriviaResolver triviaResolver = null);
    }

    /// <summary>
    /// syntax trivia related services
    /// </summary>
    internal interface ISyntaxTriviaService : ILanguageService
    {
        /// <summary>
        /// save trivia around span and let user restore trivia later
        /// </summary>
        /// <param name="root">root node of a tree</param>
        /// <param name="textSpan">selection whose trivia around its edges will be saved</param>
        /// <returns>object that holds onto enough information to restore trivia later</returns>
        ITriviaSavedResult SaveTriviaAroundSelection(SyntaxNode root, TextSpan textSpan);
    }
}
