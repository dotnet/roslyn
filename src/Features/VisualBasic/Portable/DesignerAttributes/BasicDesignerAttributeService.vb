' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.DesignerAttributes
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.DesignerAttributes
    <ExportLanguageServiceFactory(GetType(IDesignerAttributeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDesignerAttributeServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New BasicDesignerAttributeService(languageServices.WorkspaceServices.Workspace)
        End Function

    End Class

    Friend Class BasicDesignerAttributeService
        Inherits AbstractDesignerAttributeService

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace)
        End Sub

        Protected Overrides Function GetAllTopLevelTypeDefined(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim compilationUnit = TryCast(node, CompilationUnitSyntax)
            If compilationUnit Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
            End If

            Return compilationUnit.Members.SelectMany(AddressOf GetAllTopLevelTypeDefined)
        End Function

        Private Overloads Function GetAllTopLevelTypeDefined(member As StatementSyntax) As IEnumerable(Of SyntaxNode)
            Dim namespaceMember = TryCast(member, NamespaceBlockSyntax)
            If namespaceMember IsNot Nothing Then
                Return namespaceMember.Members.SelectMany(AddressOf GetAllTopLevelTypeDefined)
            End If

            Dim type = TryCast(member, ClassBlockSyntax)
            If type IsNot Nothing Then
                Return SpecializedCollections.SingletonEnumerable(Of SyntaxNode)(type)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Protected Overrides Function ProcessOnlyFirstTypeDefined() As Boolean
            Return True
        End Function

        Protected Overrides Function HasAttributesOrBaseTypeOrIsPartial(typeNode As SyntaxNode) As Boolean
            Dim type = TryCast(typeNode, ClassBlockSyntax)
            If type IsNot Nothing Then
                ' VB can't actually use any syntactic tricks to limit the types we need to look at.
                ' VB allows up to one partial declaration omit the 'Partial' keyword; so the presence
                ' or absence of attributes, base types, or the 'Partial' keyword doesn't mean anything.
                ' If this is a ClassBlockSyntax node, we're going to have to bind.

                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
