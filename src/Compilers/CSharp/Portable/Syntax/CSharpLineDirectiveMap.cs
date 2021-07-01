// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// Adds C# specific parts to the line directive map.
    /// </summary>
    internal class CSharpLineDirectiveMap : LineDirectiveMap<DirectiveTriviaSyntax>
    {
        public CSharpLineDirectiveMap(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }

        // Add all active #line directives under trivia into the list, in source code order.
        protected override bool ShouldAddDirective(DirectiveTriviaSyntax directive)
        {
            return directive.IsActive && (directive.Kind() is SyntaxKind.LineDirectiveTrivia or SyntaxKind.LineSpanDirectiveTrivia);
        }

        // Given a directive and the previous entry, create a new entry.
        protected override LineMappingEntry GetEntry(DirectiveTriviaSyntax directiveNode, SourceText sourceText, LineMappingEntry previous)
        {
            Debug.Assert(ShouldAddDirective(directiveNode));
            var directive = (LineOrSpanDirectiveTriviaSyntax)directiveNode;

            // Get line number of NEXT line, hence the +1.
            var directiveLineNumber = sourceText.Lines.IndexOf(directive.SpanStart) + 1;

            // The default for the current entry does the same thing as the previous entry, except
            // resetting hidden.
            var unmappedLine = directiveLineNumber;

            // Modify the current entry based on the directive.
            switch (directive)
            {
                case LineDirectiveTriviaSyntax lineDirective:
                    {
                        var mappedLine = (previous.State == PositionState.RemappedSpan) ?
                            unmappedLine :
                            previous.MappedLine + directiveLineNumber - previous.UnmappedLine;
                        var mappedPathOpt = previous.MappedPathOpt;
                        PositionState state = PositionState.Unmapped;
                        SyntaxToken lineToken = lineDirective.Line;

                        if (!lineToken.IsMissing)
                        {
                            switch (lineToken.Kind())
                            {
                                case SyntaxKind.HiddenKeyword:
                                    state = PositionState.Hidden;
                                    break;

                                case SyntaxKind.DefaultKeyword:
                                    mappedLine = unmappedLine;
                                    mappedPathOpt = null;
                                    state = PositionState.Unmapped;
                                    break;

                                case SyntaxKind.NumericLiteralToken:
                                    // skip both the mapped line and the filename if the line number is not valid
                                    if (!lineToken.ContainsDiagnostics)
                                    {
                                        tryGetOneBasedNumericLiteralValue(lineToken, ref mappedLine);
                                        tryGetStringLiteralValue(directive.File, ref mappedPathOpt);
                                        return new LineMappingEntry(unmappedLine, mappedLine, mappedPathOpt, PositionState.Remapped);
                                    }
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(lineToken);
                            }
                        }

                        return new LineMappingEntry(unmappedLine, mappedLine, mappedPathOpt, state);
                    }

                case LineSpanDirectiveTriviaSyntax spanDirective:
                    if (!spanDirective.HasErrors)
                    {
                        LinePosition mappedStart = default;
                        LinePosition mappedEnd = default;
                        int? characterOffset = null;
                        string? mappedPathOpt = null;

                        if (tryGetPosition(spanDirective.Start, isEnd: false, ref mappedStart) &&
                            tryGetPosition(spanDirective.End, isEnd: true, ref mappedEnd) &&
                            tryGetOptionalCharacterOffset(spanDirective.CharacterOffset, ref characterOffset) &&
                            tryGetStringLiteralValue(spanDirective.File, ref mappedPathOpt))
                        {
                            return new LineMappingEntry(unmappedLine, new LinePositionSpan(mappedStart, mappedEnd), characterOffset, mappedPathOpt);
                        }
                    }

                    return new LineMappingEntry(unmappedLine, unmappedLine, mappedPathOpt: null, PositionState.Unmapped);

                default:
                    throw ExceptionUtilities.UnexpectedValue(directive);
            }

            static bool tryGetOneBasedNumericLiteralValue(in SyntaxToken token, ref int value)
            {
                if (!token.IsMissing &&
                    token.Kind() == SyntaxKind.NumericLiteralToken &&
                    token.Value is int tokenValue)
                {
                    // convert one-based line number into zero-based line number
                    value = tokenValue - 1;
                    return true;
                }
                return false;
            }

            static bool tryGetStringLiteralValue(in SyntaxToken token, ref string? value)
            {
                if (token.Kind() == SyntaxKind.StringLiteralToken)
                {
                    value = (string?)token.Value;
                    return true;
                }
                return false;
            }

            // returns false on error
            static bool tryGetOptionalCharacterOffset(in SyntaxToken token, ref int? value)
            {
                if (!token.IsMissing)
                {
                    if (token.Kind() == SyntaxKind.None)
                    {
                        value = null;
                        return true;
                    }
                    int val = 0;
                    if (tryGetOneBasedNumericLiteralValue(token, ref val))
                    {
                        value = val;
                        return true;
                    }
                }
                return false;
            }

            // returns false on error
            static bool tryGetPosition(LineDirectivePositionSyntax syntax, bool isEnd, ref LinePosition position)
            {
                int line = 0;
                int character = 0;
                if (tryGetOneBasedNumericLiteralValue(syntax.Line, ref line) &&
                    tryGetOneBasedNumericLiteralValue(syntax.Character, ref character))
                {
                    position = new LinePosition(line, isEnd ? character + 1 : character);
                    return true;
                }
                return false;
            }
        }

        protected override LineMappingEntry InitializeFirstEntry()
        {
            // The first entry of the map is always 0,0,null,Unmapped -- the default mapping.
            return new LineMappingEntry(0, 0, null, PositionState.Unmapped);
        }

        public override LineVisibility GetLineVisibility(SourceText sourceText, int position)
        {
            var unmappedPos = sourceText.Lines.GetLinePosition(position);

            // if there's only one entry (which is created as default for each file), all lines
            // are treated as being visible
            if (Entries.Length == 1)
            {
                Debug.Assert(Entries[0].State == PositionState.Unmapped);
                return LineVisibility.Visible;
            }

            var index = FindEntryIndex(unmappedPos.Line);
            var entry = Entries[index];

            // the state should not be set to the ones used for VB only.
            Debug.Assert(entry.State != PositionState.Unknown &&
                         entry.State != PositionState.RemappedAfterHidden &&
                         entry.State != PositionState.RemappedAfterUnknown);

            switch (entry.State)
            {
                case PositionState.Unmapped:
                    if (index == 0)
                    {
                        return LineVisibility.BeforeFirstLineDirective;
                    }
                    else
                    {
                        return LineVisibility.Visible;
                    }

                case PositionState.Remapped:
                    return LineVisibility.Visible;

                case PositionState.Hidden:
                    return LineVisibility.Hidden;

                default:
                    throw ExceptionUtilities.UnexpectedValue(entry.State);
            }
        }

        // C# does not have unknown visibility state
        protected override LineVisibility GetUnknownStateVisibility(int index)
            => throw ExceptionUtilities.Unreachable;

        internal override FileLinePositionSpan TranslateSpanAndVisibility(SourceText sourceText, string treeFilePath, TextSpan span, out bool isHiddenPosition)
        {
            var lines = sourceText.Lines;
            var unmappedStartPos = lines.GetLinePosition(span.Start);
            var unmappedEndPos = lines.GetLinePosition(span.End);

            // most common case is where we have only one mapping entry.
            if (this.Entries.Length == 1)
            {
                Debug.Assert(this.Entries[0].State == PositionState.Unmapped);
                Debug.Assert(this.Entries[0].MappedLine == this.Entries[0].UnmappedLine);
                Debug.Assert(this.Entries[0].MappedLine == 0);
                Debug.Assert(this.Entries[0].MappedPathOpt == null);

                isHiddenPosition = false;
                return new FileLinePositionSpan(treeFilePath, unmappedStartPos, unmappedEndPos);
            }

            var entry = FindEntry(unmappedStartPos.Line);

            // the state should not be set to the ones used for VB only.
            Debug.Assert(entry.State != PositionState.Unknown &&
                            entry.State != PositionState.RemappedAfterHidden &&
                            entry.State != PositionState.RemappedAfterUnknown);

            isHiddenPosition = entry.State == PositionState.Hidden;

            return TranslateSpan(entry, treeFilePath, unmappedStartPos, unmappedEndPos);
        }
    }
}
