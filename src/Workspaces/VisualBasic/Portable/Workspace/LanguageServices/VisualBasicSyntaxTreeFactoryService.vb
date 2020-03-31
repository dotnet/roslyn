' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.IO
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(ISyntaxTreeFactoryService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory
        Implements ILanguageServiceFactory

        Private Shared ReadOnly _parseOptionsWithLatestLanguageVersion As VisualBasicParseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSyntaxTreeFactoryService(provider)
        End Function

        Partial Friend Class VisualBasicSyntaxTreeFactoryService
            Inherits AbstractSyntaxTreeFactoryService

            Public Sub New(languageServices As HostLanguageServices)
                MyBase.New(languageServices)
            End Sub

            Public Overloads Overrides Function GetDefaultParseOptions() As ParseOptions
                Return VisualBasicParseOptions.Default
            End Function

            Public Overloads Overrides Function GetDefaultParseOptionsWithLatestLanguageVersion() As ParseOptions
                Return _parseOptionsWithLatestLanguageVersion
            End Function

            Public Overrides Function ParseSyntaxTree(filePath As String, options As ParseOptions, text As SourceText, analyzerConfigOptionsResult As AnalyzerConfigOptionsResult?, cancellationToken As CancellationToken) As SyntaxTree
                If options Is Nothing Then
                    options = GetDefaultParseOptions()
                End If

                ' NOTE: Unlike the C# SyntaxFactory.ParseSyntaxTree API, the VB version does not currently support the flag 'isGeneratedCode' from analyzer options.
                Return SyntaxFactory.ParseSyntaxTree(text, options, filePath, analyzerConfigOptionsResult?.TreeOptions, cancellationToken)
            End Function

            Public Overrides Function CreateSyntaxTree(filePath As String, options As ParseOptions, encoding As Encoding, root As SyntaxNode, analyzerConfigOptionsResult As AnalyzerConfigOptionsResult) As SyntaxTree
                If options Is Nothing Then
                    options = GetDefaultParseOptions()
                End If

                ' NOTE: Unlike the CSharpSyntaxTree.Create API, the VB version does not currently support the flag 'isGeneratedCode' from analyzer options.
                Return VisualBasicSyntaxTree.Create(DirectCast(root, VisualBasicSyntaxNode), DirectCast(options, VisualBasicParseOptions), filePath, encoding, analyzerConfigOptionsResult.TreeOptions)
            End Function

            Public Overrides Function CanCreateRecoverableTree(root As SyntaxNode) As Boolean
                Dim cu = TryCast(root, CompilationUnitSyntax)
                Return MyBase.CanCreateRecoverableTree(root) AndAlso cu IsNot Nothing AndAlso cu.Attributes.Count = 0
            End Function

            Public Overrides Function CreateRecoverableTree(cacheKey As ProjectId,
                                                            filePath As String,
                                                            optionsOpt As ParseOptions,
                                                            text As ValueSource(Of TextAndVersion),
                                                            encoding As Encoding,
                                                            root As SyntaxNode,
                                                            treeDiagnosticReportingOptionsOpt As ImmutableDictionary(Of String, ReportDiagnostic)) As SyntaxTree

                Debug.Assert(CanCreateRecoverableTree(root))
                Return RecoverableSyntaxTree.CreateRecoverableTree(
                    Me,
                    cacheKey,
                    filePath,
                    If(optionsOpt, GetDefaultParseOptions()),
                    text,
                    encoding,
                    DirectCast(root, CompilationUnitSyntax),
                    treeDiagnosticReportingOptionsOpt)
            End Function

            Public Overrides Function DeserializeNodeFrom(stream As Stream, cancellationToken As CancellationToken) As SyntaxNode
                Return VisualBasicSyntaxNode.DeserializeFrom(stream, cancellationToken)
            End Function
        End Class
    End Class
End Namespace
