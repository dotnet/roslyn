' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO

Namespace BasicAnalyzers

    ''' <summary>
    ''' Analyzer to demonstrate reading an additional file with a structed format.
    ''' It looks for an additional file named "Terms.xml" and dumps it to a stream
    ''' so that it can be loaded into an <see cref="XDocument"/>. It then extracts
    ''' terms from the XML, detects type names that use those terms and reports
    ''' diagnostics on them.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic, LanguageNames.CSharp)>
    Public Class XmlAdditionalFileAnalyzer
        Inherits DiagnosticAnalyzer

        Private Const Title As String = "Type name contains invalid term"
        Private Const MessageFormat As String = "The term '{0}' is not allowed in a type name."

        Private Shared Rule As DiagnosticDescriptor =
            New DiagnosticDescriptor(
                DiagnosticIds.XmlAdditionalFileAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.AdditionalFile,
                DiagnosticSeverity.Error,
                isEnabledByDefault:=True)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterCompilationStartAction(
                Sub(compilationStartContext)
                    ' Find the additional file with the terms.
                    Dim additionalFiles As ImmutableArray(Of AdditionalText) = compilationStartContext.Options.AdditionalFiles
                    Dim termsFile As AdditionalText = additionalFiles.FirstOrDefault(
                        Function(file)
                            Return Path.GetFileName(file.Path).Equals("Terms.txt")
                        End Function)

                    If termsFile IsNot Nothing Then
                        Dim terms As HashSet(Of String) = New HashSet(Of String)
                        Dim fileText As SourceText = termsFile.GetText(compilationStartContext.CancellationToken)

                        ' Write the additional file back to a stream.
                        Dim stream As MemoryStream = New MemoryStream()
                        Using writer As StreamWriter = New StreamWriter(stream)
                            fileText.Write(writer)
                        End Using

                        ' Read all the <Term> elements to get the terms.
                        Dim document As XDocument = XDocument.Load(stream)
                        For Each termElement As XElement In document.Descendants("Term")
                            terms.Add(termElement.Value)
                        Next

                        ' Check every named type for the invalid terms.
                        compilationStartContext.RegisterSymbolAction(
                            Sub(symbolAnalysisContext)
                                Dim namedTypeSymbol As INamedTypeSymbol = DirectCast(symbolAnalysisContext.Symbol, INamedTypeSymbol)
                                Dim symbolName As String = namedTypeSymbol.Name

                                For Each term As String In terms
                                    If symbolName.Contains(term) Then
                                        symbolAnalysisContext.ReportDiagnostic(
                                        Diagnostic.Create(Rule, namedTypeSymbol.Locations(0), term))
                                    End If
                                Next
                            End Sub,
                            SymbolKind.NamedType)
                    End If

                End Sub)
        End Sub
    End Class
End Namespace