' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class NamespaceScopeBuilder
        Private Sub New()
        End Sub

        Public Shared Function BuildNamespaceScope(
            moduleBuilder As Emit.PEModuleBuilder,
            xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
            aliasImports As IEnumerable(Of AliasAndImportsClausePosition),
            memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
            diagnostics As DiagnosticBag
        ) As ImmutableArray(Of Cci.UsedNamespaceOrType)
            Dim scopeBuilder = ArrayBuilder(Of Cci.UsedNamespaceOrType).GetInstance

            ' first come xml imports
            If xmlNamespaces IsNot Nothing Then
                For Each xmlImport In xmlNamespaces
                    scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateXmlNamespace(xmlImport.Key, xmlImport.Value.XmlNamespace))
                Next
            End If

            ' then come alias imports
            If aliasImports IsNot Nothing Then
                For Each aliasImport In aliasImports
                    Dim target = aliasImport.Alias.Target
                    If target.IsNamespace Then
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateNamespace(DirectCast(target, NamespaceSymbol), aliasOpt:=aliasImport.Alias.Name))
                    ElseIf Not target.ContainingAssembly.IsLinked
                        ' We skip alias imports of embedded types to avoid breaking existing code that
                        ' imports types that can't be embedded but doesn't use them anywhere else in the code.
                        Dim typeRef = GetTypeReference(DirectCast(target, NamedTypeSymbol), moduleBuilder, diagnostics)
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateType(typeRef, aliasOpt:=aliasImport.Alias.Name))
                    End If
                Next
            End If

            ' then come the imports
            If Not memberImports.IsEmpty Then
                For Each import In memberImports
                    Dim target = import.NamespaceOrType
                    If target.IsNamespace Then
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateNamespace(DirectCast(target, NamespaceSymbol)))
                    ElseIf Not target.ContainingAssembly.IsLinked
                        ' We skip imports of embedded types to avoid breaking existing code that
                        ' imports types that can't be embedded but doesn't use them anywhere else in the code.
                        Dim typeRef = GetTypeReference(DirectCast(target, NamedTypeSymbol), moduleBuilder, diagnostics)
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateType(typeRef))
                    End If
                Next
            End If

            Return scopeBuilder.ToImmutableAndFree()
        End Function

        Private Shared Function GetTypeReference(type As TypeSymbol, moduleBuilder As CommonPEModuleBuilder, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Return moduleBuilder.Translate(type, Nothing, diagnostics)
        End Function
    End Class
End Namespace
