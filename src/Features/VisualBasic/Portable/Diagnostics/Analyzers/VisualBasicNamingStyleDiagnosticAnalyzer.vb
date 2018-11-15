' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicNamingStyleDiagnosticAnalyzer
        Inherits NamingStyleDiagnosticAnalyzerBase(Of SyntaxKind)

        Protected Overrides ReadOnly Property SupportedSyntaxKinds As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.ModifiedIdentifier,
                SyntaxKind.CatchStatement,
                SyntaxKind.Parameter,
                SyntaxKind.TypeParameter)
    End Class
End Namespace
