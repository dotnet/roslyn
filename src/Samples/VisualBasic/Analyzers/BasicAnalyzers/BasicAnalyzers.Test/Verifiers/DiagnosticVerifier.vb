' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Text

Namespace TestHelper
    ''' <summary> Superclass of all Unit Tests for DiagnosticAnalyzers. </summary>
    Partial Public MustInherit Class DiagnosticVerifier
#Region " To be implemented by Test classes "
        ''' <summary>
        ''' Get the CSharp analyzer being tested - to be implemented in non-abstract class
        ''' </summary>
        Protected Overridable Function GetCSharpDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return Nothing
        End Function

        ''' <summary>
        ''' Get the Visual Basic analyzer being tested (C#) - to be implemented in non-abstract class
        ''' </summary>
        Protected Overridable Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return Nothing
        End Function
#End Region

#Region " Verifier wrappers "

        ''' <summary>
        ''' Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="source">A class in the form of a string to run the analyzer on</param>
        ''' <param name="expected"> DiagnosticResults that should appear after the analyzer Is run on the source</param>
        Protected Sub VerifyCSharpDiagnostic(source As String, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics({source}, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected)
        End Sub

        ''' <summary>
        ''' Called to test a VB DiagnosticAnalyzer when applied on the single inputted string as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="source">A class in the form of a string to run the analyzer on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the source</param>
        Protected Sub VerifyBasicDiagnostic(source As String, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics({source}, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), expected)
        End Sub

        ''' <summary>
        ''' Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Protected Sub VerifyCSharpDiagnostic(sources As String(), ParamArray expected As DiagnosticResult())

            VerifyDiagnostics(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected)
        End Sub

        ''' <summary>
        ''' Called to test a VB DiagnosticAnalyzer when applied on the inputted strings as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Protected Sub VerifyBasicDiagnostic(sources As String(), ParamArray expected As DiagnosticResult())

            VerifyDiagnostics(sources, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), expected)
        End Sub

        ''' <summary>
        ''' General method that gets a collection of actual diagnostics found in the source after the analyzer Is run, 
        ''' then verifies each of them.
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="language">The language of the classes represented by the source strings</param>
        ''' <param name="analyzer">The analyzer to be run on the source code</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Private Sub VerifyDiagnostics(sources As String(), language As String, analyzer As DiagnosticAnalyzer, ParamArray expected As DiagnosticResult())

            Dim diagnostics = GetSortedDiagnostics(sources, language, analyzer, parseOptions:=Nothing, compilationOptions:=Nothing)
            VerifyDiagnosticResults(diagnostics, analyzer, expected)
        End Sub

        ''' <summary>
        ''' Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="source">A class in the form of a string to run the analyzer on</param>
        ''' <param name="expected"> DiagnosticResults that should appear after the analyzer Is run on the source</param>
        Protected Sub VerifyCSharpDiagnostic(source As String, parseOptions As ParseOptions, compilationOptions As CompilationOptions, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics({source}, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), parseOptions, compilationOptions, expected)
        End Sub

        ''' <summary>
        ''' Called to test a VB DiagnosticAnalyzer when applied on the single inputted string as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="source">A class in the form of a string to run the analyzer on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the source</param>
        Protected Sub VerifyBasicDiagnostic(source As String, parseOptions As ParseOptions, compilationOptions As CompilationOptions, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics({source}, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), parseOptions, compilationOptions, expected)
        End Sub

        ''' <summary>
        ''' Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Protected Sub VerifyCSharpDiagnostic(sources As String(), parseOptions As ParseOptions, compilationOptions As CompilationOptions, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), parseOptions, compilationOptions, expected)
        End Sub

        ''' <summary>
        ''' Called to test a VB DiagnosticAnalyzer when applied on the inputted strings as a source
        ''' Note: input a DiagnosticResult For Each Diagnostic expected
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Protected Sub VerifyBasicDiagnostic(sources As String(), parseOptions As ParseOptions, compilationOptions As CompilationOptions, ParamArray expected As DiagnosticResult())

            VerifyDiagnostics(sources, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), parseOptions, compilationOptions, expected)
        End Sub

        ''' <summary>
        ''' General method that gets a collection of actual diagnostics found in the source after the analyzer Is run, 
        ''' then verifies each of them.
        ''' </summary>
        ''' <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        ''' <param name="language">The language of the classes represented by the source strings</param>
        ''' <param name="analyzer">The analyzer to be run on the source code</param>
        ''' <param name="expected">DiagnosticResults that should appear after the analyzer Is run on the sources</param>
        Private Sub VerifyDiagnostics(sources As String(), language As String, analyzer As DiagnosticAnalyzer, parseOptions As ParseOptions, compilationOptions As CompilationOptions, ParamArray expected As DiagnosticResult())

            Dim diagnostics = GetSortedDiagnostics(sources, language, analyzer, parseOptions, compilationOptions)
            VerifyDiagnosticResults(diagnostics, analyzer, expected)
        End Sub

#End Region

