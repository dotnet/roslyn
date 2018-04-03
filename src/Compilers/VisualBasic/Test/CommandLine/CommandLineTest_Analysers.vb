' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SharedResourceHelpers
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Partial Public Class CommandLineTests
        Inherits BasicTestBase

        <Fact>
        Public Sub ParseAnalyzers()
            Dim parsedArgs = DefaultParse({"/a:goo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/analyzer:goo.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/analyzer:""goo.dll""", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)

            parsedArgs = DefaultParse({"/a:goo.dll,bar.dll", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify()
            Assert.Equal(2, parsedArgs.AnalyzerReferences.Length)
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences(0).FilePath)
            Assert.Equal("bar.dll", parsedArgs.AnalyzerReferences(1).FilePath)

            parsedArgs = DefaultParse({"/a:", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("a", ":<file_list>"))

            parsedArgs = DefaultParse({"/a", "a.vb"}, _baseDirectory)
            parsedArgs.Errors.Verify(Diagnostic(ERRID.ERR_ArgumentRequired).WithArguments("a", ":<file_list>"))
        End Sub

        <Fact>
        Public Sub Analyzers_Missing()
            Dim source = "Imports System"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:missing.dll", "a.vb"})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                Assert.Equal("vbc : error BC2017: could not find library 'missing.dll'", outWriter.ToString().Trim())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_Empty()
            Dim source = "Imports System"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:" + GetType(Object).Assembly.Location, "a.vb"})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                Assert.DoesNotContain("warning", outWriter.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_Found()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)
            ' This assembly has a MockDiagnosticAnalyzer type which should get run by this compilation.
            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb"})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                ' Diagnostic cannot instantiate
                Assert.True(outWriter.ToString().Contains("warning BC42376"))
                ' Diagnostic is thrown
                Assert.True(outWriter.ToString().Contains("a.vb(2) : warning Warning01: Throwing a diagnostic for types declared"))
                Assert.True(outWriter.ToString().Contains("a.vb(2) : warning Warning03: Throwing a diagnostic for types declared"))
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_WithRuleSet()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Test02" Action="Warning"/>
                                        <Rule Id="Warning03" Action="None"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb", "/ruleset:" + ruleSetFile.Path})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                ' Diagnostic cannot instantiate
                Assert.True(outWriter.ToString().Contains("warning BC42376"))
                '' Diagnostic thrown as error
                'Assert.True(outWriter.ToString().Contains("error Warning01"))
                ' Diagnostic is suppressed
                Assert.False(outWriter.ToString().Contains("warning Warning03"))
            End Using

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_CommandLineOverridesRuleset1()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <IncludeAll Action="Warning"/>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)
            Dim vbc As MockVisualBasicCompiler
            Dim exitCode As Integer
            Dim output As String
            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                          {
                                                "/nologo", "/preferreduilang:en", "/preferreduilang:en", "/t:library",
                                                "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                "/ruleset:" & ruleSetFile.Path, "/warnaserror", "/nowarn:42376"
                                          })
                exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                ' Diagnostics thrown as error: command line always overrides ruleset.
                output = outWriter.ToString()
                Assert.Contains("error Warning01", output, StringComparison.Ordinal)
                Assert.Contains("error Warning03", output, StringComparison.Ordinal)
            End Using

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                          {
                                                "/nologo", "/preferreduilang:en", "/t:library",
                                                "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                "/warnaserror+", "/ruleset:" & ruleSetFile.Path, "/nowarn:42376"
                                          })
                exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                ' Diagnostics thrown as error: command line always overrides ruleset.
                output = outWriter.ToString()
            End Using
            Assert.Contains("error Warning01", output, StringComparison.Ordinal)
            Assert.Contains("error Warning03", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzer_CommandLineOverridesRuleset2()
            Dim source = "Imports System " + vbCrLf + "Public Class Tester" + vbCrLf + "End Class"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile("a.vb").WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Warning03" Action="Warning"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)
            Dim vbc As MockVisualBasicCompiler
            Dim exitCode As Integer
            Dim output As String

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                              {
                                                    "/nologo", "/t:library",
                                                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                    "/ruleset:" & ruleSetFile.Path, "/nowarn"
                                              })
                exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                ' Diagnostics suppressed: command line always overrides ruleset.
                output = outWriter.ToString()
            End Using
            Assert.DoesNotContain("Warning01", output, StringComparison.Ordinal)
            Assert.DoesNotContain("BC31072", output, StringComparison.Ordinal)
            Assert.DoesNotContain("Warning03", output, StringComparison.Ordinal)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                vbc = New MockVisualBasicCompiler(Nothing, dir.Path,
                                          {
                                                "/nologo", "/t:library",
                                                "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb",
                                                "/nowarn", "/ruleset:" & ruleSetFile.Path
                                          })
                exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(0, exitCode)
                ' Diagnostics suppressed: command line always overrides ruleset.
                output = outWriter.ToString()
            End Using

            Assert.DoesNotContain("Warning01", output, StringComparison.Ordinal)
            Assert.DoesNotContain("BC31072", output, StringComparison.Ordinal)
            Assert.DoesNotContain("Warning03", output, StringComparison.Ordinal)

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <Fact>
        Public Sub Analyzers_WithRuleSetIncludeAll()
            Dim source = "Imports System \r\n Public Class Tester \r\n Public Sub Goo() \r\n Dim x As Integer \r\n End Sub \r\n End Class"

            Dim dir = Temp.CreateDirectory()

            Dim file = dir.CreateFile("a.vb")
            file.WriteAllText(source)

            Dim rulesetSource = <?xml version="1.0" encoding="utf-8"?>
                                <RuleSet Name="Ruleset1" Description="Test" ToolsVersion="12.0">
                                    <IncludeAll Action="Error"/>
                                    <Rules AnalyzerId="Microsoft.Analyzers.ManagedCodeAnalysis" RuleNamespace="Microsoft.Rules.Managed">
                                        <Rule Id="Warning01" Action="Error"/>
                                        <Rule Id="Test02" Action="Warning"/>
                                        <Rule Id="Warning03" Action="None"/>
                                    </Rules>
                                </RuleSet>

            Dim ruleSetFile = CreateRuleSetFile(rulesetSource)

            Using outWriter = New StringWriter(CultureInfo.InvariantCulture)
                Dim vbc = New MockVisualBasicCompiler(Nothing, dir.Path, {"/nologo", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.vb", "/ruleset:" + ruleSetFile.Path})
                Dim exitCode = vbc.Run(outWriter, Nothing)
                Assert.Equal(1, exitCode)
                ' Compiler warnings as errors
                Assert.True(outWriter.ToString().Contains("error BC42376"))
                ' User diagnostics not thrown due to compiler errors
                Assert.False(outWriter.ToString().Contains("Warning01"))
                Assert.False(outWriter.ToString().Contains("Warning03"))
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

    End Class

End Namespace
