' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
Imports System.Composition

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.DesignerAttribute
    <ExportPerLanguageIncrementalAnalyzerProvider(DesignerAttributeIncrementalAnalyzerProvider.Name, LanguageNames.VisualBasic), [Shared]>
    Friend Class BasicDesignerAttributeIncrementalAnalyzerProvider
        Implements IPerLanguageIncrementalAnalyzerProvider

        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _notificationService As IForegroundNotificationService
        Private ReadOnly _asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata))

        <ImportingConstructor>
        Public Sub New(
            serviceProvider As SVsServiceProvider,
            notificationService As IForegroundNotificationService,
            <ImportMany> asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
            Me._serviceProvider = serviceProvider
            Me._notificationService = notificationService
            Me._asyncListeners = asyncListeners
        End Sub

        Public Function CreatePerLanguageIncrementalAnalyzer(workspace As Workspace, provider As IIncrementalAnalyzerProvider) As IIncrementalAnalyzer Implements IPerLanguageIncrementalAnalyzerProvider.CreatePerLanguageIncrementalAnalyzer
            Return New DesignerAttributeIncrementalAnalyzer(Me._serviceProvider, Me._notificationService, Me._asyncListeners)
        End Function

        Private Class DesignerAttributeIncrementalAnalyzer
            Inherits AbstractDesignerAttributeIncrementalAnalyzer

            Public Sub New(
                serviceProvider As IServiceProvider,
                notificationService As IForegroundNotificationService,
                asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
                MyBase.New(serviceProvider, notificationService, asyncListeners)
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
    End Class
End Namespace