#Region " Actual comparisons And verifications "
        ''' <summary>
        ''' Checks each of the actual Diagnostics found And compares them with the corresponding DiagnosticResult in the array of expected results.
        ''' Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, And Message of the DiagnosticResult match the actual diagnostic.
        ''' </summary>
        ''' <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        ''' <param name="analyzer">The analyzer that was being run on the sources</param>
        ''' <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        Private Shared Sub VerifyDiagnosticResults(actualResults As IEnumerable(Of Diagnostic), analyzer As DiagnosticAnalyzer, ParamArray expectedResults As DiagnosticResult())

            Dim expectedCount = expectedResults.Count()
            Dim actualCount = actualResults.Count()

            If expectedCount <> actualCount Then

                Dim diagnosticsOutput = If(actualResults.Any(), FormatDiagnostics(analyzer, actualResults.ToArray()), "    NONE.")

                Assert.IsTrue(False,
                    String.Format(
"Mismatch between number of diagnostics returned, expected ""{0}"" actual ""{1}""

Diagnostics:
{2}
", expectedCount, actualCount, diagnosticsOutput))
            End If

            For i = 0 To expectedResults.Length - 1

                Dim actual = actualResults.ElementAt(i)
                Dim expected = expectedResults(i)

                If expected.Line = -1 AndAlso expected.Column = -1 Then

                    If (actual.Location <> Location.None) Then

                        Assert.IsTrue(False,
                        String.Format(
"Expected:
A project diagnostic with No location
Actual:
{0}",
                        FormatDiagnostics(analyzer, actual)))
                    End If

                Else

                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First())
                    Dim additionalLocations = actual.AdditionalLocations.ToArray()

                    If (additionalLocations.Length <> expected.Locations.Length - 1) Then

                        Assert.IsTrue(False,
                    String.Format(
"Expected {0} additional locations but got {1} for Diagnostic:
    {2}
",
                        expected.Locations.Length - 1, additionalLocations.Length,
                        FormatDiagnostics(analyzer, actual)))
                    End If

                    For j = 0 To additionalLocations.Length - 1

                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations(j), expected.Locations(j + 1))
                    Next
                End If

                If (actual.Id <> expected.Id) Then

                    Assert.IsTrue(False,
    String.Format(
"Expected diagnostic id to be ""{0}"" was ""{1}""

Diagnostic:
    {2}
",
                            expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)))
                End If

                If (actual.Severity <> expected.Severity) Then

                    Assert.IsTrue(False,
String.Format(
"Expected diagnostic severity to be ""{0}"" was ""{1}""

Diagnostic:
    {2}
",
                            expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)))
                End If

                If (actual.GetMessage() <> expected.Message) Then

                    Assert.IsTrue(False,
String.Format(
"Expected diagnostic message to be ""{0}"" was ""{1}""

Diagnostic:
    {2}
",
                            expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)))
                End If
            Next
        End Sub

        ''' <summary>
        ''' Helper method to VerifyDiagnosticResult that checks the location of a diagnostic And compares it with the location in the expected DiagnosticResult.
        ''' </summary>
        ''' <param name="analyzer">The analyzer that was being run on the sources</param>
        ''' <param name="diagnostic">The diagnostic that was found in the code</param>
        ''' <param name="actual">The Location of the Diagnostic found in the code</param>
        ''' <param name="expected">The DiagnosticResultLocation that should have been found</param>
        Private Shared Sub VerifyDiagnosticLocation(analyzer As DiagnosticAnalyzer, diagnostic As Diagnostic, actual As Location, expected As DiagnosticResultLocation)

            Dim actualSpan = actual.GetLineSpan()

            Assert.IsTrue(actualSpan.Path = expected.Path OrElse (actualSpan.Path IsNot Nothing AndAlso actualSpan.Path.Contains("Test0.") AndAlso expected.Path.Contains("Test.")),
                String.Format(
"Expected diagnostic to be in file ""{0}"" was actually in file ""{1}""

Diagnostic:
    {2}
",
                    expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)))

            Dim actualLinePosition = actualSpan.StartLinePosition

            ' Only check line position if there Is an actual line in the real diagnostic
            If (actualLinePosition.Line > 0) Then

                If (actualLinePosition.Line + 1.0 <> expected.Line) Then

                    Assert.IsTrue(False,
                        String.Format(
"Expected diagnostic to be on line ""{0}"" was actually on line ""{1}""
                        
Diagnostic:
    {2}
",
                            expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)))
                End If
            End If

            ' Only check column position if there Is an actual column position in the real diagnostic
            If (actualLinePosition.Character > 0) Then

                If (actualLinePosition.Character + 1.0 <> expected.Column) Then

                    Assert.IsTrue(False,
                        String.Format(
"Expected diagnostic to start at column ""{0}"" was actually at column ""{1}""

Diagnostic:
    {2}
",
                            expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)))
                End If
            End If
        End Sub
#End Region

#Region " Formatting Diagnostics "
        ''' <summary>
        ''' Helper method to format a Diagnostic into an easily readable string
        ''' </summary>
        ''' <param name="analyzer">The analyzer that this verifier tests</param>
        ''' <param name="diagnostics">The Diagnostics to be formatted</param>
        ''' <returns>The Diagnostics formatted as a string</returns>
        Private Shared Function FormatDiagnostics(analyzer As DiagnosticAnalyzer, ParamArray diagnostics As Diagnostic()) As String

            Dim builder = New StringBuilder()
            For i = 0 To diagnostics.Length - 1

                builder.AppendLine("' " & diagnostics(i).ToString())

                Dim analyzerType = analyzer.GetType()
                Dim rules = analyzer.SupportedDiagnostics

                For Each rule In rules

                    If (rule IsNot Nothing AndAlso rule.Id = diagnostics(i).Id) Then

                        Dim location = diagnostics(i).Location
                        If (location = location.None) Then

                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id)
                        Else

                            Assert.IsTrue(location.IsInSource,
                                $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics(i)}
                                ")

                            Dim resultMethodName As String = If(diagnostics(i).Location.SourceTree.FilePath.EndsWith(".cs"), "GetCSharpResultAt", "GetBasicResultAt")
                            Dim linePosition = diagnostics(i).Location.GetLineSpan().StartLinePosition

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id)
                        End If

                        If i <> diagnostics.Length - 1 Then

                            builder.Append(","c)
                        End If

                        builder.AppendLine()
                        Exit For
                    End If
                Next
            Next
            Return builder.ToString()
        End Function
#End Region
    End Class
End Namespace
