// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.AddMissingImports;

[ExportLanguageService(typeof(IAddMissingImportsFeatureService), LanguageNames.CSharp), Shared]
internal class CSharpAddMissingImportsFeatureService : AbstractAddMissingImportsFeatureService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAddMissingImportsFeatureService()
    {
    }

    protected sealed override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;

    protected override ImmutableArray<AbstractFormattingRule> GetFormatRules(SourceText text)
        => [new CleanUpNewLinesFormatter(text), new IndentBlockFormattingRule()];
}
