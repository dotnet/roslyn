' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AmbiguityCodeFixProvider
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AliasType), [Shared]>
<ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
Friend Class VisualBasicAmbiguousTypeCodeFixProvider
    Inherits AbstractAmbiguousTypeCodeFixProvider

    'BC30561: '<name1>' is ambiguous, imported from the namespaces or types '<name2>'
    Private Const BC30561 As String = NameOf(BC30561)

    Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
        Get
            Return ImmutableArray.Create(BC30561)
        End Get
    End Property

    Protected Overrides Function GetAliasDirective(ByVal typeName As String, ByVal symbol As ISymbol) As SyntaxNode
        Return SyntaxFactory.ImportsStatement(SyntaxFactory.SeparatedList(Of ImportsClauseSyntax).Add(
                                              SyntaxFactory.SimpleImportsClause(
                                              SyntaxFactory.ImportAliasClause(typeName),
                                              SyntaxFactory.IdentifierName(symbol.ToNameDisplayString()))))
    End Function
End Class
