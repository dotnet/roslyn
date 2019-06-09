' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.Shell.Interop
Imports IVsTextBufferCoordinator = Microsoft.VisualStudio.TextManager.Interop.IVsTextBufferCoordinator
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Venus

    Friend Class VisualBasicContainedLanguage
        Inherits ContainedLanguage(Of VisualBasicPackage, VisualBasicLanguageService)
        Implements IVsContainedLanguageStaticEventBinding

        Public Sub New(bufferCoordinator As IVsTextBufferCoordinator,
                componentModel As IComponentModel,
                project As VisualStudioProject,
                hierarchy As IVsHierarchy,
                itemid As UInteger,
                languageService As VisualBasicLanguageService,
                sourceCodeKind As SourceCodeKind)
            MyBase.New(bufferCoordinator, componentModel, project, hierarchy, itemid, projectTrackerOpt:=Nothing, project.Id, languageService, VisualBasicHelperFormattingRule.Instance)
        End Sub

        Public Function AddStaticEventBinding(pszClassName As String,
                                              pszUniqueMemberID As String,
                                              pszObjectName As String,
                                              pszNameOfEvent As String) As Integer Implements IVsContainedLanguageStaticEventBinding.AddStaticEventBinding
            Me.ComponentModel.GetService(Of IWaitIndicator)().Wait(
                BasicVSResources.IntelliSense,
                allowCancel:=False,
                action:=Sub(c)
                            Dim visualStudioWorkspace = ComponentModel.GetService(Of VisualStudioWorkspace)()
                            Dim document = GetThisDocument()
                            ContainedLanguageStaticEventBinding.AddStaticEventBinding(
                                document, visualStudioWorkspace, pszClassName, pszUniqueMemberID, pszObjectName, pszNameOfEvent, c.CancellationToken)
                        End Sub)
            Return VSConstants.S_OK
        End Function

        Public Function EnsureStaticEventHandler(pszClassName As String,
                                                 pszObjectTypeName As String,
                                                 pszObjectName As String,
                                                 pszNameOfEvent As String,
                                                 pszEventHandlerName As String,
                                                 itemidInsertionPoint As UInteger,
                                                 ByRef pbstrUniqueMemberID As String,
                                                 ByRef pbstrEventBody As String,
                                                 pSpanInsertionPoint() As VsTextSpan) As Integer Implements IVsContainedLanguageStaticEventBinding.EnsureStaticEventHandler

            Dim thisDocument = GetThisDocument()
            Dim targetDocumentId = Me.ContainedDocument.FindProjectDocumentIdWithItemId(itemidInsertionPoint)
            Dim targetDocument = thisDocument.Project.Solution.GetDocument(targetDocumentId)
            If targetDocument Is Nothing Then
                Throw New InvalidOperationException("Can't generate into that itemid")
            End If

            Dim idBodyAndInsertionPoint = ContainedLanguageCodeSupport.EnsureEventHandler(
                thisDocument,
                targetDocument,
                pszClassName,
                pszObjectName,
                pszObjectTypeName,
                pszNameOfEvent,
                pszEventHandlerName,
                itemidInsertionPoint,
                useHandlesClause:=True,
                additionalFormattingRule:=LineAdjustmentFormattingRule.Instance,
                cancellationToken:=Nothing)

            pbstrUniqueMemberID = idBodyAndInsertionPoint.Item1
            pbstrEventBody = idBodyAndInsertionPoint.Item2
            pSpanInsertionPoint(0) = idBodyAndInsertionPoint.Item3
            Return VSConstants.S_OK
        End Function

        Public Function GetStaticEventBindingsForObject(pszClassName As String,
                                                        pszObjectName As String,
                                                        ByRef pcMembers As Integer,
                                                        ppbstrEventNames As IntPtr,
                                                        ppbstrDisplayNames As IntPtr,
                                                        ppbstrMemberIDs As IntPtr) As Integer Implements IVsContainedLanguageStaticEventBinding.GetStaticEventBindingsForObject
            Dim members As Integer
            Me.ComponentModel.GetService(Of IWaitIndicator)().Wait(
                BasicVSResources.IntelliSense,
                allowCancel:=False,
                action:=Sub(c)
                            Dim eventNamesAndMemberNamesAndIds = ContainedLanguageStaticEventBinding.GetStaticEventBindings(
                                GetThisDocument(), pszClassName, pszObjectName, c.CancellationToken)
                            members = eventNamesAndMemberNamesAndIds.Count()
                            CreateBSTRArray(ppbstrEventNames, eventNamesAndMemberNamesAndIds.Select(Function(e) e.Item1))
                            CreateBSTRArray(ppbstrDisplayNames, eventNamesAndMemberNamesAndIds.Select(Function(e) e.Item2))
                            CreateBSTRArray(ppbstrMemberIDs, eventNamesAndMemberNamesAndIds.Select(Function(e) e.Item3))
                        End Sub)

            pcMembers = members
            Return VSConstants.S_OK
        End Function

        Public Function RemoveStaticEventBinding(pszClassName As String,
                                                 pszUniqueMemberID As String,
                                                 pszObjectName As String,
                                                 pszNameOfEvent As String) As Integer Implements IVsContainedLanguageStaticEventBinding.RemoveStaticEventBinding

            Me.ComponentModel.GetService(Of IWaitIndicator)().Wait(
                BasicVSResources.IntelliSense,
                allowCancel:=False,
                action:=Sub(c)
                            Dim visualStudioWorkspace = ComponentModel.GetService(Of VisualStudioWorkspace)()
                            Dim document = GetThisDocument()
                            ContainedLanguageStaticEventBinding.RemoveStaticEventBinding(
                                document, visualStudioWorkspace, pszClassName, pszUniqueMemberID, pszObjectName, pszNameOfEvent, c.CancellationToken)
                        End Sub)
            Return VSConstants.S_OK
        End Function

        Private Class VisualBasicHelperFormattingRule
            Inherits CompatAbstractFormattingRule

            Public Shared Shadows Instance As AbstractFormattingRule = New VisualBasicHelperFormattingRule()

            Public Overrides Sub AddIndentBlockOperationsSlow(list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, ByRef nextOperation As NextIndentBlockOperationAction)
                ' we need special behavior for VB due to @Helper code generation weird-ness.
                ' this will looking for code gen specific style to make it not so expansive
                If IsEndHelperPattern(node) Then
                    Return
                End If

                Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
                If multiLineLambda IsNot Nothing AndAlso IsHelperSubLambda(multiLineLambda) Then
                    Return
                End If

                MyBase.AddIndentBlockOperationsSlow(list, node, optionSet, nextOperation)
            End Sub

            Private Shared Function IsHelperSubLambda(multiLineLambda As MultiLineLambdaExpressionSyntax) As Boolean
                If multiLineLambda.Kind <> SyntaxKind.MultiLineSubLambdaExpression Then
                    Return False
                End If

                If multiLineLambda.SubOrFunctionHeader Is Nothing OrElse
                   multiLineLambda.SubOrFunctionHeader.ParameterList Is Nothing OrElse
                   multiLineLambda.SubOrFunctionHeader.ParameterList.Parameters.Count <> 1 OrElse
                   multiLineLambda.SubOrFunctionHeader.ParameterList.Parameters(0).Identifier.Identifier.Text <> "__razor_helper_writer" Then
                    Return False
                End If

                Return True
            End Function

            Private Shared Function IsEndHelperPattern(node As SyntaxNode) As Boolean
                If Not node.HasStructuredTrivia Then
                    Return False
                End If

                Dim method = TryCast(node, MethodBlockSyntax)
                If method Is Nothing OrElse method.Statements.Count <> 2 Then
                    Return False
                End If

                Dim statementWithEndHelper = method.Statements(0)
                Dim endToken = statementWithEndHelper.GetFirstToken()
                If endToken.Kind <> SyntaxKind.EndKeyword Then
                    Return False
                End If

                Dim helperToken = endToken.GetNextToken(includeSkipped:=True)
                If helperToken.Kind <> SyntaxKind.IdentifierToken OrElse
                   Not String.Equals(helperToken.Text, "Helper", StringComparison.OrdinalIgnoreCase) Then
                    Return False
                End If

                Dim asToken = helperToken.GetNextToken(includeSkipped:=True)
                If asToken.Kind <> SyntaxKind.AsKeyword Then
                    Return False
                End If

                Return True
            End Function
        End Class
    End Class
End Namespace
