' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(PreprocessorCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(SymbolCompletionProvider))>
    <[Shared]>
    Friend Class PreprocessorCompletionProvider
        Inherits AbstractPreprocessorCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerCharsAndParen

        Protected Overrides Function DefinesPreprocessingSymbolName(trivia As SyntaxTrivia, <NotNullWhen(True)> ByRef definedName As String) As Boolean
            definedName = Nothing
            Dim isConst = trivia.IsKind(SyntaxKind.ConstDirectiveTrivia)
            Dim defines = False
            If (isConst) Then
                Dim [structure] = trivia.GetStructure()
                Contract.ThrowIfFalse(TypeOf [structure] Is ConstDirectiveTriviaSyntax)
                Dim directive = DirectCast([structure], ConstDirectiveTriviaSyntax)
                Dim valueString = directive.Value.ToString()
                Dim undefines = valueString.Equals("false", StringComparison.OrdinalIgnoreCase) OrElse
                    valueString.Equals("0")
                defines = Not undefines
                If (defines) Then
                    definedName = directive.Name.Text
                End If
            End If
            Return defines
        End Function
    End Class
End Namespace
