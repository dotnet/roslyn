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
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Diagnostic is disabled.
        /// </summary>
        Disabled = 2,
    }


    internal class CSharpPragmaWarningStateMap : AbstractWarningStateMap<PragmaWarningState>
    {
        public CSharpPragmaWarningStateMap(SyntaxTree syntaxTree) :
            base(syntaxTree)
        {
        }

        protected override WarningStateMapEntry[] CreateWarningStateMapEntries(SyntaxTree syntaxTree)
        {
            // Accumulate all the pragma warning directives, in source code order
            var directives = ArrayBuilder<DirectiveTriviaSyntax>.GetInstance();
            GetAllPragmaWarningDirectives(syntaxTree, directives);

            // Create the pragma warning map.
            WarningStateMapEntry[] result = CreatePragmaWarningStateEntries(directives);
            directives.Free();

            return result;
        }

        // Add all active #pragma warn and #nullable directives under trivia into the list, in source code order.
        private static void GetAllPragmaWarningDirectives(SyntaxTree syntaxTree, ArrayBuilder<DirectiveTriviaSyntax> directiveList)
        {
            foreach (var d in syntaxTree.GetRoot().GetDirectives())
            {
                // Ignore directives inside disabled code (by #if and #endif)
                if (!d.IsActive)
                {
                    continue;
                }

                switch (d.Kind())
                {
                    case SyntaxKind.PragmaWarningDirectiveTrivia:
                        var w = (PragmaWarningDirectiveTriviaSyntax)d;

                        // Ignore directives with errors (i.e., Unrecognized #pragma directive)
                        if (!w.DisableOrRestoreKeyword.IsMissing && !w.WarningKeyword.IsMissing)
                        {
                            directiveList.Add(w);
                        }
                        break;

                    case SyntaxKind.NullableDirectiveTrivia:
                        var nullableDirective = (NullableDirectiveTriviaSyntax)d;

                        // Ignore directives with errors (i.e., Unrecognized #nullable directive)
                        if (!nullableDirective.SettingToken.IsMissing)
                        {
                            directiveList.Add(nullableDirective);
                        }
                        break;
                }
            }
        }

        // Given the ordered list of all pragma warning and nullable directives in the syntax tree, return a list of mapping entries, 
        // containing the cumulative set of warnings that are disabled for that point in the source.
        // This mapping also contains a global warning option, accumulated of all #pragma up to the current line position.
        private static WarningStateMapEntry[] CreatePragmaWarningStateEntries(ArrayBuilder<DirectiveTriviaSyntax> directiveList)
        {
            var entries = new WarningStateMapEntry[directiveList.Count + 1];
            var current = new WarningStateMapEntry(0, PragmaWarningState.Default, null);
            var index = 0;
            entries[index] = current;

            // Captures the general reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedGeneralWarningState = PragmaWarningState.Default;

            // Captures the mapping of a warning number to the reporting option, accumulated of all #pragma up to the current directive.
            var accumulatedSpecificWarningState = ImmutableDictionary.Create<string, PragmaWarningState>();

            while (index < directiveList.Count)
            {
                var currentDirective = directiveList[index];

                if (currentDirective.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia))
                {
                    var currentPragmaDirective = (PragmaWarningDirectiveTriviaSyntax)currentDirective;

                    // Compute the directive state (either Disable or Restore)
                    var directiveState = currentPragmaDirective.DisableOrRestoreKeyword.Kind() == SyntaxKind.DisableKeyword ? PragmaWarningState.Disabled : PragmaWarningState.Default;

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
                }
                else
                {
                    var currentNullableDirective = (NullableDirectiveTriviaSyntax)currentDirective;
                    PragmaWarningState safetyState;
                    PragmaWarningState nonSafetyState;

                    switch (currentNullableDirective.SettingToken.Kind())
                    {
                        case SyntaxKind.DisableKeyword:
                            safetyState = PragmaWarningState.Disabled;
                            nonSafetyState = PragmaWarningState.Disabled;
                            break;

                        case SyntaxKind.EnableKeyword:
                            safetyState = PragmaWarningState.Enabled;
                            nonSafetyState = PragmaWarningState.Enabled;
                            break;

                        case SyntaxKind.SafeOnlyKeyword:
                            safetyState = PragmaWarningState.Enabled;
                            nonSafetyState = PragmaWarningState.Disabled;
                            break;

                        case SyntaxKind.RestoreKeyword:
                            safetyState = PragmaWarningState.Default;
                            nonSafetyState = PragmaWarningState.Default;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(currentNullableDirective.SettingToken.Kind());
                    }

                    var builder = ArrayBuilder<KeyValuePair<string, PragmaWarningState>>.GetInstance(ErrorFacts.NullableFlowAnalysisSafetyWarnings.Count + ErrorFacts.NullableFlowAnalysisNonSafetyWarnings.Count);
                    // Update the state of the error codes with the current directive state
                    addNewStates(safetyState, ErrorFacts.NullableFlowAnalysisSafetyWarnings);
                    addNewStates(nonSafetyState, ErrorFacts.NullableFlowAnalysisNonSafetyWarnings);

                    accumulatedSpecificWarningState = accumulatedSpecificWarningState.SetItems(builder);
                    builder.Free();

                    void addNewStates(PragmaWarningState directiveState, ImmutableHashSet<string> warnings)
                    {
                        foreach (string id in warnings)
                        {
                            builder.Add(new KeyValuePair<string, PragmaWarningState>(id, directiveState));
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
