// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

// Verifies recursively that a syntax tree has no gaps in terms of position/location.
internal class SyntaxTreeVerifier
{
    public static void Verify(RazorSyntaxTree syntaxTree, bool ensureFullFidelity = true)
    {
        using var verifier = new Verifier(syntaxTree.Source);
        verifier.Visit(syntaxTree.Root);

        if (ensureFullFidelity)
        {
            var syntaxTreeString = syntaxTree.Root.ToString();
            var sourceText = syntaxTree.Source.Text;
            var builder = new StringBuilder(sourceText.Length);
            for (var i = 0; i < sourceText.Length; i++)
            {
                builder.Append(sourceText[i]);
            }

            var sourceString = builder.ToString();

            // Make sure the syntax tree contains all of the text in the document.
            AssertEx.Equal(sourceString, syntaxTreeString);
        }

        // Verify that NextToken/PreviousToken/FirstToken/LastToken work correctly
        ref readonly var tokens = ref verifier.AllTokens;

        if (tokens.Count == 0)
        {
            Assert.Fail("No tokens found in the syntax tree. There should at least be an EOF token.");
        }

        var root = syntaxTree.Root;
        var lastToken = root.GetLastToken(includeZeroWidth: true);
        var firstToken = root.GetFirstToken(includeZeroWidth: true);
        Assert.Equal(SyntaxKind.EndOfFile, lastToken.Kind);
        Assert.Equal(default, lastToken.GetNextToken(includeZeroWidth: true));
        Assert.Equal(default, lastToken.GetNextToken(includeZeroWidth: false));
        Assert.Equal(default, firstToken.GetPreviousToken(includeZeroWidth: true));
        Assert.Equal(default, firstToken.GetPreviousToken(includeZeroWidth: false));

        Assert.Equal(tokens[0], firstToken);
        Assert.Equal(tokens[^1], lastToken);

        if (tokens.Count == 1)
        {
            Assert.Equal(lastToken, firstToken);
            Assert.Equal(default, lastToken.GetPreviousToken(includeZeroWidth: true));
            Assert.Equal(default, lastToken.GetPreviousToken(includeZeroWidth: false));
            return;
        }

        for (var i = 1; i < (tokens.Count - 1); i++)
        {
            var previousTokenIndex = i - 1;
            var previous = tokens[previousTokenIndex];
            var current = tokens[i];
            Assert.Equal(previous.GetNextToken(includeZeroWidth: true), current);
            Assert.Equal(current.GetPreviousToken(includeZeroWidth: true), previous);
            validateNonZeroWidth(previous.GetNextToken(includeZeroWidth: false), previousTokenIndex, countUp: true, in tokens);
            validateNonZeroWidth(previous.GetPreviousToken(includeZeroWidth: false), previousTokenIndex, countUp: false, in tokens);
        }

        validateNonZeroWidth(lastToken.GetPreviousToken(includeZeroWidth: false), tokens.Count - 1, countUp: false, in tokens);

        void validateNonZeroWidth(SyntaxToken foundNonZeroWidthToken, int originalTokenIndex, bool countUp, in PooledArrayBuilder<SyntaxToken> tokens)
        {
            var (targetIndex, increment) = countUp ? (tokens.Count, 1) : (-1, -1);
            if (foundNonZeroWidthToken.Kind == SyntaxKind.None)
            {
                for (var i = originalTokenIndex + increment; i != targetIndex; i += increment)
                {
                    Assert.Equal(0, tokens[i].Width);
                }

                return;
            }

            Assert.NotEqual(0, foundNonZeroWidthToken.Width);

            for (var i = originalTokenIndex + increment; i != targetIndex; i += increment)
            {
                var token = tokens[i];

                if (token.Width == 0)
                {
                    continue;
                }

                Assert.Equal(foundNonZeroWidthToken, token);
                return;
            }

            Assert.Fail("Did not find the non-zero width token in the list of tokens.");
        }
    }

    private class Verifier : SyntaxWalker, IDisposable
    {
        private readonly RazorSourceDocument _source;
        private SourceLocation _currentLocation;
#pragma warning disable CA1805
        internal PooledArrayBuilder<SyntaxToken> AllTokens = new();
#pragma warning restore CA1805

        public Verifier(RazorSourceDocument source)
        {
            _currentLocation = new SourceLocation(source.FilePath, 0, 0, 0);
            _source = source;
        }

        public override void VisitToken(SyntaxToken token)
        {
            if (token.Kind != SyntaxKind.None)
            {
                AllTokens.Add(token);
                if (!token.IsMissing && token.Kind != SyntaxKind.Marker)
                {
                    var start = token.GetSourceLocation(_source);
                    if (!start.Equals(_currentLocation))
                    {
                        throw new InvalidOperationException($"Token starting at {start} should start at {_currentLocation} - {token} ");
                    }

                    _currentLocation = SourceLocationTracker.Advance(_currentLocation, token.Content);
                }
            }

            base.VisitToken(token);
        }

        public void Dispose()
        {
            AllTokens.Dispose();
        }
    }
}
