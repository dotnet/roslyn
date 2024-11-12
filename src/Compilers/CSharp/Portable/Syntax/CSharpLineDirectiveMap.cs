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

            // Get line number of NEXT line, hence the +1.
            var directiveLineNumber = sourceText.Lines.IndexOf(directiveNode.SpanStart) + 1;

            if (directiveNode is LineSpanDirectiveTriviaSyntax spanDirective)
            {
                return GetLineSpanDirectiveEntry(spanDirective, directiveLineNumber);
            }

            var directive = (LineDirectiveTriviaSyntax)directiveNode;

            // The default for the current entry does the same thing as the previous entry, except
            // resetting hidden.
            var unmappedLine = directiveLineNumber;
            var mappedLine = (previous.State == PositionState.RemappedSpan) ? unmappedLine : previous.MappedLine + directiveLineNumber - previous.UnmappedLine;
            var mappedPathOpt = (previous.State == PositionState.RemappedSpan) ? null : previous.MappedPathOpt;
            PositionState state = PositionState.Unmapped;

            // Modify the current entry based on the directive.
            SyntaxToken lineToken = directive.Line;

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
                            object? value = lineToken.Value;
                            if (value is int)
                            {
                                // convert one-based line number into zero-based line number
                                mappedLine = ((int)value) - 1;
                            }

                            if (directive.File.Kind() == SyntaxKind.StringLiteralToken)
                            {
                                mappedPathOpt = (string?)directive.File.Value;
                            }

                            state = PositionState.Remapped;
                        }

                        break;
                }
            }

            return new LineMappingEntry(
                unmappedLine,
                mappedLine,
                mappedPathOpt,
                state);
        }

        private static LineMappingEntry GetLineSpanDirectiveEntry(LineSpanDirectiveTriviaSyntax spanDirective, int unmappedLine)
        {
            if (!spanDirective.HasErrors &&
                tryGetPosition(spanDirective.Start, isEnd: false, out LinePosition mappedStart) &&
                tryGetPosition(spanDirective.End, isEnd: true, out LinePosition mappedEnd) &&
                tryGetOptionalCharacterOffset(spanDirective.CharacterOffset, out int? characterOffset) &&
                tryGetStringLiteralValue(spanDirective.File, out string? mappedPathOpt))
            {
                return new LineMappingEntry(unmappedLine, new LinePositionSpan(mappedStart, mappedEnd), characterOffset, mappedPathOpt);
            }
            return new LineMappingEntry(unmappedLine, unmappedLine, mappedPathOpt: null, PositionState.Unmapped);

            static bool tryGetNumericLiteralValue(in SyntaxToken token, out int value, bool oneBased)
            {
                if (!token.IsMissing &&
                    token.Kind() == SyntaxKind.NumericLiteralToken &&
                    token.Value is int tokenValue)
                {
                    value = tokenValue;

                    // convert one-based line number into zero-based line number
                    if (oneBased)
                    {
                        value--;
                    }

                    return true;
                }
                value = 0;
                return false;
            }

            static bool tryGetStringLiteralValue(in SyntaxToken token, out string? value)
            {
                if (token.Kind() == SyntaxKind.StringLiteralToken)
                {
                    value = (string?)token.Value;
                    return true;
                }
                value = null;
                return false;
            }

            // returns false on error
            static bool tryGetOptionalCharacterOffset(in SyntaxToken token, out int? value)
            {
                if (!token.IsMissing)
                {
                    if (token.Kind() == SyntaxKind.None)
                    {
                        value = null;
                        return true;
                    }
                    int val = 0;
                    if (tryGetNumericLiteralValue(token, out val, oneBased: false))
                    {
                        value = val;
                        return true;
                    }
                }
                value = null;
                return false;
            }

            // returns false on error
            static bool tryGetPosition(LineDirectivePositionSyntax syntax, bool isEnd, out LinePosition position)
            {
                if (tryGetNumericLiteralValue(syntax.Line, out int line, oneBased: true) &&
                    tryGetNumericLiteralValue(syntax.Character, out int character, oneBased: true))
                {
                    position = new LinePosition(line, isEnd ? character + 1 : character);
                    return true;
                }
                position = default;
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
                case PositionState.RemappedSpan:
                    return LineVisibility.Visible;

                case PositionState.Hidden:
                    return LineVisibility.Hidden;

                default:
                    throw ExceptionUtilities.UnexpectedValue(entry.State);
            }
        }

        // C# does not have unknown visibility state
        protected override LineVisibility GetUnknownStateVisibility(int index)
            => throw ExceptionUtilities.Unreachable();

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
