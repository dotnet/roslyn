// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract class TrackingDiagnosticAnalyzer<TLanguageKindEnum> : TestDiagnosticAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        #region Tracking

        public class Entry
        {
            public readonly string AbstractMemberName;
            public readonly string CallerName;
            public readonly TLanguageKindEnum SyntaxKind;
            public readonly SymbolKind? SymbolKind;
            public readonly MethodKind? MethodKind;
            public readonly bool ReturnsVoid;

            public Entry(string abstractMemberName, string callerName, SyntaxNode node, ISymbol symbol)
            {
                AbstractMemberName = abstractMemberName;
                CallerName = callerName;
                SyntaxKind = node == null ? default(TLanguageKindEnum) : (TLanguageKindEnum)(object)(ushort)node.RawKind;
                SymbolKind = symbol == null ? (SymbolKind?)null : symbol.Kind;
                MethodKind = symbol is IMethodSymbol ? ((IMethodSymbol)symbol).MethodKind : (MethodKind?)null;
                ReturnsVoid = symbol is IMethodSymbol ? ((IMethodSymbol)symbol).ReturnsVoid : false;
            }

            public override string ToString()
            {
                return CallerName + "(" + string.Join(", ", SymbolKind, MethodKind, SyntaxKind) + ")";
            }
        }

        private readonly ConcurrentQueue<Entry> _callLog = new ConcurrentQueue<Entry>();

        protected override void OnAbstractMember(string abstractMemberName, SyntaxNode node, ISymbol symbol, string callerName)
        {
            _callLog.Enqueue(new Entry(abstractMemberName, callerName, node, symbol));
        }

        public IEnumerable<Entry> CallLog
        {
            get { return _callLog; }
        }

        #endregion

        #region Analysis

        private static readonly Regex s_omittedSyntaxKindRegex =
            new Regex(@"None|Trivia|Token|Keyword|List|Xml|Cref|Compilation|Namespace|Class|Struct|Enum|Interface|Delegate|Field|Property|Indexer|Event|Operator|Constructor|Access|Incomplete|Attribute|Filter|InterpolatedString.*");

        private bool FilterByAbstractName(Entry entry, string abstractMemberName)
        {
            return abstractMemberName == entry.AbstractMemberName;
        }

        public void VerifyAllAnalyzerMembersWereCalled()
        {
            var actualMembers = _callLog.Select(e => e.CallerName).Distinct();
            AssertSequenceEqual(AllAnalyzerMemberNames, actualMembers);
        }

        public void VerifyAnalyzeSymbolCalledForAllSymbolKinds()
        {
            var expectedSymbolKinds = new[] { SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Property };
            var actualSymbolKinds = _callLog.Where(a => FilterByAbstractName(a, "Symbol")).Where(e => e.SymbolKind.HasValue).Select(e => e.SymbolKind.Value).Distinct();
            AssertSequenceEqual(expectedSymbolKinds, actualSymbolKinds);
        }

        protected virtual bool IsAnalyzeNodeSupported(TLanguageKindEnum syntaxKind)
        {
            return !s_omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString());
        }

        public void VerifyAnalyzeNodeCalledForAllSyntaxKinds(HashSet<TLanguageKindEnum> syntaxKindsPatterns)
        {
            var expectedSyntaxKinds = AllSyntaxKinds.Where(IsAnalyzeNodeSupported);
            var actualSyntaxKinds = _callLog.Where(a => FilterByAbstractName(a, "SyntaxNode")).Select(e => e.SyntaxKind).Concat(syntaxKindsPatterns).Distinct();
            AssertIsSuperset(expectedSyntaxKinds, actualSyntaxKinds);
        }

        protected virtual bool IsOnCodeBlockSupported(SymbolKind symbolKind, MethodKind methodKind, bool returnsVoid)
        {
            return true;
        }

        public void VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(HashSet<SymbolKind> symbolKindsWithNoCodeBlocks = null, bool allowUnexpectedCalls = false)
        {
            const MethodKind InvalidMethodKind = (MethodKind)(-1);
            var expectedArguments = new[]
            {
                new { SymbolKind = SymbolKind.Event,  MethodKind = InvalidMethodKind, ReturnsVoid = false }, // C# only
                new { SymbolKind = SymbolKind.Field,  MethodKind = InvalidMethodKind, ReturnsVoid = false },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.Constructor, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.Conversion, ReturnsVoid = false },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.Destructor, ReturnsVoid = true }, // C# only
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.EventAdd, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.EventRemove, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.EventRaise, ReturnsVoid = true }, // VB only
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.ExplicitInterfaceImplementation, ReturnsVoid = true }, // C# only
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.Ordinary, ReturnsVoid = false },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.Ordinary, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.PropertyGet, ReturnsVoid = false },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.PropertySet, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.StaticConstructor, ReturnsVoid = true },
                new { SymbolKind = SymbolKind.Method, MethodKind = MethodKind.UserDefinedOperator, ReturnsVoid = false },
                new { SymbolKind = SymbolKind.Property, MethodKind = InvalidMethodKind, ReturnsVoid = false },
            }.AsEnumerable();

            if (symbolKindsWithNoCodeBlocks != null)
            {
                expectedArguments = expectedArguments.Where(a => !symbolKindsWithNoCodeBlocks.Contains(a.SymbolKind));
            }

            expectedArguments = expectedArguments.Where(a => IsOnCodeBlockSupported(a.SymbolKind, a.MethodKind, a.ReturnsVoid));

            var actualOnCodeBlockStartedArguments = _callLog.Where(a => FilterByAbstractName(a, "CodeBlockStart"))
                .Select(e => new { SymbolKind = e.SymbolKind.Value, MethodKind = e.MethodKind ?? InvalidMethodKind, e.ReturnsVoid }).Distinct();
            var actualOnCodeBlockEndedArguments = _callLog.Where(a => FilterByAbstractName(a, "CodeBlock"))
                .Select(e => new { SymbolKind = e.SymbolKind.Value, MethodKind = e.MethodKind ?? InvalidMethodKind, e.ReturnsVoid }).Distinct();

            if (!allowUnexpectedCalls)
            {
                AssertSequenceEqual(expectedArguments, actualOnCodeBlockStartedArguments, items => items.OrderBy(p => p.SymbolKind).ThenBy(p => p.MethodKind).ThenBy(p => p.ReturnsVoid));
                AssertSequenceEqual(expectedArguments, actualOnCodeBlockEndedArguments, items => items.OrderBy(p => p.SymbolKind).ThenBy(p => p.MethodKind).ThenBy(p => p.ReturnsVoid));
            }
            else
            {
                AssertIsSuperset(expectedArguments, actualOnCodeBlockStartedArguments);
                AssertIsSuperset(expectedArguments, actualOnCodeBlockEndedArguments);
            }
        }

        private void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<IEnumerable<T>, IOrderedEnumerable<T>> sorter = null)
        {
            sorter = sorter ?? new Func<IEnumerable<T>, IOrderedEnumerable<T>>(items => items.OrderBy(i => i));
            expected = sorter(expected);
            actual = sorter(actual);
            Assert.True(expected.SequenceEqual(actual),
                Environment.NewLine + "Expected: " + string.Join(", ", expected) +
                Environment.NewLine + "Actual:   " + string.Join(", ", actual));
        }

        private void AssertIsSuperset<T>(IEnumerable<T> expectedSubset, IEnumerable<T> actualSuperset)
        {
            var missingElements = expectedSubset.GroupJoin(actualSuperset, e => e, a => a, (e, a) => new { Element = e, IsMissing = !a.Any() })
                .Where(p => p.IsMissing).Select(p => p.Element).ToList();
            var presentElements = expectedSubset.Except(missingElements);
            Assert.True(missingElements.Count == 0,
                Environment.NewLine + "Missing: " + string.Join(", ", missingElements) +
                Environment.NewLine + "Present: " + string.Join(", ", presentElements));
        }

        #endregion
    }
}
