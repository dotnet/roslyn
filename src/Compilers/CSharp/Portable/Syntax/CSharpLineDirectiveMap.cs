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
            return directive.IsActive && directive.Kind() == SyntaxKind.LineDirectiveTrivia;
        }

        // Given a directive and the previous entry, create a new entry.
        protected override LineMappingEntry GetEntry(DirectiveTriviaSyntax directiveNode, SourceText sourceText, LineMappingEntry previous)
        {
            Debug.Assert(ShouldAddDirective(directiveNode));
            var directive = (LineDirectiveTriviaSyntax)directiveNode;

            // Get line number of NEXT line, hence the +1.
            var directiveLineNumber = sourceText.Lines.IndexOf(directive.SpanStart) + 1;

            // The default for the current entry does the same thing as the previous entry, except
            // resetting hidden.
            var unmappedLine = directiveLineNumber;
            var mappedLine = previous.MappedLine + directiveLineNumber - previous.UnmappedLine;
            var mappedPathOpt = previous.MappedPathOpt;
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
