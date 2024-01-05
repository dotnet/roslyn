// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using static EmbeddedSyntaxHelpers;
    using static RegexHelpers;

    using RegexToken = EmbeddedSyntaxToken<RegexKind>;

    internal partial struct RegexParser
    {
        /// <summary>
        /// Analyzes the first parsed tree to determine the set of capture numbers and names.  These are
        /// then used to do the second parsing pass as they can change how the regex engine interprets
        /// some parts of the pattern (though not the groups themselves).
        /// </summary>
        private struct CaptureInfoAnalyzer
        {
            private readonly VirtualCharSequence _text;
            private readonly ImmutableDictionary<int, TextSpan>.Builder _captureNumberToSpan;
            private readonly ImmutableDictionary<string, TextSpan>.Builder _captureNameToSpan;
            private readonly ArrayBuilder<string> _captureNames;
            private int _autoNumber;
            private int _recursionDepth;

            private CaptureInfoAnalyzer(VirtualCharSequence text)
            {
                _text = text;
                _captureNumberToSpan = ImmutableDictionary.CreateBuilder<int, TextSpan>();
                _captureNameToSpan = ImmutableDictionary.CreateBuilder<string, TextSpan>();
                _captureNames = ArrayBuilder<string>.GetInstance();
                _autoNumber = 1;
                _recursionDepth = 0;

                _captureNumberToSpan.Add(0, text.IsEmpty ? default : GetSpan(text));
            }

            public static (ImmutableDictionary<string, TextSpan>, ImmutableDictionary<int, TextSpan>) Analyze(
                VirtualCharSequence text, RegexCompilationUnit root, RegexOptions options)
            {
                var analyzer = new CaptureInfoAnalyzer(text);
                return analyzer.Analyze(root, options);
            }

            private (ImmutableDictionary<string, TextSpan>, ImmutableDictionary<int, TextSpan>) Analyze(
                RegexCompilationUnit root, RegexOptions options)
            {
                CollectCaptures(root, options);
                AssignNumbersToCaptureNames();

                _captureNames.Free();
                return (_captureNameToSpan.ToImmutable(), _captureNumberToSpan.ToImmutable());
            }

            private void CollectCaptures(RegexNode node, RegexOptions options)
            {
                try
                {
                    _recursionDepth++;
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    CollectCapturesWorker(node, options);
                }
                finally
                {
                    _recursionDepth--;
                }
            }

            private void CollectCapturesWorker(RegexNode node, RegexOptions options)
            {
                switch (node.Kind)
                {
                    case RegexKind.CaptureGrouping:
                        var captureGrouping = (RegexCaptureGroupingNode)node;
                        RecordCapture(captureGrouping.CaptureToken, GetGroupingSpan(captureGrouping));
                        break;

                    case RegexKind.BalancingGrouping:
                        var balancingGroup = (RegexBalancingGroupingNode)node;
                        RecordCapture(balancingGroup.FirstCaptureToken, GetGroupingSpan(balancingGroup));
                        break;

                    case RegexKind.ConditionalExpressionGrouping:
                        // Explicitly recurse into conditionalGrouping.Grouping.  That grouping
                        // itself does not create a capture group, but nested groupings inside of it
                        // will.
                        var conditionalGrouping = (RegexConditionalExpressionGroupingNode)node;
                        RecurseIntoChildren(conditionalGrouping.Grouping, options);
                        CollectCaptures(conditionalGrouping.Result, options);
                        return;

                    case RegexKind.SimpleGrouping:
                        RecordSimpleGroupingCapture((RegexSimpleGroupingNode)node, options);
                        break;

                    case RegexKind.NestedOptionsGrouping:
                        // When we see (?opts:...)
                        // Recurse explicitly, setting the new options as we process the inner expression.
                        // When this pops out we'll be back to these options we're currently at now.
                        var nestedOptions = (RegexNestedOptionsGroupingNode)node;
                        CollectCaptures(nestedOptions.Expression, GetNewOptionsFromToken(options, nestedOptions.OptionsToken));
                        return;
                }

                RecurseIntoChildren(node, options);
            }

            private void RecurseIntoChildren(RegexNode node, RegexOptions options)
            {
                foreach (var child in node)
                {
                    if (child.IsNode)
                    {
                        // When we see a SimpleOptionsGroup ```(?opts)``` then determine what the options will
                        // be for successive nodes in the sequence.
                        var childNode = child.Node;
                        if (childNode is RegexSimpleOptionsGroupingNode simpleOptions)
                        {
                            options = GetNewOptionsFromToken(options, simpleOptions.OptionsToken);
                        }

                        CollectCaptures(child.Node, options);
                    }
                }
            }

            private readonly TextSpan GetGroupingSpan(RegexGroupingNode grouping)
            {
                Debug.Assert(!grouping.OpenParenToken.IsMissing);
                var lastChar = grouping.CloseParenToken.IsMissing
                    ? _text.Last()
                    : grouping.CloseParenToken.VirtualChars.Last();

                return GetSpan(grouping.OpenParenToken.VirtualChars[0], lastChar);
            }

            private void RecordSimpleGroupingCapture(RegexSimpleGroupingNode node, RegexOptions options)
            {
                if (HasOption(options, RegexOptions.ExplicitCapture))
                {
                    // Don't automatically add simple groups if the explicit capture option is on.
                    // Only add captures for 'CaptureGrouping' and 'BalancingGrouping' nodes.
                    return;
                }

                // Don't count a bogus (? node as a capture node.  We only have this to keep our error
                // messages in line with the native parser.  i.e. even though the bogus (? code would 
                // cause an exception, we might get an earlier exception if there's a reference to
                // this grouping.  So if we note this grouping we'll end up not causing that error
                // to happen, bringing out behavior out of sync with the native system.
                var expr = node.Expression;
                if (expr is RegexAlternationNode alternation)
                    expr = alternation.SequenceList[0];

                if (expr is RegexSequenceNode sequence &&
                    sequence.ChildCount > 0)
                {
                    var leftMost = sequence.ChildAt(0);
                    if (leftMost.Node is RegexTextNode textNode &&
                        IsTextChar(textNode.TextToken, '?'))
                    {
                        return;
                    }
                }

                AddIfMissing(_captureNumberToSpan, list: null, _autoNumber++, GetGroupingSpan(node));
            }

            private readonly void RecordCapture(RegexToken token, TextSpan span)
            {
                if (!token.IsMissing)
                {
                    if (token.Kind == RegexKind.NumberToken)
                    {
                        AddIfMissing(_captureNumberToSpan, list: null, (int)token.Value, span);
                    }
                    else
                    {
                        AddIfMissing(_captureNameToSpan, list: _captureNames, (string)token.Value, span);
                    }
                }
            }

            private static void AddIfMissing<T>(
                ImmutableDictionary<T, TextSpan>.Builder mapping,
                ArrayBuilder<T> list,
                T val, TextSpan span)
            {
                if (!mapping.ContainsKey(val))
                {
                    mapping.Add(val, span);
                    list?.Add(val);
                }
            }

            /// <summary>
            /// Give numbers to all named captures.  They will get successive <see
            /// cref="_autoNumber"/> values that have not already been handed out to existing
            /// numbered capture groups.
            /// </summary>
            private void AssignNumbersToCaptureNames()
            {
                foreach (var name in _captureNames)
                {
                    while (_captureNumberToSpan.ContainsKey(_autoNumber))
                    {
                        _autoNumber++;
                    }

                    _captureNumberToSpan.Add(_autoNumber, _captureNameToSpan[name]);
                    _autoNumber++;
                }
            }
        }
    }
}
