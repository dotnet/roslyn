// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class CSharpPragmaWarningStateMap : AbstractWarningStateMap
    {
        private WarningStateMapEntry[] warningStateMapEntries;

        public CSharpPragmaWarningStateMap(SyntaxTree syntaxTree)
        {
            // Accumulate all the pragma warning directives, in source code order
            var directives = ArrayBuilder<PragmaWarningDirectiveTriviaSyntax>.GetInstance();
            GetAllPragmaWarningDirectives(syntaxTree, directives);

            // Create the pragma warning map.
            this.warningStateMapEntries = CreatePragmaWarningStateEntries(directives.ToImmutableAndFree());
        }

        public override ReportDiagnostic GetWarningState(string id, int position)
        {
            var entry = GetEntryAtOrBeforePosition(this.warningStateMapEntries, position);

            ReportDiagnostic report;
            if (entry.SpecificWarningOption.TryGetValue(id, out report))
            {
                return report;
            }

            return entry.GeneralWarningOption;
        }

        // Add all active #pragma warn directives under trivia into the list, in source code order.
        private static void GetAllPragmaWarningDirectives(SyntaxTree syntaxTree, ArrayBuilder<PragmaWarningDirectiveTriviaSyntax> directiveList)
        {
            foreach (var d in syntaxTree.GetRoot().GetDirectives())
            {
                if (d.Kind == SyntaxKind.PragmaWarningDirectiveTrivia)
                {
                    var w = d as PragmaWarningDirectiveTriviaSyntax;

                    // Ignore directives with errors (i.e., Unrecognized #pragma directive) and
                    // directives inside disabled code (by #if and #endif)
                    if (!w.DisableOrRestoreKeyword.IsMissing && !w.WarningKeyword.IsMissing && w.IsActive)
                        directiveList.Add(w);
                }
            }
        }

        // Given the ordered list of all pragma warning directives in the syntax tree, return a list of mapping entries, 
        // containing the cumulative set of warnings that are disabled for that point in the source.
        // This mapping also contains a global warning option, accumulated of all #pragma up to the current line position.
        private static WarningStateMapEntry[] CreatePragmaWarningStateEntries(ImmutableArray<PragmaWarningDirectiveTriviaSyntax> directiveList)
        {
            var entries = new WarningStateMapEntry[directiveList.Length + 1];
            var current = new WarningStateMapEntry(0, ReportDiagnostic.Default, null);
            var index = 0;
            entries[index] = current;

            // Captures the general reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedGeneralWarningState = ReportDiagnostic.Default;

            // Captures the mapping of a warning number to the reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedSpecificWarningState = ImmutableDictionary.Create<string, ReportDiagnostic>();

            while (index < directiveList.Length)
            {
                var currentDirective = directiveList[index];

                // Compute the directive state (either Disable or Restore)
                var directiveState = currentDirective.DisableOrRestoreKeyword.CSharpKind() == SyntaxKind.DisableKeyword ? ReportDiagnostic.Suppress : ReportDiagnostic.Default;

                // Check if this directive applies for all (e.g., #pragma warning disable)
                if (currentDirective.ErrorCodes.Count == 0)
                {
                    // Update the warning state and reset the specific one
                    accumulatedGeneralWarningState = directiveState;
                    accumulatedSpecificWarningState = ImmutableDictionary.Create<string, ReportDiagnostic>();
                }
                else
                {
                    // Compute warning numbers from the current directive's codes
                    for (int x = 0; x < currentDirective.ErrorCodes.Count; x++)
                    {
                        if (currentDirective.ErrorCodes[x].IsMissing || currentDirective.ErrorCodes[x].ContainsDiagnostics)
                            continue;

                        var token = ((LiteralExpressionSyntax)currentDirective.ErrorCodes[x]).Token;
                        string errorId = token.CSharpKind() == SyntaxKind.NumericLiteralToken ?
                            MessageProvider.Instance.GetIdForErrorCode((int)token.Value) :
                            (string)token.Value;

                        // Update the state of this error code with the current directive state
                        accumulatedSpecificWarningState = accumulatedSpecificWarningState.SetItem(errorId, directiveState);
                    }
                }

                current = new WarningStateMapEntry(currentDirective.Location.SourceSpan.End, accumulatedGeneralWarningState, accumulatedSpecificWarningState);
                ++index;
                entries[index] = current;
            }

#if DEBUG
            // Make sure the entries array is correctly sorted. 
            for (int i = 1; i < entries.Length - 1; ++i)
            {
                Debug.Assert(entries[i].CompareTo(entries[i + 1]) < 0);
            }
#endif

            return entries;
        }
    }
}
