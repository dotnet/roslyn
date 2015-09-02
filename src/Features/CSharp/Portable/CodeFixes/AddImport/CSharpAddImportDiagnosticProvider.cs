// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.AddImport;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddImport
{
    [ExportDiagnosticProvider(PredefinedDiagnosticProviderNames.AddUsingOrImport, LanguageNames.CSharp)]
    internal sealed class CSharpAddImportDiagnosticProvider : AbstractAddImportDiagnosticProvider<SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax, BlockSyntax, EqualsValueClauseSyntax>
    {
    }
}
