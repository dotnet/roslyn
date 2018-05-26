' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Venus
    Friend Module ContainedLanguageStaticEventBinding

        ''' <summary>
        ''' Find all the methods that handle events (though "Handles" clauses).
        ''' </summary>
        ''' <returns></returns>
        Public Function GetStaticEventBindings(document As Document,
                                               className As String,
                                               objectName As String,
                                               cancellationToken As CancellationToken) As IEnumerable(Of Tuple(Of String, String, String))
            Dim type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken).GetTypeByMetadataName(className)
            Dim methods = type.GetMembers().
                Where(Function(m) m.CanBeReferencedByName AndAlso m.Kind = SymbolKind.Method).
                Cast(Of IMethodSymbol)()

            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()
            Dim methodAndMethodSyntaxesWithHandles = methods.
                Select(Function(m) Tuple.Create(m, GetMethodStatement(syntaxFacts, m))).
                Where(Function(t) t.Item2.HandlesClause IsNot Nothing).
                ToArray()

            If Not methodAndMethodSyntaxesWithHandles.Any() Then
                Return SpecializedCollections.EmptyEnumerable(Of Tuple(Of String, String, String))()
            End If

            Dim result As New List(Of Tuple(Of String, String, String))()
            For Each methodAndMethodSyntax In methodAndMethodSyntaxesWithHandles
                For Each handleClauseItem In methodAndMethodSyntax.Item2.HandlesClause.Events
                    If handleClauseItem.EventContainer.ToString() = objectName OrElse
                        (String.IsNullOrEmpty(objectName) AndAlso handleClauseItem.EventContainer.IsKind(SyntaxKind.MeKeyword, SyntaxKind.MyBaseKeyword, SyntaxKind.MyClassKeyword)) Then
                        result.Add(Tuple.Create(handleClauseItem.EventMember.Identifier.ToString(),
                                                methodAndMethodSyntax.Item2.Identifier.ToString(),
                                                ContainedLanguageCodeSupport.ConstructMemberId(methodAndMethodSyntax.Item1)))
                    End If
                Next
            Next

            Return result
        End Function

        Public Sub AddStaticEventBinding(document As Document,
                                         visualStudioWorkspace As VisualStudioWorkspace,
                                         className As String,
                                         memberId As String,
                                         objectName As String,
                                         nameOfEvent As String,
                                         cancellationToken As CancellationToken)

            Dim type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken).GetTypeByMetadataName(className)
            Dim memberSymbol = ContainedLanguageCodeSupport.LookupMemberId(type, memberId)
            Dim targetDocument = document.Project.Solution.GetDocument(memberSymbol.Locations.First().SourceTree)
            Dim syntaxFacts = targetDocument.Project.LanguageServices.GetService(Of ISyntaxFactsService)()

            If HandlesEvent(GetMethodStatement(syntaxFacts, memberSymbol), objectName, nameOfEvent) Then
                Return
            End If

            Dim textBuffer = targetDocument.GetTextSynchronously(cancellationToken).Container.TryGetTextBuffer()
            If textBuffer Is Nothing Then
                Using visualStudioWorkspace.OpenInvisibleEditor(targetDocument.Id)
                    targetDocument = visualStudioWorkspace.CurrentSolution.GetDocument(targetDocument.Id)
                    AddStaticEventBinding(targetDocument, visualStudioWorkspace, className, memberId, objectName, nameOfEvent, cancellationToken)
                End Using
            Else
                Dim memberStatement = GetMemberBlockOrBegin(syntaxFacts, memberSymbol)
                Dim codeModel = targetDocument.Project.LanguageServices.GetService(Of ICodeModelService)()
                codeModel.AddHandlesClause(targetDocument, objectName & "." & nameOfEvent, memberStatement, cancellationToken)
            End If
        End Sub

        Public Sub RemoveStaticEventBinding(document As Document,
                                            visualStudioWorkspace As VisualStudioWorkspace,
                                            className As String,
                                            memberId As String,
                                            objectName As String,
                                            nameOfEvent As String,
                                            cancellationToken As CancellationToken)
            Dim type = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken).GetTypeByMetadataName(className)
            Dim memberSymbol = ContainedLanguageCodeSupport.LookupMemberId(type, memberId)
            Dim targetDocument = document.Project.Solution.GetDocument(memberSymbol.Locations.First().SourceTree)
            Dim syntaxFacts = targetDocument.Project.LanguageServices.GetService(Of ISyntaxFactsService)()

            If Not HandlesEvent(GetMethodStatement(syntaxFacts, memberSymbol), objectName, nameOfEvent) Then
                Return
            End If

            Dim textBuffer = targetDocument.GetTextSynchronously(cancellationToken).Container.TryGetTextBuffer()
            If textBuffer Is Nothing Then
                Using visualStudioWorkspace.OpenInvisibleEditor(targetDocument.Id)
                    targetDocument = visualStudioWorkspace.CurrentSolution.GetDocument(targetDocument.Id)
                    RemoveStaticEventBinding(targetDocument, visualStudioWorkspace, className, memberId, objectName, nameOfEvent, cancellationToken)
                End Using
            Else
                Dim memberStatement = GetMemberBlockOrBegin(syntaxFacts, memberSymbol)
                Dim codeModel = targetDocument.Project.LanguageServices.GetService(Of ICodeModelService)()
                codeModel.RemoveHandlesClause(targetDocument, objectName & "." & nameOfEvent, memberStatement, cancellationToken)
            End If

        End Sub

        Public Function HandlesEvent(methodStatement As MethodStatementSyntax, objectName As String, eventName As String) As Boolean
            If methodStatement.HandlesClause Is Nothing Then
                Return False
            End If

            For Each handlesClauseItem In methodStatement.HandlesClause.Events
                If handlesClauseItem.EventMember.ToString() = eventName Then

                    If String.IsNullOrEmpty(objectName) AndAlso handlesClauseItem.EventContainer.IsKind(SyntaxKind.MeKeyword, SyntaxKind.MyBaseKeyword, SyntaxKind.MyClassKeyword) Then
                        Return True
                    ElseIf handlesClauseItem.EventContainer.ToString() = objectName Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        Private Function GetMemberBlockOrBegin(syntaxFacts As ISyntaxFactsService, member As ISymbol) As SyntaxNode
            Return member.DeclaringSyntaxReferences.Select(Function(r) r.GetSyntax()).FirstOrDefault()
        End Function

        Private Function GetMethodStatement(syntaxFacts As ISyntaxFactsService, member As ISymbol) As MethodStatementSyntax
            Dim node = GetMemberBlockOrBegin(syntaxFacts, member)
            If node.Kind = SyntaxKind.SubBlock OrElse node.Kind = SyntaxKind.FunctionBlock Then
                Return DirectCast(DirectCast(node, MethodBlockSyntax).BlockStatement, MethodStatementSyntax)
            ElseIf node.Kind = SyntaxKind.SubStatement OrElse node.Kind = SyntaxKind.FunctionStatement Then
                Return DirectCast(node, MethodStatementSyntax)
            Else
                Throw New InvalidOperationException()
            End If
        End Function
    End Module
End Namespace
