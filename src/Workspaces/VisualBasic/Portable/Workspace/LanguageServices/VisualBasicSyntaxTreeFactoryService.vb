' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ISyntaxTreeFactoryService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSyntaxTreeFactoryService
        Inherits AbstractSyntaxTreeFactoryService

        Private Shared ReadOnly _parseOptionsWithLatestLanguageVersion As VisualBasicParseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overloads Overrides Function GetDefaultParseOptions() As ParseOptions
            Return VisualBasicParseOptions.Default
        End Function

        Public Overrides Function TryParsePdbParseOptions(metadata As IReadOnlyDictionary(Of String, String)) As ParseOptions
            Dim langVersionString As String = Nothing
            Dim langVersion As LanguageVersion = Nothing

            If Not metadata.TryGetValue("language-version", langVersionString) OrElse
                   Not TryParse(langVersionString, langVersion) Then
                langVersion = LanguageVersion.[Default]
            End If

            Dim defineString As String = Nothing
            If Not metadata.TryGetValue("define", defineString) Then
                Return Nothing
            End If

            Dim diagnostics As IEnumerable(Of Diagnostic) = Nothing
            Dim preprocessorSymbols = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(defineString, diagnostics)
            If diagnostics Is Nothing Then
                Return Nothing
            End If

            Return New VisualBasicParseOptions(languageVersion:=langVersion, preprocessorSymbols:=preprocessorSymbols)
        End Function

        Public Overrides Function OptionsDifferOnlyByPreprocessorDirectives(options1 As ParseOptions, options2 As ParseOptions) As Boolean
            Dim vbOptions1 = DirectCast(options1, VisualBasicParseOptions)
            Dim vbOptions2 = DirectCast(options2, VisualBasicParseOptions)

            ' The easy way to figure out if these only differ by a single field is to update one with the preprocessor symbols of the
            ' other, and then do an equality check from there; this is future proofed if another value is ever added.
            Return vbOptions1.WithPreprocessorSymbols(vbOptions2.PreprocessorSymbols) = vbOptions2
        End Function

        Public Overloads Overrides Function GetDefaultParseOptionsWithLatestLanguageVersion() As ParseOptions
            Return _parseOptionsWithLatestLanguageVersion
        End Function

        Public Overrides Function ParseSyntaxTree(filePath As String, options As ParseOptions, text As SourceText, cancellationToken As CancellationToken) As SyntaxTree
            If options Is Nothing Then
                options = GetDefaultParseOptions()
            End If

            Return SyntaxFactory.ParseSyntaxTree(text, options, filePath, cancellationToken)
        End Function

        Public Overrides Function CreateSyntaxTree(filePath As String, options As ParseOptions, text As SourceText, encoding As Encoding, checksumAlgorithm As SourceHashAlgorithm, root As SyntaxNode) As SyntaxTree
            If options Is Nothing Then
                options = GetDefaultParseOptions()
            End If

            Return New ParsedSyntaxTree(text, DirectCast(root, VisualBasicSyntaxNode), DirectCast(options, VisualBasicParseOptions), filePath, encoding, checksumAlgorithm)
        End Function
    End Class
End Namespace
