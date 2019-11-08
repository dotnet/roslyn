// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// Describes how to report a warning diagnostic.
    /// </summary>
    internal enum PragmaWarningState : byte
    {
        /// <summary>
        /// Report a diagnostic by default.
        /// Either there is no corresponding #pragma, or the action is "restore".
        /// </summary>
        Default = 0,

        /// <summary>
        /// Diagnostic is enabled.
        /// NOTE: this may be removed as part of https://github.com/dotnet/roslyn/issues/36550
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Diagnostic is disabled.
        /// </summary>
        Disabled = 2,
    }

    internal class CSharpPragmaWarningStateMap : AbstractWarningStateMap<PragmaWarningState>
    {
        public CSharpPragmaWarningStateMap(SyntaxTree syntaxTree, bool isGeneratedCode) :
            base(syntaxTree, isGeneratedCode)
        {
        }

        protected override WarningStateMapEntry[] CreateWarningStateMapEntries(SyntaxTree syntaxTree)
        {
            // Accumulate all the pragma warning directives, in source code order
            var directives = ArrayBuilder<DirectiveTriviaSyntax>.GetInstance();
            GetAllPragmaWarningDirectives(syntaxTree, directives);

            // Create the pragma warning map.
            WarningStateMapEntry[] result = CreatePragmaWarningStateEntries(directives, _isGeneratedCode);
            directives.Free();

            return result;
        }

        // Add all active #pragma warn and #nullable directives under trivia into the list, in source code order.
        private static void GetAllPragmaWarningDirectives(SyntaxTree syntaxTree, ArrayBuilder<DirectiveTriviaSyntax> directiveList)
        {
            foreach (var d in syntaxTree.GetRoot().GetDirectives())
            {
                if (!d.IsActive || d.Kind() != SyntaxKind.PragmaWarningDirectiveTrivia)
                {
                    continue;
                }

                var w = (PragmaWarningDirectiveTriviaSyntax)d;

                // Ignore directives with errors (i.e., Unrecognized #pragma directive)
                if (w is { DisableOrRestoreKeyword: { IsMissing: false }, WarningKeyword: { IsMissing: false } })
                {
                    directiveList.Add(w);
                }
            }
        }

        // Given the ordered list of all pragma warning and nullable directives in the syntax tree, return a list of mapping entries, 
        // containing the cumulative set of warnings that are disabled for that point in the source.
        // This mapping also contains a global warning option, accumulated of all #pragma up to the current line position.
        private static WarningStateMapEntry[] CreatePragmaWarningStateEntries(ArrayBuilder<DirectiveTriviaSyntax> directiveList, bool isGeneratedCode)
        {
            var entries = new WarningStateMapEntry[directiveList.Count + 1];
            var index = 0;

            // Captures the mapping of a warning number to the reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedSpecificWarningState = ImmutableDictionary.Create<string, PragmaWarningState>();

            // Captures the general reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedGeneralWarningState = PragmaWarningState.Default;

            var current = new WarningStateMapEntry(0, PragmaWarningState.Default, accumulatedSpecificWarningState);
            entries[index] = current;

            while (index < directiveList.Count)
            {
                var currentDirective = directiveList[index];
                var currentPragmaDirective = (PragmaWarningDirectiveTriviaSyntax)currentDirective;

                // Compute the directive state
                PragmaWarningState directiveState = currentPragmaDirective.DisableOrRestoreKeyword.Kind() switch
                {
                    SyntaxKind.DisableKeyword => PragmaWarningState.Disabled,
                    SyntaxKind.RestoreKeyword => PragmaWarningState.Default,
                    SyntaxKind.EnableKeyword => PragmaWarningState.Enabled,
                    var kind => throw ExceptionUtilities.UnexpectedValue(kind)
                };

                // Check if this directive applies for all (e.g., #pragma warning disable)
                if (currentPragmaDirective.ErrorCodes.Count == 0)
                {
                    // Update the warning state and reset the specific one
                    accumulatedGeneralWarningState = directiveState;
                    accumulatedSpecificWarningState = ImmutableDictionary.Create<string, PragmaWarningState>();
                }
                else
                {
                    // Compute warning numbers from the current directive's codes
                    for (int x = 0; x < currentPragmaDirective.ErrorCodes.Count; x++)
                    {
                        var currentErrorCode = currentPragmaDirective.ErrorCodes[x];
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
