' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDiagnosticAnalyzerFieldsAnalyzer
        Inherits DiagnosticAnalyzerFieldsAnalyzer(Of ClassBlockSyntax, StructureBlockSyntax, FieldDeclarationSyntax, TypeSyntax, SimpleAsClauseSyntax, TypeArgumentListSyntax, GenericNameSyntax)
    End Class
End Namespace

