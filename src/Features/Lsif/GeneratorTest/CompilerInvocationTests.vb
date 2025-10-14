' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public Class CompilerInvocationTests
        <Fact>
        Public Async Function TestCSharpProject() As Task
            ' PortableExecutableReference.CreateFromFile implicitly reads the file so the file must exist.
            Dim referencePath = GetType(Object).Assembly.Location

            Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                {
                    ""tool"": ""csc"",
                    ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG /reference:" + referencePath.Replace("\", "\\") + " Z:\\SourceFile.cs /target:library /out:Z:\\Output.dll"",
                    ""projectFilePath"": ""Z:\\Project.csproj"",
                    ""sourceRootPath"": ""Z:\\""
                }")

            Assert.Equal(LanguageNames.CSharp, project.Language)
            Assert.Equal("Z:\Project.csproj", project.FilePath)
            Dim compilation = Await project.GetCompilationAsync()
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, compilation.Options.OutputKind)

            Dim syntaxTree = Assert.Single(compilation.SyntaxTrees)
            Assert.Equal("Z:\SourceFile.cs", syntaxTree.FilePath)
            Assert.Equal("DEBUG", Assert.Single(syntaxTree.Options.PreprocessorSymbolNames))

            Dim metadataReference = Assert.Single(compilation.References)

            Assert.Equal(referencePath, DirectCast(metadataReference, PortableExecutableReference).FilePath)
        End Function

        <Fact>
        Public Async Function TestVisualBasicProject() As Task
            ' PortableExecutableReference.CreateFromFile implicitly reads the file so the file must exist.
            Dim referencePath = GetType(Object).Assembly.Location

            Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                {
                    ""tool"": ""vbc"",
                    ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG /reference:" + referencePath.Replace("\", "\\") + " Z:\\SourceFile.vb /target:library /out:Z:\\Output.dll"",
                    ""projectFilePath"": ""Z:\\Project.vbproj"",
                    ""sourceRootPath"": ""Z:\\""
                }")

            Assert.Equal(LanguageNames.VisualBasic, project.Language)
            Assert.Equal("Z:\Project.vbproj", project.FilePath)
            Dim compilation = Await project.GetCompilationAsync()
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, compilation.Options.OutputKind)

            Dim syntaxTree = Assert.Single(compilation.SyntaxTrees)
            Assert.Equal("Z:\SourceFile.vb", syntaxTree.FilePath)
            Assert.Contains("DEBUG", syntaxTree.Options.PreprocessorSymbolNames)

            Dim metadataReference = Assert.Single(compilation.References)

            Assert.Equal(referencePath, DirectCast(metadataReference, PortableExecutableReference).FilePath)
        End Function

        <Theory>
        <CombinatorialData>
        Public Async Function TestSourceFilePathMappingWithDriveLetters(<CombinatorialValues("F:", "F:\")> from As String, <CombinatorialValues("T:", "T:\")> [to] As String) As Task
            Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                {
                    ""tool"": ""csc"",
                    ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG F:\\SourceFile.cs /target:library /out:F:\\Output.dll"",
                    ""projectFilePath"": ""F:\\Project.csproj"",
                    ""sourceRootPath"": ""F:\\"",
                    ""pathMappings"": [
                         {
                             ""from"": """ + from.Replace("\", "\\") + """,
                             ""to"": """ + [to].Replace("\", "\\") + """
                         }]
                }")

            Dim compilation = Await project.GetCompilationAsync()
            Dim syntaxTree = Assert.Single(compilation.SyntaxTrees)

            Assert.Equal("T:\SourceFile.cs", syntaxTree.FilePath)
        End Function

        <Fact>
        Public Async Function TestSourceFilePathMappingWithSubdirectoriesWithoutTrailingSlashes() As Task
            Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                {
                    ""tool"": ""csc"",
                    ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG F:\\Directory\\SourceFile.cs /target:library /out:F:\\Output.dll"",
                    ""projectFilePath"": ""F:\\Project.csproj"",
                    ""sourceRootPath"": ""F:\\"",
                    ""pathMappings"": [
                         {
                             ""from"": ""F:\\Directory"",
                             ""to"": ""T:\\Directory""
                         }]
                }")

            Dim compilation = Await project.GetCompilationAsync()
            Dim syntaxTree = Assert.Single(compilation.SyntaxTrees)

            Assert.Equal("T:\Directory\SourceFile.cs", syntaxTree.FilePath)
        End Function

        <Fact>
        Public Async Function TestSourceFilePathMappingWithSubdirectoriesWithDoubleSlashesInFilePath() As Task
            Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                {
                    ""tool"": ""csc"",
                    ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG F:\\Directory\\\\SourceFile.cs /target:library /out:F:\\Output.dll"",
                    ""projectFilePath"": ""F:\\Project.csproj"",
                    ""sourceRootPath"": ""F:\\"",
                    ""pathMappings"": [
                         {
                             ""from"": ""F:\\Directory"",
                             ""to"": ""T:\\Directory""
                         }]
                }")

            Dim compilation = Await project.GetCompilationAsync()
            Dim syntaxTree = Assert.Single(compilation.SyntaxTrees)

            Assert.Equal("T:\Directory\SourceFile.cs", syntaxTree.FilePath)
        End Function

        <Fact>
        Public Async Function TestRuleSetPathMapping() As Task
            Const RuleSetContents = "<?xml version=""1.0""?>
<RuleSet Name=""Name"" ToolsVersion=""10.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1001"" Action=""Warning"" />
  </Rules>
</RuleSet>"

            Using ruleSet = New DisposableFile(extension:=".ruleset")
                ruleSet.WriteAllText(RuleSetContents)

                ' We will test that if we redirect the ruleset to the temporary file that we wrote that the values are still read.
                Dim project = Await Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.CompilerInvocation.CreateFromJsonAsync("
                    {
                        ""tool"": ""csc"",
                        ""arguments"": ""/noconfig /nowarn:1701,1702 /fullpaths /define:DEBUG /ruleset:F:\\Ruleset.ruleset /out:Output.dll"",
                        ""projectFilePath"": ""F:\\Project.csproj"",
                        ""sourceRootPath"": ""F:\\"",
                        ""pathMappings"": [
                             {
                                 ""from"": ""F:\\Ruleset.ruleset"",
                                 ""to"": """ + ruleSet.Path.Replace("\", "\\") + """
                             }]
                    }")

                Dim compilation = Await project.GetCompilationAsync()
                Assert.Equal(ReportDiagnostic.Warn, compilation.Options.SpecificDiagnosticOptions("CA1001"))
            End Using
        End Function

        <Fact>
        Public Async Function TestSourceGeneratorOutputIncludedInCompilation() As Task
            Dim sourceGeneratorLocation = GetType(TestSourceGenerator.HelloWorldGenerator).Assembly.Location

            Dim project = Await CompilerInvocation.CreateFromJsonAsync("
                    {
                        ""tool"": ""csc"",
                        ""arguments"": ""/noconfig /analyzer:\""" + sourceGeneratorLocation.Replace("\", "\\") + "\""  /out:Output.dll"",
                        ""projectFilePath"": ""F:\\Project.csproj"",
                        ""sourceRootPath"": ""F:\\""
                    }")

            Dim compilation = Await project.GetCompilationAsync()
            Dim generatedTrees = compilation.SyntaxTrees

            Assert.Single(generatedTrees, Function(t) t.FilePath.EndsWith(TestSourceGenerator.HelloWorldGenerator.GeneratedEnglishClassName + ".cs"))
            Assert.Single(generatedTrees, Function(t) t.FilePath.EndsWith(TestSourceGenerator.HelloWorldGenerator.GeneratedSpanishClassName + ".cs"))
        End Function
    End Class
End Namespace
