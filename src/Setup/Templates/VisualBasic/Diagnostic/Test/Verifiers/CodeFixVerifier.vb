Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports System.Threading

Namespace TestHelper
    ''' <summary>
    ''' Superclass of all Unit tests made for diagnostics with codefixes.
    ''' Contains methods used to verify correctness of codefixes
    ''' </summary>
    Partial Public MustInherit Class CodeFixVerifier
        Inherits DiagnosticVerifier
        ''' <summary>
        ''' Returns the codefix being tested (C#) - to be implemented in non-abstract class
        ''' </summary>
        ''' <returns>The CodeFixProvider to be used for CSharp code</returns>
        Protected Overridable Function GetCSharpCodeFixProvider() As CodeFixProvider
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns the codefix being tested (VB) - to be implemented in non-abstract class
        ''' </summary>
        ''' <returns>The CodeFixProvider to be used for VisualBasic code</returns>
        Protected Overridable Function GetBasicCodeFixProvider() As CodeFixProvider
            Return Nothing
        End Function

        ''' <summary>
        ''' Called to test a C# codefix when applied on the inputted string as a source
        ''' </summary>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Protected Sub VerifyCSharpFix(oldSource As String, newSource As String, Optional codeFixIndex As Integer? = Nothing, Optional allowNewCompilerDiagnostics As Boolean = False)
            VerifyFix(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics)
        End Sub

        ''' <summary>
        ''' Called to test a VB codefix when applied on the inputted string as a source
        ''' </summary>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Protected Sub VerifyBasicFix(oldSource As String, newSource As String, Optional codeFixIndex As Integer? = Nothing, Optional allowNewCompilerDiagnostics As Boolean = False)
            VerifyFix(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics)
        End Sub

        ''' <summary>
        ''' General verifier for codefixes.
        ''' Creates a Document from the source string, then gets diagnostics on it And applies the relevant codefixes.
        ''' Then gets the string after the codefix Is applied And compares it with the expected result.
        ''' Note: If any codefix causes New diagnostics To show up, the test fails unless allowNewCompilerDiagnostics Is Set To True.
        ''' </summary>
        ''' <param name="language">The language the source code Is in</param>
        ''' <param name="analyzer">The analyzer to be applied to the source code</param>
        ''' <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic Is found</param>
        ''' <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        ''' <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        ''' <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        ''' <param name="allowNewCompilerDiagnostics">A bool controlling whether Or Not the test will fail if the CodeFix introduces other warnings after being applied</param>
        Private Sub VerifyFix(language As String, analyzer As DiagnosticAnalyzer, codeFixProvider As CodeFixProvider, oldSource As String, newSource As String, codeFixIndex As Integer?, allowNewCompilerDiagnostics As Boolean)

            Dim document = CreateDocument(oldSource, language)
            Dim analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, New document() {document})
            Dim compilerDiagnostics = GetCompilerDiagnostics(document)
            Dim attempts = analyzerDiagnostics.Length

            For i = 0 To attempts - 1
                Dim actions = New List(Of CodeAction)()
                Dim context = New CodeFixContext(document, analyzerDiagnostics(0), Sub(a, d) actions.Add(a), CancellationToken.None)
                codeFixProvider.RegisterCodeFixesAsync(context).Wait()

                If Not actions.Any() Then
                    Exit For
                End If

                If (codeFixIndex IsNot Nothing) Then
                    document = ApplyFix(document, actions.ElementAt(codeFixIndex.Value))
                    Exit For
                End If

                document = ApplyFix(document, actions.ElementAt(0))
                analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, New document() {document})

                Dim newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, GetCompilerDiagnostics(document))

                'check if applying the code fix introduced any New compiler diagnostics
                If Not allowNewCompilerDiagnostics AndAlso newCompilerDiagnostics.Any() Then
                    ' Format And get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace))
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, GetCompilerDiagnostics(document))

                    Assert.IsTrue(False,
                        String.Format("Fix introduced new compiler diagnostics:{2}{0}{2}{2}New document:{2}{1}{2}",
                            String.Join(vbNewLine, newCompilerDiagnostics.Select(Function(d) d.ToString())),
                            document.GetSyntaxRootAsync().Result.ToFullString(), vbNewLine))
                End If

                'check if there are analyzer diagnostics left after the code fix
                If Not analyzerDiagnostics.Any() Then
                    Exit For
                End If
            Next

            'after applying all of the code fixes, compare the resulting string to the inputted one
            Dim actual = GetStringFromDocument(document)
            Assert.AreEqual(newSource, actual)
        End Sub
    End Class
End Namespace