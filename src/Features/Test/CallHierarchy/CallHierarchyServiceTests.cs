// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CallHierarchy;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CallHierarchy)]
public sealed class CallHierarchyServiceTests
{
    [Fact]
    public async Task CreateItemAsync_ForVirtualMethod_ProvidesExpectedRelationships()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            public class Base
            {
                public virtual void $$M() { }
            }

            public class Derived : Base
            {
                public override void M() { }
            }

            class Caller
            {
                void N(Base b)
                {
                    b.M();
                }
            }
            """);

        var (_, _, item) = await GetItemAsync(workspace);

        Assert.Equal("M()", item.MemberName);
        Assert.Equal("Base", item.ContainingTypeName);
        AssertEx.SetEqual(
            [
                CallHierarchyRelationshipKind.Callers,
                CallHierarchyRelationshipKind.CallsToOverrides,
                CallHierarchyRelationshipKind.Overrides,
            ],
            item.SupportedSearchDescriptors.Select(static d => d.Relationship));
    }

    [Fact]
    public async Task SearchAsync_Callers_RespectsDocumentFilter()
    {
        using var workspace = TestWorkspace.Create("""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
        <Document>
namespace C
{
    public class CC
    {
        public int GetFive() { return 5; }
    }
}
        </Document>
        <Document>
using C;
namespace G
{
    public class G
    {
        public void G()
        {
            CC c = new CC();
            c.GetFive();
        }
    }
}
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using C;
public class D
{
    void bar()
    {
        var c = new C.CC();
        var d = c.Ge$$tFive();
    }
}
        </Document>
        <Document>
using C;
public class DSSS
{
    void bar()
    {
        var c = new C.CC();
        var d = c.GetFive();
    }
}
        </Document>
    </Project>
</Workspace>
""");

        var filteredDocument = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.Single(d => d.Name == "Test3.cs").Id);
        var results = await GetSearchResultsAsync(
            workspace,
            CallHierarchyRelationshipKind.Callers,
            documents: ImmutableHashSet.Create(filteredDocument));

        AssertEx.SetEqual(["D.bar()"], GetItemDisplayNames(results));
    }

    [Fact]
    public async Task SearchAsync_Implementations_FindsCrossProjectImplementations()
    {
        using var workspace = TestWorkspace.Create("""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
        <Document>
namespace C
{
    public interface I
    {
        void go$$o();
    }

    public class C : I
    {
        public void goo() { }
    }
}
        </Document>
        <Document>
using C;
namespace G
{
    public class G : I
    {
        public void goo()
        {
        }
    }
}
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using C;
public class D : I
{
    public void goo()
    {
    }
}
        </Document>
    </Project>
</Workspace>
""");

        var results = await GetSearchResultsAsync(workspace, CallHierarchyRelationshipKind.Implementations);

        AssertEx.SetEqual(["C.goo()", "D.goo()", "G.goo()"], GetItemDisplayNames(results));
    }

    [Fact]
    public async Task SearchAsync_FieldReferences_ProducesInitializerResult()
    {
        using var workspace = TestWorkspace.CreateCSharp("""
            class C
            {
                int $$f = 0;
                int g = f;
                int h = f + 1;

                void M()
                {
                    var value = f;
                }
            }
            """);

        var results = await GetSearchResultsAsync(workspace, CallHierarchyRelationshipKind.FieldReferences);

        AssertEx.SetEqual(["C.M()"], GetItemDisplayNames(results));

        var locationResult = Assert.Single(results.Where(r => r.Item is null));
        Assert.Equal(2, locationResult.ReferenceLocations.Length);
    }

    private static IEnumerable<string> GetItemDisplayNames(ImmutableArray<CallHierarchySearchResult> results)
        => results.Where(static r => r.Item is not null).Select(static r => GetDisplayName(r.Item!));

    private static string GetDisplayName(CallHierarchyItemDescriptor item)
        => string.IsNullOrEmpty(item.ContainingTypeName)
            ? item.MemberName
            : $"{item.ContainingTypeName}.{item.MemberName}";

    private static async Task<ImmutableArray<CallHierarchySearchResult>> GetSearchResultsAsync(
        TestWorkspace workspace,
        CallHierarchyRelationshipKind relationship,
        IImmutableSet<Document>? documents = null)
    {
        var (document, service, item) = await GetItemAsync(workspace);
        var searchDescriptor = Assert.Single(item.SupportedSearchDescriptors.Where(d => d.Relationship == relationship));
        return await service.SearchAsync(document.Project.Solution, searchDescriptor, documents, CancellationToken.None);
    }

    private static async Task<(Document Document, ICallHierarchyService Service, CallHierarchyItemDescriptor Item)> GetItemAsync(TestWorkspace workspace)
    {
        var hostDocument = workspace.DocumentWithCursor;
        var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, hostDocument.CursorPosition!.Value, cancellationToken: CancellationToken.None);
        Assert.NotNull(symbol);

        var service = document.GetRequiredLanguageService<ICallHierarchyService>();
        var item = await service.CreateItemAsync(symbol, document.Project, CancellationToken.None);
        Assert.NotNull(item);

        return (document, service, item);
    }
}
