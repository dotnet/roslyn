' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDiagnosticAnalyzerFieldsAnalyzer
        Inherits DiagnosticAnalyzerFieldsAnalyzer(Of ClassBlockSyntax, StructureBlockSyntax, FieldDeclarationSyntax, TypeSyntax, SimpleAsClauseSyntax)

        Protected Overrides Function IsContainedInFuncOrAction(typeSyntax As TypeSyntax, model As SemanticModel, funcs As ImmutableArray(Of INamedTypeSymbol), actions As ImmutableArray(Of INamedTypeSymbol)) As Boolean
            Dim current = typeSyntax.Parent
            Dim typeArgumentList = TryCast(current, TypeArgumentListSyntax)
            Dim genericName = TryCast(current, GenericNameSyntax)
            While Not typeArgumentList Is Nothing AndAlso Not genericName Is Nothing
                If Not genericName Is Nothing
                    Dim currentSymbol = TryCast(model.GetSymbolInfo(current).Symbol, INamedTypeSymbol)
                    If Not currentSymbol Is Nothing _
                        AndAlso (funcs.Contains(currentSymbol.OriginalDefinition, SymbolEqualityComparer.Default) Or actions.Contains(currentSymbol.OriginalDefinition, SymbolEqualityComparer.Default))
                        Return True
                    End If
                End If
                
                current = current.Parent
            End While
            
            Return False
        End Function
    End Class
End Namespace

