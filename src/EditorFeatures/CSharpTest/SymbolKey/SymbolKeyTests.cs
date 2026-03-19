// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId;

[UseExportProvider]
public sealed class SymbolKeyTests
{
    [Fact]
    public async Task FileType_01()
    {
        var typeSource = """
            file class C1
            {
                public static void M() { }
            }
            """;

        var workspaceXml = $"""
            <Workspace>
                <Project Language="C#">
                    <CompilationOptions Nullable="Enable"/>
                    <Document FilePath="C.cs">
            {typeSource}
                    </Document>
                </Project>
            </Workspace>
            """;
        using var workspace = EditorTestWorkspace.Create(workspaceXml);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var compilation = await project.GetCompilationAsync();
        var type = compilation.GlobalNamespace.GetMembers("C1").Single();
        Assert.NotNull(type);
        var symbolKey = SymbolKey.Create(type);
        var resolved = symbolKey.Resolve(compilation).Symbol;
        Assert.Same(type, resolved);
    }

    [Fact]
    public async Task FileType_02()
    {
        var workspaceXml = $$"""
<Workspace>
    <Project Language="C#">
        <CompilationOptions Nullable="Enable"/>
        <Document FilePath="File0.cs">
file class C
{
    public static void M() { }
}
        </Document>
        <Document FilePath="File1.cs">
file class C
{
    public static void M() { }
}
        </Document>
    </Project>
</Workspace>
""";
        using var workspace = EditorTestWorkspace.Create(workspaceXml);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var compilation = await project.GetCompilationAsync();

        var members = compilation.GlobalNamespace.GetMembers("C").ToArray();
        Assert.Equal(2, members.Length);

        var type = members[0];
        Assert.NotNull(type);
        var symbolKey = SymbolKey.Create(type);
        var resolved = symbolKey.Resolve(compilation).Symbol;
        Assert.Same(type, resolved);

        type = members[1];
        Assert.NotNull(type);
        symbolKey = SymbolKey.Create(type);
        resolved = symbolKey.Resolve(compilation).Symbol;
        Assert.Same(type, resolved);
    }

    [Fact]
    public async Task FileType_03()
    {
        var workspaceXml = $$"""
<Workspace>
    <Project Language="C#">
        <CompilationOptions Nullable="Enable"/>
        <Document FilePath="File0.cs">
file class C
{
    public class Inner { }
}
        </Document>
    </Project>
</Workspace>
""";
        using var workspace = EditorTestWorkspace.Create(workspaceXml);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var compilation = await project.GetCompilationAsync();

        var type = compilation.GlobalNamespace.GetMembers("C").Single().GetMembers("Inner").Single();
        Assert.NotNull(type);
        var symbolKey = SymbolKey.Create(type);
        var resolved = symbolKey.Resolve(compilation).Symbol;
        Assert.Same(type, resolved);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45437")]
    public async Task TestGenericsAndNullability()
    {
        var typeSource = """
            #nullable enable

                public sealed class ConditionalWeakTableTest<TKey, TValue> /*: IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable*/
                    where TKey : class
                    where TValue : class
                {
                    public ConditionalWeakTable() { }
                    public void Add(TKey key, TValue value) { }
                    public void AddOrUpdate(TKey key, TValue value) { }
                    public void Clear() { }
                    public TValue GetOrCreateValue(TKey key) => default;
                    public TValue GetValue(TKey key, ConditionalWeakTableTest<TKey, TValue>.CreateValueCallback createValueCallback) => default;
                    public bool Remove(TKey key) => false;

                    public delegate TValue CreateValueCallback(TKey key);
                }
            """.Replace("<", "&lt;").Replace(">", "&gt;");

        var workspaceXml = $"""
            <Workspace>
                <Project Language="C#">
                    <CompilationOptions Nullable="Enable"/>
                    <Document FilePath="C.cs">
            {typeSource}
                    </Document>
                </Project>
            </Workspace>
            """;
        using var workspace = EditorTestWorkspace.Create(workspaceXml);

        var solution = workspace.CurrentSolution;
        var project = solution.Projects.Single();

        var compilation = await project.GetCompilationAsync();

        var type = compilation.GetTypeByMetadataName("ConditionalWeakTableTest`2");
        var method = type.GetMembers("GetValue").OfType<IMethodSymbol>().Single();
        var callbackParamater = method.Parameters[1];
        var parameterType = callbackParamater.Type;
        Assert.Equal("global::ConditionalWeakTableTest<TKey!, TValue!>.CreateValueCallback!", parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)));

        var symbolKey = SymbolKey.Create(method);
        var resolved = symbolKey.Resolve(compilation).Symbol;

