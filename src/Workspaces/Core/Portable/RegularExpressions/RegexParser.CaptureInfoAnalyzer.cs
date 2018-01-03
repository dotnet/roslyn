// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    using static RegexHelpers;

    internal partial struct RegexParser
    {
        private struct CaptureInfoAnalyzer
        {
            private readonly ImmutableArray<VirtualChar> _text;
            private readonly ImmutableDictionary<int, TextSpan>.Builder _captureNumberToSpan;
            private readonly ImmutableDictionary<string, TextSpan>.Builder _captureNameToSpan;
            private readonly ArrayBuilder<string> _captureNames;
            private int _autoNumber;

            public CaptureInfoAnalyzer(ImmutableArray<VirtualChar> text)
            {
                _text = text;
                _captureNumberToSpan = ImmutableDictionary.CreateBuilder<int, TextSpan>();
                _captureNameToSpan = ImmutableDictionary.CreateBuilder<string, TextSpan>();
                _captureNames = ArrayBuilder<string>.GetInstance();
                _autoNumber = 1;

                _captureNumberToSpan.Add(0, text.Length == 0 ? default : GetSpan(text[0], text.Last()));
            }

            public (ImmutableDictionary<string, TextSpan>, ImmutableDictionary<int, TextSpan>) Analyze(
                RegexCompilationUnit root, RegexOptions options)
            {
                CollectCaptures(root, options);
                AssignNumbersToCaptureNames();

                _captureNames.Free();
                return (_captureNameToSpan.ToImmutable(), _captureNumberToSpan.ToImmutable());
            }


            private void CollectCaptures(RegexNode node, RegexOptions options)
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

                    case RegexKind.SimpleGrouping:
                        RecordSimpleGroupingCapture((RegexSimpleGroupingNode)node, options);
                        break;

                    case RegexKind.NestedOptionsGrouping:
                        var nestedOptions = (RegexNestedOptionsGroupingNode)node;
                        CollectCaptures(nestedOptions.Expression, GetNewOptionsFromToken(options, nestedOptions.OptionsToken));
                        return;
                }

                for (int i = 0, n = node.ChildCount; i < n; i++)
                {
                    var child = node.ChildAt(i);
                    if (child.IsNode)
                    {
                        var childNode = child.Node;
                        if (childNode is RegexSimpleOptionsGroupingNode simpleOptions)
                        {
                            options = GetNewOptionsFromToken(options, simpleOptions.OptionsToken);
                        }

                        CollectCaptures(child.Node, options);
                    }
                }
            }

            private TextSpan GetGroupingSpan(RegexGroupingNode grouping)
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
                    // Don't automatically add simply groups if the explicit capture option is on.
                    return;
                }

                // Don't count a bogus (? node as a capture node.
                var expr = node.Expression;
                while (expr is RegexAlternationNode alternation)
                {
                    expr = alternation.Left;
                }

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

            private void RecordCapture(RegexToken token, TextSpan span)
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

            private  void AssignNumbersToCaptureNames()
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
