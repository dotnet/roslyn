// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ExportProviderBuilderTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task MefCompositionIsCached()
    {
        await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

        await AssertCacheWriteWasAttemptedAsync();

        AssertCachedCompositionCountEquals(expectedCount: 1);
    }

    [Fact]
    public async Task MefCompositionIsReused()
    {
        await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

        await AssertCacheWriteWasAttemptedAsync();

        // Second test server with the same set of assemblies.
        await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: false);

        AssertNoCacheWriteWasAttempted();

        AssertCachedCompositionCountEquals(expectedCount: 1);
    }

    [Fact]
    public async Task MultipleMefCompositionsAreCached()
    {
        await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

        await AssertCacheWriteWasAttemptedAsync();

        // Second test server with a different set of assemblies.
        await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: true);

        await AssertCacheWriteWasAttemptedAsync();

        AssertCachedCompositionCountEquals(expectedCount: 2);
    }

    [Theory, WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/1686")]
    // gen-delims
    [InlineData("#"),
    InlineData(":"),
    InlineData("?"),
    InlineData("["),
    InlineData("]"),
    InlineData("@"),
    // sub-delims
    InlineData("!"),
    InlineData("$"),
    InlineData("&"),
    InlineData("'"),
    InlineData("("),
    InlineData(")"),
    InlineData("*"),
    InlineData("+"),
    InlineData(","),
    InlineData(";"),
    InlineData("=")]
    public async Task CanFindCodeBaseWhenReservedCharactersInPath(string reservedCharacter)
    {
        // Test that given an unescaped code base URI (as vs-mef gives us), we can resolve the file path even if it contains reserved characters.

        // Certain characters aren't valid file paths on different file systems and so can't be in the path.
        if (ExecutionConditionUtil.IsWindows && reservedCharacter is "*" or ":" or "?")
        {
            return;
        }

        var dllPath = GenerateDll(reservedCharacter, out var assemblyName);

        await using var testServer = await TestLspServer.CreateAsync(new Roslyn.LanguageServer.Protocol.ClientCapabilities(), LoggerFactory, MefCacheDirectory.Path, includeDevKitComponents: true, [dllPath]);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembly = Assert.Single(assemblies, a => a.GetName().Name == assemblyName);
        var type = Assert.Single(assembly.GetTypes(), t => t.FullName?.Contains("ExportedType") == true);
        var values = testServer.ExportProvider.GetExportedValues(type, contractName: null);
        Assert.Single(values);
    }

    private string GenerateDll(string reservedCharacter, out string assemblyName)
    {
        var directory = TempRoot.CreateDirectory();
        CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);

        // Create a dll that exports and imports a mef type to ensure that MEF attempts to load and create a MEF graph
        // using this assembly.
        var source = """
            namespace MyTestExportNamespace
            {
                [System.ComponentModel.Composition.Export(typeof(ExportedType))]
                public class ExportedType { }

                public class ImportType
                {
                    [System.ComponentModel.Composition.ImportingConstructorAttribute]
                    public ImportType(ExportedType t) { }
                }
            }
            """;

        // Generate an assembly name associated with the character we're testing - this ensures
        // that if multiple of these tests run in the same process we're making sure that the correct expected assembly is loaded.
        assemblyName = "MyAssembly" + reservedCharacter.GetHashCode();
#pragma warning disable RS0030 // Do not use banned APIs - intentionally using System.ComponentModel.Composition to verify mef construction.
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [SyntaxFactory.ParseSyntaxTree(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default))],
            references:
            [
                NetStandard20.References.mscorlib,
                NetStandard20.References.netstandard,
                NetStandard20.References.SystemRuntime,
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location)
            ],
            options: options);
#pragma warning restore RS0030 // Do not use banned APIs

        // Write a dll to a subdir with a reserved character in the name.
        var tempSubDir = directory.CreateDirectory(reservedCharacter);
        var tempFile = tempSubDir.CreateFile($"{assemblyName}.dll");
        var dllData = compilation.EmitToStream();
        tempFile.WriteAllBytes(dllData.ToArray());

        // Mark the file as read only to prevent mutations.
        var fileInfo = new FileInfo(tempFile.Path);
        fileInfo.IsReadOnly = true;

        return tempFile.Path;
    }

    private async Task AssertCacheWriteWasAttemptedAsync()
    {
        var cacheWriteTask = LanguageServerExportProviderBuilder.TestAccessor.GetCacheWriteTask();
        Assert.NotNull(cacheWriteTask);

        await cacheWriteTask;
    }

    private void AssertNoCacheWriteWasAttempted()
    {
        var cacheWriteTask2 = LanguageServerExportProviderBuilder.TestAccessor.GetCacheWriteTask();
        Assert.Null(cacheWriteTask2);
    }

    private void AssertCachedCompositionCountEquals(int expectedCount)
    {
        var mefCompositions = Directory.EnumerateFiles(MefCacheDirectory.Path, "*.mef-composition", SearchOption.AllDirectories);

        Assert.Equal(expectedCount, mefCompositions.Count());
    }
}
