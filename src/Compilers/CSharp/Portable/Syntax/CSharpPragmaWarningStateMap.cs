// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class CSharpPragmaWarningStateMap : AbstractWarningStateMap
    {
        public CSharpPragmaWarningStateMap(SyntaxTree syntaxTree) :
            base(syntaxTree)
        {
        }

        protected override WarningStateMapEntry[] CreateWarningStateMapEntries(SyntaxTree syntaxTree)
        {
            // Accumulate all the pragma warning directives, in source code order
            var directives = ArrayBuilder<PragmaWarningDirectiveTriviaSyntax>.GetInstance();
            GetAllPragmaWarningDirectives(syntaxTree, directives);

            // Create the pragma warning map.
            return CreatePragmaWarningStateEntries(directives.ToImmutableAndFree());
        }

        // Add all active #pragma warn directives under trivia into the list, in source code order.
        private static void GetAllPragmaWarningDirectives(SyntaxTree syntaxTree, ArrayBuilder<PragmaWarningDirectiveTriviaSyntax> directiveList)
        {
            foreach (var d in syntaxTree.GetRoot().GetDirectives())
            {
                if (d.Kind() == SyntaxKind.PragmaWarningDirectiveTrivia)
                {
                    var w = (PragmaWarningDirectiveTriviaSyntax)d;

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
                var directiveState = currentDirective.DisableOrRestoreKeyword.Kind() == SyntaxKind.DisableKeyword ? ReportDiagnostic.Suppress : ReportDiagnostic.Default;

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
                        var currentErrorCode = currentDirective.ErrorCodes[x];
                        if (currentErrorCode.IsMissing || currentErrorCode.ContainsDiagnostics)
                            continue;

                        var errorId = string.Empty;
                        if (currentErrorCode.Kind() == SyntaxKind.NumericLiteralExpression)
                        {
                            var token = ((LiteralExpressionSyntax)currentErrorCode).Token;
                            errorId = MessageProvider.Instance.GetIdForErrorCode((int)token.Value);
                        }
                        else if (currentErrorCode.Kind() == SyntaxKind.IdentifierName)
                        {
                            errorId = ((IdentifierNameSyntax)currentErrorCode).Identifier.ValueText;
                        }

                        if (!string.IsNullOrWhiteSpace(errorId))
                        {
                            // Update the state of this error code with the current directive state
                            accumulatedSpecificWarningState = accumulatedSpecificWarningState.SetItem(errorId, directiveState);
                        }
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
