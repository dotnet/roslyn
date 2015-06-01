' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
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
                Dim tree = Await document.GetVisualBasicSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                Dim token = root.FindToken(position)
                Dim memberDeclaration = token.GetContainingMember()
                ' Unlike C#, VB doesn't show field names.
                If memberDeclaration.Kind = SyntaxKind.FieldDeclaration Then
                    memberDeclaration = memberDeclaration.GetAncestor(Of DeclarationStatementSyntax)()
                End If

                If memberDeclaration Is Nothing Then
                    Return Nothing
                End If

                Dim compilation = Await document.GetVisualBasicCompilationAsync(cancellationToken).ConfigureAwait(False)
                Dim name = GetName(memberDeclaration, compilation.Options.RootNamespace)

                Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
                Dim lineNumber = text.Lines.GetLineFromPosition(position).LineNumber
                Dim memberLine = text.Lines.GetLineFromPosition(memberDeclaration.GetMemberBlockBegin().SpanStart).LineNumber
                Dim lineOffset = lineNumber - memberLine

                Return New DebugLocationInfo(name, lineOffset)
            End Function

            Private Function GetName(memberDeclaration As DeclarationStatementSyntax, rootNamespace As String) As String
                Const missingInformationPlaceholder As String = "?"
                Const dotToken As Char = "."c

                ' root namespace
                Dim pooled = PooledStringBuilder.GetInstance()
                Dim builder = pooled.Builder
                If Not String.IsNullOrEmpty(rootNamespace) Then
                    builder.Append(rootNamespace)
                    builder.Append(dotToken)
                End If
                ' containing namespace(s) and type(s)
                Dim containingDeclarationNames As ArrayBuilder(Of String) = ArrayBuilder(Of String).GetInstance()
                Dim containingDeclaration = memberDeclaration.Parent
                If TypeOf memberDeclaration Is TypeStatementSyntax Then
                    containingDeclaration = containingDeclaration?.Parent
                End If
                While containingDeclaration IsNot Nothing
                    Dim [namespace] = TryCast(containingDeclaration, NamespaceBlockSyntax)
                    If [namespace] IsNot Nothing Then
                        Dim syntax = [namespace].NamespaceStatement.Name
                        containingDeclarationNames.Add(If(syntax.IsMissing, missingInformationPlaceholder, syntax.ToString()))
                    Else
                        Dim type = TryCast(containingDeclaration, TypeBlockSyntax)
                        If type IsNot Nothing Then
                            Dim token = type.GetNameToken()
                            If token.IsMissing Then
                                containingDeclarationNames.Add(missingInformationPlaceholder)
                            Else
                                ' generic type parameters (if any)
                                Dim typeParameters = type.GetTypeParameterList()
                                containingDeclarationNames.Add(If(typeParameters IsNot Nothing, token.Text & typeParameters.ToString(), token.Text))
                            End If
                        End If
                    End If
                    containingDeclaration = containingDeclaration.Parent
                End While
                For i = containingDeclarationNames.Count - 1 To 0 Step -1
                    builder.Append(containingDeclarationNames(i))
                    builder.Append(dotToken)
                Next
                containingDeclarationNames.Free()

                ' simple name
                Dim nameToken = memberDeclaration.GetNameToken()
                builder.Append(If(nameToken.IsMissing, missingInformationPlaceholder, nameToken.Text))

                ' generic type parameters (if any)
                builder.Append(memberDeclaration.GetTypeParameterList())

                ' parameter list (if any)
                builder.Append(memberDeclaration.GetParameterList())

                ' As clause (if any)
                Dim asClause = memberDeclaration.GetAsClause()
                If asClause IsNot Nothing Then
                    builder.Append(" "c)
                    builder.Append(asClause)
                End If

                Return pooled.ToStringAndFree()
            End Function

            Private Sub AppendContainingDeclarationNames(Of TDeclarationSyntax As DeclarationStatementSyntax)(
                ByRef containingDeclarationNames As ArrayBuilder(Of String),
                ByRef declaration As TDeclarationSyntax,
                appendText As Action(Of ArrayBuilder(Of String), TDeclarationSyntax),
                getAncestor As Func(Of TDeclarationSyntax, TDeclarationSyntax))


            End Sub
        End Module
    End Namespace
End Namespace
