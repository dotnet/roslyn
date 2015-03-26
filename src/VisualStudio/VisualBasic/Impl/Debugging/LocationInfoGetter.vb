' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Debugging

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
    ' TODO: Make this class static when we add that functionality to VB.
    Namespace LocationInfoGetter
        Friend Module LocationInfoGetterModule
            ' TODO remove after general error format has been fixed
            Private ReadOnly s_qualifiedNameFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or
                                SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or
                                    SymbolDisplayParameterOptions.IncludeType Or
                                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                                        SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            Friend Async Function GetInfoAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of DebugLocationInfo)
                Dim name As String = Nothing
                Dim lineOffset As Integer = 0

                ' Note that we get the InProgressSolution here.  Technically, this means that we may
                ' not fully understand the signature of the member.  But that's ok.  We just need this
                ' symbol so we can create a display string to put into the debugger.  If we try to
                ' find the document in the "CurrentSolution" then when we try to get the semantic 
                ' model below then it might take a *long* time as all dependent compilations are built.

                Dim tree = Await document.GetVisualBasicSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                Dim token = root.FindToken(position)
                Dim memberDecl = token.GetContainingMemberBlockBegin()

                If memberDecl IsNot Nothing Then
                    Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                    Dim memberSymbol = semanticModel.GetDeclaredSymbol(memberDecl, cancellationToken)

                    Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
                    Dim lineNumber = text.Lines.GetLineFromPosition(position).LineNumber
                    Dim memberLine = text.Lines.GetLineFromPosition(memberDecl.SpanStart).LineNumber

                    name = memberSymbol.ToDisplayString(s_qualifiedNameFormat)
                    lineOffset = lineNumber - memberLine
                    Return New DebugLocationInfo(name, lineOffset)
                End If

                Return Nothing
            End Function
        End Module
    End Namespace
End Namespace
