' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDiagnosticAnalyzerFieldsAnalyzer
        Inherits DiagnosticAnalyzerFieldsAnalyzer(Of ClassBlockSyntax, StructureBlockSyntax, FieldDeclarationSyntax, TypeSyntax, SimpleAsClauseSyntax, TypeArgumentListSyntax, GenericNameSyntax)
    End Class
End Namespace

