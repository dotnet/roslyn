// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;

internal abstract partial class AbstractCodeCleanUpFixer
{
    protected internal const string FormatDocumentFixId = nameof(FormatDocumentFixId);
    protected internal const string RemoveUnusedImportsFixId = nameof(RemoveUnusedImportsFixId);
    protected internal const string SortImportsFixId = nameof(SortImportsFixId);
    protected internal const string ApplyThirdPartyFixersId = nameof(ApplyThirdPartyFixersId);
    protected internal const string ApplyAllAnalyzerFixersId = nameof(ApplyAllAnalyzerFixersId);

    internal static EnabledDiagnosticOptions AdjustDiagnosticOptions(EnabledDiagnosticOptions enabledDiagnostics, Func<string, bool> isFixIdEnabled)
    {
        if (!isFixIdEnabled(ApplyAllAnalyzerFixersId))
        {
            var enabledDiagnosticSets = ArrayBuilder<DiagnosticSet>.GetInstance();

            foreach (var diagnostic in enabledDiagnostics.Diagnostics)
            {
                foreach (var diagnosticId in diagnostic.DiagnosticIds)
                {
                    if (isFixIdEnabled(diagnosticId))
                    {
                        enabledDiagnosticSets.Add(diagnostic);
                        break;
                    }
                }
            }

            var isFormatDocumentEnabled = isFixIdEnabled(FormatDocumentFixId);
            var isRemoveUnusedUsingsEnabled = isFixIdEnabled(RemoveUnusedImportsFixId);
            var isSortUsingsEnabled = isFixIdEnabled(SortImportsFixId);
            var isApplyThirdPartyFixersEnabled = isFixIdEnabled(ApplyThirdPartyFixersId);
            return new EnabledDiagnosticOptions(
                isFormatDocumentEnabled,
                isApplyThirdPartyFixersEnabled,
                enabledDiagnosticSets.ToImmutableArray(),
                new OrganizeUsingsSet(isRemoveUnusedUsingsEnabled, isSortUsingsEnabled));
        }
        else
        {
            var enabledDiagnosticSets = ArrayBuilder<DiagnosticSet>.GetInstance();

            foreach (var diagnostic in enabledDiagnostics.Diagnostics)
            {
                var isAnyDiagnosticIdExplicitlyEnabled = false;
                foreach (var diagnosticId in diagnostic.DiagnosticIds)
                {
                    if (isFixIdEnabled(diagnosticId))
                    {
                        isAnyDiagnosticIdExplicitlyEnabled = true;
                        break;
                    }
                }

                enabledDiagnosticSets.Add(diagnostic.With(isAnyDiagnosticIdExplicitlyEnabled));
            }

            return new EnabledDiagnosticOptions(
                enabledDiagnostics.FormatDocument,
                enabledDiagnostics.RunThirdPartyFixers,
                enabledDiagnosticSets.ToImmutableArray(),
                enabledDiagnostics.OrganizeUsings);
        }
    }
}
