// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.UnitTests;

public sealed class RazorAnalyzerAssemblyResolverTests : IDisposable
{
    private TempRoot TempRoot { get; } = new TempRoot();
    private ImmutableArray<string?> InitialAssemblies { get; }

    public RazorAnalyzerAssemblyResolverTests()
    {
        InitialAssemblies = AssemblyLoadContext.GetLoadContext(this.GetType().Assembly)!.Assemblies.SelectAsArray(a => a.FullName);
    }

    public void Dispose()
    {
        TempRoot.Dispose();

        // This test should not change the set of assemblies loaded in the current context.
        var count = AssemblyLoadContext.GetLoadContext(this.GetType().Assembly)!.Assemblies.SelectAsArray(a => a.FullName);
        AssertEx.SetEqual(InitialAssemblies, count);
    }

    private void CreateRazorAssemblies(string directory, string versionNumber = "1.0.0.0")
    {
        _ = Directory.CreateDirectory(directory);
        foreach (var simpleName in RazorAnalyzerAssemblyResolver.RazorAssemblyNames)
        {
            BuildOne(simpleName);
        }

        void BuildOne(string simpleName)
        {
            var i = simpleName.LastIndexOf('.');
            var typeName = simpleName[(i + 1)..];
            var source = $$"""
                using System.Reflection;

                [assembly: AssemblyVersion("{{versionNumber}}")]
                [assembly: AssemblyFileVersion("{{versionNumber}}")]

                public sealed class {{typeName}} { }
                """;

            var compilation = CSharpCompilation.Create(
                simpleName,
                [CSharpSyntaxTree.ParseText(source)],
                NetStandard20.References.All,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var result = compilation.Emit(Path.Combine(directory, $"{simpleName}.dll"));
            Assert.True(result.Success);
        }
    }

    private static void RunWithLoader(Action<RazorAnalyzerAssemblyResolver, AnalyzerAssemblyLoader, AssemblyLoadContext> action)
    {
        var compilerLoadContext = new AssemblyLoadContext("Compiler", isCollectible: true);
        var currentLoadContext = new AssemblyLoadContext("Current", isCollectible: true);
        var loader = new AnalyzerAssemblyLoader([], [AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver], compilerLoadContext);
#pragma warning disable 612 
        var resolver = CreateResolver();
#pragma warning restore 612 
        action(resolver, loader, currentLoadContext);

        Assert.Empty(currentLoadContext.Assemblies);
        currentLoadContext.Unload();
        compilerLoadContext.Unload();
    }

    [Obsolete]
    internal static RazorAnalyzerAssemblyResolver CreateResolver() => new RazorAnalyzerAssemblyResolver();

    /// <summary>
    /// When running in Visual Studio the razor generator will be redirected to the razor language 
    /// services directory. That will not contain all of the necessary DLLs. Anything that is a 
    /// platform DLL, like the object pool, will be in the VS platform directory. Need to fall back
    /// to the compiler context to find those.
    /// </summary>
    [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/79352")]
    public void FallbackToCompilerContext()
    {
        var dir1 = TempRoot.CreateDirectory().Path;
        CreateRazorAssemblies(dir1);
        var dir2 = TempRoot.CreateDirectory().Path;
        var fileName = $"{RazorAnalyzerAssemblyResolver.ObjectPoolAssemblyName}.dll";
        File.Move(Path.Combine(dir1, fileName), Path.Combine(dir2, fileName));

        RunWithLoader((resolver, loader, currentLoadContext) =>
        {
            Assembly? expectedAssembly = null;
            loader.CompilerLoadContext.Resolving += (context, name) =>
            {
                if (name.Name == RazorAnalyzerAssemblyResolver.ObjectPoolAssemblyName)
                {
                    expectedAssembly = context.LoadFromAssemblyPath(Path.Combine(dir2, fileName));
                    return expectedAssembly;
                }

                return null;
            };

            var actualAssembly = resolver.Resolve(
                loader,
                new AssemblyName(RazorAnalyzerAssemblyResolver.ObjectPoolAssemblyName),
                currentLoadContext,
                dir1);
            Assert.NotNull(expectedAssembly);
            Assert.NotNull(actualAssembly);
            Assert.Same(expectedAssembly, actualAssembly);
        });
    }

    [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/79352")]
    public void FirstLoadWins()
    {
        var dir1 = TempRoot.CreateDirectory().Path;
        CreateRazorAssemblies(dir1, versionNumber: "1.0.0.0");
        var dir2 = TempRoot.CreateDirectory().Path;
        CreateRazorAssemblies(dir2, versionNumber: "2.0.0.0");

        RunWithLoader((resolver, loader, currentLoadContext) =>
        {
            var assembly1 = resolver.Resolve(
                loader,
                new AssemblyName(RazorAnalyzerAssemblyResolver.RazorCompilerAssemblyName),
                currentLoadContext,
                dir1);
            var assembly2 = resolver.Resolve(
                loader,
                new AssemblyName(RazorAnalyzerAssemblyResolver.RazorCompilerAssemblyName),
                currentLoadContext,
                dir2);
            Assert.Same(assembly1, assembly2);
        });
    }

    [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/79352")]
    public void ChooseServiceHubFolder()
    {
        var dir = TempRoot.CreateDirectory().Path;
        CreateRazorAssemblies(dir);
        var serviceHubFolder = Path.Combine(dir, RazorAnalyzerAssemblyResolver.ServiceHubCoreFolderName);
        CreateRazorAssemblies(serviceHubFolder);

        coreTest(dir, serviceHubFolder);
        coreTest(dir + Path.DirectorySeparatorChar, serviceHubFolder);
        coreTest(serviceHubFolder, serviceHubFolder);
        coreTest(serviceHubFolder + Path.DirectorySeparatorChar, serviceHubFolder);

        void coreTest(string loadDir, string serviceHubDir)
        {
            var name = Path.GetFileName(loadDir.AsSpan());
            RunWithLoader((resolver, loader, currentLoadContext) =>
            {
                var assembly1 = resolver.Resolve(
                    loader,
                    new AssemblyName(RazorAnalyzerAssemblyResolver.RazorCompilerAssemblyName),
                    currentLoadContext,
                    loadDir);
                var assembly2 = resolver.Resolve(
                    loader,
                    new AssemblyName(RazorAnalyzerAssemblyResolver.RazorCompilerAssemblyName),
                    currentLoadContext,
                    serviceHubFolder);
                Assert.NotNull(assembly1);
                Assert.Same(assembly1, assembly2);
                Assert.Equal(serviceHubFolder, Path.GetDirectoryName(assembly1.Location));
            });
        }
    }
}
#endif
