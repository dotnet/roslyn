' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class NamespaceScopeBuilder
        Private Sub New()
        End Sub

        Public Shared Function BuildNamespaceScope(
            context As EmitContext,
            xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
            aliasImports As IEnumerable(Of AliasAndImportsClausePosition),
            memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)
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
                    Else
                        Dim typeRef = GetTypeReference(context, DirectCast(target, NamedTypeSymbol))
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
                    Else
                        Dim typeRef = GetTypeReference(context, DirectCast(target, NamedTypeSymbol))
                        scopeBuilder.Add(Cci.UsedNamespaceOrType.CreateType(typeRef))
                    End If
                Next
            End If

            Return scopeBuilder.ToImmutableAndFree()
        End Function

        Private Shared Function GetTypeReference(context As EmitContext, type As TypeSymbol) As Cci.ITypeReference
            Return context.ModuleBuilder.Translate(type, context.SyntaxNodeOpt, context.Diagnostics)
        End Function
    End Class
End Namespace
