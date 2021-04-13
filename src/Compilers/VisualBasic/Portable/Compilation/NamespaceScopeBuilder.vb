' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class NamespaceScopeBuilder
        Private Sub New()
        End Sub

        Public Shared Function BuildNamespaceScope(
            moduleBuilder As Emit.PEModuleBuilder,
            xmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition),
            aliasImportsOpt As IEnumerable(Of AliasAndImportsClausePosition),
            memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            diagnostics As DiagnosticBag
        ) As ImmutableArray(Of Cci.UsedNamespaceOrType)
            Dim scopeBuilder = ArrayBuilder(Of Cci.UsedNamespaceOrType).GetInstance

            ' first come xml imports
            If xmlNamespacesOpt IsNot Nothing Then
                For Each xmlImport In xmlNamespacesOpt
                    scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateXmlNamespace(xmlImport.Key, xmlImport.Value.XmlNamespace))
                Next
            End If

            ' then come alias imports
            If aliasImportsOpt IsNot Nothing Then
                For Each aliasImport In aliasImportsOpt
                    Dim target = aliasImport.Alias.Target
                    If target.IsNamespace Then
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateNamespace(DirectCast(target, NamespaceSymbol).GetCciAdapter(), aliasOpt:=aliasImport.Alias.Name))
                    ElseIf target.Kind <> SymbolKind.ErrorType AndAlso Not target.ContainingAssembly.IsLinked Then
                        ' It is not an error to import a non-existing type (unlike C#), skip the error types.
                        ' We also skip alias imports of embedded types to avoid breaking existing code that
                        ' imports types that can't be embedded but doesn't use them anywhere else in the code.
                        Dim typeRef = GetTypeReference(DirectCast(target, NamedTypeSymbol), moduleBuilder, diagnostics)
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateType(typeRef, aliasOpt:=aliasImport.Alias.Name))
                    End If
                Next
            End If

            ' then come the imports
            For Each import In memberImports
                Dim target = import.NamespaceOrType

                ' Imports with erroneous targets are skipped during binding.
                Debug.Assert(target.Kind <> SymbolKind.ErrorType)

                If target.IsNamespace Then
                    scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateNamespace(DirectCast(target, NamespaceSymbol).GetCciAdapter()))
                ElseIf Not target.ContainingAssembly.IsLinked Then
                    ' We skip imports of embedded types to avoid breaking existing code that
                    ' imports types that can't be embedded but doesn't use them anywhere else in the code.
                    Dim typeRef = GetTypeReference(DirectCast(target, NamedTypeSymbol), moduleBuilder, diagnostics)
                    scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateType(typeRef))
                End If
            Next

            Return scopeBuilder.ToImmutableAndFree()
        End Function

        Private Shared Function GetTypeReference(type As TypeSymbol, moduleBuilder As CommonPEModuleBuilder, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Return moduleBuilder.Translate(type, Nothing, diagnostics)
        End Function
    End Class
End Namespace