        Assert.Equal(method, resolved);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1178861")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1192188")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1192486")]
    public async Task ResolveBodySymbolsInMultiProjectReferencesToOriginalProjectAsync()
    {
        var random = new Random(Seed: 0);

        // try to trigger race caused by ability to find reference in multiple potential projects depending on what
        // order things are in in internal collections.  This test was always able to hit the issue prior to the fix
        // going in, but does not hit it with the fix.  While this doesn't prove the race is gone, it strongly
        // implies it.
        for (var i = 0; i < 100; i++)
        {
            using var workspace = GetWorkspace();
            var solution = workspace.CurrentSolution;

            var bodyProject = solution.Projects.Single(p => p.AssemblyName == "BodyProject");
            var referenceProject = solution.Projects.Single(p => p.AssemblyName == "ReferenceProject");

            var (bodyCompilation, referenceCompilation) = await GetCompilationsAsync(bodyProject, referenceProject);
            var (bodyLocalSymbol, referenceAssemblySymbol) = await GetSymbolsAsync(bodyCompilation, referenceCompilation);

            var (bodyLocalProjectId, referenceAssemblyProjectId) = GetOriginatingProjectIds(solution, bodyLocalSymbol, referenceAssemblySymbol);

            Assert.True(bodyProject.Id == bodyLocalProjectId, $"Expected {bodyProject.Id} == {bodyLocalProjectId}. {i}");
            Assert.Equal(referenceProject.Id, referenceAssemblyProjectId);
        }

        return;

        TestWorkspace GetWorkspace()
        {
            var bodyProject = """
                    <Project Language="C#" AssemblyName="BodyProject" CommonReferences="true">
                        <Document>
                class Program
                {
                    void M()
                    {
                        int local;
                    }
                }
                        </Document>
                    </Project>
                """;
            var referenceProject = """
                <Project Language="C#" AssemblyName="ReferenceProject" CommonReferences="true">
                    <ProjectReference>BodyProject</ProjectReference>
                    <Document>
                    </Document>
                </Project>
                """;

            // Randomize the order of the projects in the workspace.
            if (random.Next() % 2 == 0)
            {
                return TestWorkspace.CreateWorkspace(XElement.Parse($"""
                    <Workspace>
                        {bodyProject}
                        {referenceProject}
                    </Workspace>
                    """));
            }
            else
            {
                return TestWorkspace.CreateWorkspace(XElement.Parse($"""
                    <Workspace>
                        {referenceProject}
                        {bodyProject}
                    </Workspace>
                    """));
            }
        }

        async Task<(Compilation bodyCompilation, Compilation referenceCompilation)> GetCompilationsAsync(Project bodyProject, Project referenceProject)
        {
            // Randomize the order that we get compilations (and thus populate our internal caches).
            Compilation bodyCompilation, referenceCompilation;
            if (random.Next() % 2 == 0)
            {
                bodyCompilation = await bodyProject.GetCompilationAsync();
                referenceCompilation = await referenceProject.GetCompilationAsync();
            }
            else
            {
                referenceCompilation = await referenceProject.GetCompilationAsync();
                bodyCompilation = await bodyProject.GetCompilationAsync();
            }

            return (bodyCompilation, referenceCompilation);
        }

        async Task<(ISymbol bodyLocalSymbol, ISymbol referenceAssemblySymbol)> GetSymbolsAsync(Compilation bodyCompilation, Compilation referenceCompilation)
        {
            // Randomize the order that we get symbols from each project.
            ISymbol bodyLocalSymbol, referenceAssemblySymbol;
            if (random.Next() % 2 == 0)
            {
                bodyLocalSymbol = await GetBodyLocalSymbol(bodyCompilation);
                referenceAssemblySymbol = referenceCompilation.Assembly;
            }
            else
            {
                referenceAssemblySymbol = referenceCompilation.Assembly;
                bodyLocalSymbol = await GetBodyLocalSymbol(bodyCompilation);
            }

            return (bodyLocalSymbol, referenceAssemblySymbol);
        }

        async Task<ILocalSymbol> GetBodyLocalSymbol(Compilation bodyCompilation)
        {
            var tree = bodyCompilation.SyntaxTrees.Single();
            var semanticModel = bodyCompilation.GetSemanticModel(tree);

            var root = await tree.GetRootAsync();
            var varDecl = root.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>().Single();

            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(varDecl);
            Assert.NotNull(local);

            return local;
        }

        (ProjectId bodyLocalProjectId, ProjectId referenceAssemblyProjectId) GetOriginatingProjectIds(Solution solution, ISymbol bodyLocalSymbol, ISymbol referenceAssemblySymbol)
        {
            // Randomize the order that we get try to get the originating project for the symbol.
            ProjectId bodyLocalProjectId, referenceAssemblyProjectId;
            if (random.Next() % 2 == 0)
            {
                bodyLocalProjectId = solution.GetOriginatingProjectId(bodyLocalSymbol);
                referenceAssemblyProjectId = solution.GetOriginatingProjectId(referenceAssemblySymbol);
            }
            else
            {
                referenceAssemblyProjectId = solution.GetOriginatingProjectId(referenceAssemblySymbol);
                bodyLocalProjectId = solution.GetOriginatingProjectId(bodyLocalSymbol);
            }

            return (bodyLocalProjectId, referenceAssemblyProjectId);
        }
    }
}
