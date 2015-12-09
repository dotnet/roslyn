// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticAnalysisContextHelpers
    {
        internal static void VerifyArguments<TContext>(Action<TContext> action)
        {
            VerifyAction(action);
        }

        internal static void VerifyArguments<TContext>(Action<TContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            VerifyAction(action);
            VerifySymbolKinds(symbolKinds);
        }

        internal static void VerifyArguments<TContext, TLanguageKindEnum>(Action<TContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            VerifyAction(action);
            VerifySyntaxKinds(syntaxKinds);
        }

        internal static void VerifyArguments(Diagnostic diagnostic, Compilation compilationOpt, Func<Diagnostic, bool> isSupportedDiagnostic)
        {
            if (diagnostic is DiagnosticWithInfo)
            {
                // Compiler diagnostic, skip validations.
                return;
            }

            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (compilationOpt != null)
            {
                VerifyDiagnosticLocationsInCompilation(diagnostic, compilationOpt);
            }

            if (!isSupportedDiagnostic(diagnostic))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.UnsupportedDiagnosticReported, diagnostic.Id), nameof(diagnostic));
            }

            if (!UnicodeCharacterUtilities.IsValidIdentifier(diagnostic.Id))
            {
                // Disallow invalid diagnostic IDs.
                // Note that the parsing logic in Csc/Vbc MSBuild tasks to decode command line compiler output relies on diagnostics having a valid ID.
                // See https://github.com/dotnet/roslyn/issues/4376 for details.
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticIdReported, diagnostic.Id), nameof(diagnostic));
            } 
        }

        internal static void VerifyDiagnosticLocationsInCompilation(Diagnostic diagnostic, Compilation compilation)
        {
            VerifyDiagnosticLocationInCompilation(diagnostic.Location, compilation);

            if (diagnostic.AdditionalLocations != null)
            {
                foreach (var location in diagnostic.AdditionalLocations)
                {
                    VerifyDiagnosticLocationInCompilation(location, compilation);
                }
            }
        }

        private static void VerifyDiagnosticLocationInCompilation(Location location, Compilation compilation)
        {
            if (location.IsInSource && !compilation.ContainsSyntaxTree(location.SourceTree))
            {
                // Disallow diagnostics with source locations outside this compilation.
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticLocationReported, location.SourceTree.FilePath), "diagnostic");
            }
        }

        private static void VerifyAction<TContext>(Action<TContext> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
        }

        private static void VerifySymbolKinds(ImmutableArray<SymbolKind> symbolKinds)
        {
            if (symbolKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(symbolKinds));
            }

            if (symbolKinds.IsEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(symbolKinds));
            }
        }

        private static void VerifySyntaxKinds<TLanguageKindEnum>(ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            if (syntaxKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(syntaxKinds));
            }

            if (syntaxKinds.IsEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(syntaxKinds));
            }
        }
    }
}
