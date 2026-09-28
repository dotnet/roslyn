// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public static class IntermediateNodeVerifier
{
    public static void Verify(IntermediateNode node, string[] baseline)
    {
        var walker = new Walker(baseline);
        walker.Visit(node);
        walker.AssertReachedEndOfBaseline();
    }

    private sealed class Walker : IntermediateNodeWalker
    {
        private readonly string[] _baseline;
        private readonly IntermediateNodeWriter _visitor;
        private readonly StringWriter _writer;

        private int _index;

        public Walker(string[] baseline)
        {
            _baseline = baseline;
            _writer = new StringWriter();
            _visitor = new IntermediateNodeWriter(_writer);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            var expected = _index < _baseline.Length ? _baseline[_index++] : null;

            // Write the node as text for comparison
            _writer.GetStringBuilder().Clear();
            _visitor.Visit(node);
            var actual = _writer.GetStringBuilder().ToString();

            AssertNodeEquals(node, Ancestors, expected, actual);

            _visitor.Depth++;
            base.VisitDefault(node);
            _visitor.Depth--;
        }

        public void AssertReachedEndOfBaseline()
        {
            // Since we're walking the nodes of our generated code there's the chance that our baseline is longer.
            Assert.True(_baseline.Length == _index, "Not all lines of the baseline were visited!");
        }

        private static void AssertNodeEquals(IntermediateNode node, ReadOnlySpan<IntermediateNode> ancestors, string? expected, string actual)
        {
            if (string.Equals(expected, actual))
            {
                // YAY!!! everything is great.
                return;
            }

            if (expected == null)
            {
                var message = "The node is missing from baseline.";
                throw new IntermediateNodeBaselineException(node, ancestors, expected, actual, message);
            }

            var charsVerified = 0;
            AssertNestingEqual(node, ancestors, expected, actual, ref charsVerified);
            AssertNameEqual(node, ancestors, expected, actual, ref charsVerified);
            AssertDelimiter(expected, actual, true, ref charsVerified);
            AssertLocationEqual(node, ancestors, expected, actual, ref charsVerified);
            AssertDelimiter(expected, actual, false, ref charsVerified);
            AssertContentEqual(node, ancestors, expected, actual, ref charsVerified);

            throw new InvalidOperationException("We can't figure out HOW these two things are different. This is a bug.");
        }

        private static void AssertNestingEqual(
            IntermediateNode node, ReadOnlySpan<IntermediateNode> ancestors, string expected, string actual, ref int charsVerified)
        {
            var i = 0;
            for (; i < expected.Length; i++)
            {
                if (expected[i] != ' ')
                {
                    break;
                }
            }

            var failed = false;
            var j = 0;
            for (; j < i; j++)
            {
                if (actual.Length <= j || actual[j] != ' ')
                {
                    failed = true;
                    break;
                }
            }

            if (actual.Length <= j + 1 || actual[j] == ' ')
            {
                failed = true;
            }

            if (failed)
            {
                var message = "The node is at the wrong level of nesting. This usually means a child is missing.";
                throw new IntermediateNodeBaselineException(node, ancestors, expected, actual, message);
            }

            charsVerified = j;
        }

        private static void AssertNameEqual(
            IntermediateNode node, ReadOnlySpan<IntermediateNode> ancestors, string expected, string actual, ref int charsVerified)
        {
            var expectedName = GetName(expected, charsVerified);
            var actualName = GetName(actual, charsVerified);

            if (!string.Equals(expectedName, actualName))
            {
                var message = "Node names are not equal.";
                throw new IntermediateNodeBaselineException(node, ancestors, expected, actual, message);
            }

            charsVerified += expectedName.Length;
        }

        // Either both strings need to have a delimiter next or neither should.
        private static void AssertDelimiter(string expected, string actual, bool required, ref int charsVerified)
        {
            if (charsVerified == expected.Length && required)
            {
                throw new InvalidOperationException($"Baseline text is not well-formed: '{expected}'.");
            }

            if (charsVerified == actual.Length && required)
            {
                throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
            }

            if (charsVerified == expected.Length && charsVerified == actual.Length)
            {
                return;
            }

            var expectedDelimiter = expected.IndexOf(" - ", charsVerified, StringComparison.Ordinal);
            if (expectedDelimiter != charsVerified && expectedDelimiter != -1)
            {
                throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
            }

            var actualDelimiter = actual.IndexOf(" - ", charsVerified, StringComparison.Ordinal);
            if (actualDelimiter != charsVerified && actualDelimiter != -1)
            {
                throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
            }

            Assert.Equal(expectedDelimiter, actualDelimiter);

            charsVerified += 3;
        }

        private static void AssertLocationEqual(
            IntermediateNode node, ReadOnlySpan<IntermediateNode> ancestors, string expected, string actual, ref int charsVerified)
        {
            var expectedLocation = GetLocation(expected, charsVerified);
            var actualLocation = GetLocation(actual, charsVerified);

            if (expectedLocation != actualLocation)
            {
                var message = "Locations are not equal.";
                throw new IntermediateNodeBaselineException(node, ancestors, expected, actual, message);
            }

            charsVerified += expectedLocation.Length;
        }

        private static void AssertContentEqual(
            IntermediateNode node, ReadOnlySpan<IntermediateNode> ancestors, string expected, string actual, ref int charsVerified)
        {
            var expectedContent = GetContent(expected, charsVerified);
            var actualContent = GetContent(actual, charsVerified);

            if (expectedContent != actualContent)
            {
                var message = "Contents are not equal.";
                throw new IntermediateNodeBaselineException(node, ancestors, expected, actual, message);
            }

            charsVerified += expectedContent.Length;
        }

        private static string GetName(string text, int start)
        {
            var delimiter = text.IndexOf(" - ", start, StringComparison.Ordinal);
            if (delimiter == -1)
            {
                throw new InvalidOperationException($"Baseline text is not well-formed: '{text}'.");
            }

            return text[start..delimiter];
        }

        private static string GetLocation(string text, int start)
        {
            var delimiter = text.IndexOf(" - ", start, StringComparison.Ordinal);
            return delimiter == -1 ? text[start..] : text[start..delimiter];
        }

        private static string GetContent(string text, int start)
        {
            return start == text.Length ? string.Empty : text[start..];
        }

        private sealed class IntermediateNodeBaselineException : XunitException
        {
            public IntermediateNode Node { get; }
            public string? Actual { get; }
            public string? Expected { get; }

            public IntermediateNodeBaselineException(
                IntermediateNode node,
                ReadOnlySpan<IntermediateNode> ancestors,
                string? expected,
                string? actual,
                string userMessage)
                : base(Format(ancestors, expected, actual, userMessage))
            {
                Node = node;
                Expected = expected;
                Actual = actual;
            }

            private static string Format(ReadOnlySpan<IntermediateNode> ancestors, string? expected, string? actual, string userMessage)
            {
                using var _ = StringBuilderPool.GetPooledObject(out var builder);

                builder.AppendLine(userMessage);
                builder.AppendLine();

                if (expected != null)
                {
                    builder.Append("Expected: ");
                    builder.AppendLine(expected);
                }

                if (actual != null)
                {
                    builder.Append("Actual: ");
                    builder.AppendLine(actual);
                }

                if (!ancestors.IsEmpty)
                {
                    builder.AppendLine();
                    builder.AppendLine("Path:");

                    foreach (var ancestor in ancestors)
                    {
                        builder.AppendLine(ancestor.ToString());
                    }
                }

                return builder.ToString();
            }
        }
    }
}
