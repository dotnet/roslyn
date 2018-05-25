' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
    ' TODO: Make this class static when we add that functionality to VB.
    Namespace LocationInfoGetter
        Friend Module LocationInfoGetterModule
            Friend Async Function GetInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugLocationInfo)
                ' PERF:  This method will be called synchronously on the UI thread for every breakpoint in the solution.
                ' Therefore, it is important that we make this call as cheap as possible.  Rather than constructing a
                ' containing Symbol and using ToDisplayString (which might be more *correct*), we'll just do the best we
                ' can with Syntax.  This approach is capable of providing parity with the pre-Roslyn implementation.
                Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)()
                Dim memberDeclaration = TryCast(syntaxFactsService.GetContainingMemberDeclaration(root, position, useFullSpan:=True), DeclarationStatementSyntax)

                ' Unlike C#, VB doesn't show field names.
                If memberDeclaration?.Kind = SyntaxKind.FieldDeclaration Then
                    memberDeclaration = memberDeclaration.GetAncestor(Of DeclarationStatementSyntax)()
                End If

                If memberDeclaration Is Nothing Then
                    Return Nothing
                End If

                Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                Dim options = CType(compilation.Options, VisualBasicCompilationOptions)
                Dim name = syntaxFactsService.GetDisplayName(memberDeclaration,
                                                             DisplayNameOptions.IncludeNamespaces Or
                                                             DisplayNameOptions.IncludeParameters Or
                                                             DisplayNameOptions.IncludeType Or
                                                             DisplayNameOptions.IncludeTypeParameters,
                                                             options.RootNamespace)

                Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
                Dim lineNumber = text.Lines.GetLineFromPosition(position).LineNumber
                Dim memberLine = text.Lines.GetLineFromPosition(memberDeclaration.GetMemberBlockBegin().SpanStart).LineNumber
                Dim lineOffset = lineNumber - memberLine

                Return New DebugLocationInfo(name, lineOffset)
            End Function
        End Module
    End Namespace
End Namespace
