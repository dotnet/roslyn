﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    public class DiagnosticDataSerializerTests : TestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task SerializationTest_Document()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures.AddParts(
                typeof(TestPersistentStorageServiceFactory)));

            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", "");

            var diagnostics = new[]
            {
                new DiagnosticData(
                    id: "test1",
                    category: "Test",
                    message: "test1 message",
                    enuMessageForBingSearch: "test1 message format",
                    severity: DiagnosticSeverity.Info,
                    defaultSeverity:DiagnosticSeverity.Info,
                    isEnabledByDefault: false,
                    warningLevel: 1,
                    customTags: ImmutableArray<string>.Empty,
                    properties: ImmutableDictionary<string, string>.Empty,
                    document.Project.Id,
                    new DiagnosticDataLocation(document.Id, new TextSpan(10, 20), "originalFile1", 30, 30, 40, 40, "mappedFile1", 10, 10, 20, 20),
                    language: LanguageNames.CSharp),

                new DiagnosticData(
                    id: "test2",
                    category: "Test",
                    message: "test2 message",
                    enuMessageForBingSearch: "test2 message format",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray.Create("Test2"),
                    properties: ImmutableDictionary<string, string>.Empty.Add("propertyKey", "propertyValue"),
                    document.Project.Id,
                    new DiagnosticDataLocation(document.Id, new TextSpan(30, 40), "originalFile2", 70, 70, 80, 80, "mappedFile2", 50, 50, 60, 60),
                    language: "VB",
                    title: "test2 title",
                    description: "test2 description",
                    helpLink: "http://test2link"),

                new DiagnosticData(
                    id: "test3",
                    category: "Test",
                    message: "test3 message",
                    enuMessageForBingSearch: "test3 message format",
                    severity:DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 2,
                    customTags: ImmutableArray.Create("Test3", "Test3_2"),
                    properties: ImmutableDictionary<string, string>.Empty.Add("p1Key", "p1Value").Add("p2Key", "p2Value"),
                    document.Project.Id,
                    new DiagnosticDataLocation(document.Id, new TextSpan(50, 60), "originalFile3", 110, 110, 120, 120, "mappedFile3", 90, 90, 100, 100),
                    title: "test3 title",
                    description: "test3 description",
                    helpLink: "http://test3link"),

            }.ToImmutableArray();

            var utcTime = DateTime.UtcNow;
            var analyzerVersion = VersionStamp.Create(utcTime);
            var version = VersionStamp.Create(utcTime.AddDays(1));

            var key = "document";

            var persistentService = workspace.Services.GetRequiredService<IPersistentStorageService>();
            var serializer = new CodeAnalysis.Workspaces.Diagnostics.DiagnosticDataSerializer(analyzerVersion, version);

            Assert.True(await serializer.SerializeAsync(persistentService, document.Project, document, key, diagnostics, CancellationToken.None).ConfigureAwait(false));

            var recovered = await serializer.DeserializeAsync(persistentService, document.Project, document, key, CancellationToken.None);

            AssertDiagnostics(diagnostics, recovered);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task SerializationTest_Project()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures.AddParts(
                typeof(TestPersistentStorageServiceFactory)));

            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", "");

            var diagnostics = new[]
            {
                new DiagnosticData(
                    id: "test1",
                    category: "Test",
                    message: "test1 message",
                    enuMessageForBingSearch: "test1 message format",
                    severity: DiagnosticSeverity.Info,
                    defaultSeverity: DiagnosticSeverity.Info,
                    isEnabledByDefault: false,
                    warningLevel: 1,
                    customTags: ImmutableArray<string>.Empty,
                    properties: ImmutableDictionary<string, string>.Empty,
                    projectId: document.Project.Id,
                    language: LanguageNames.VisualBasic,
                    description: "test1 description",
                    helpLink: "http://test1link"),

                new DiagnosticData(
                    id: "test2",
                    category: "Test",
                    message: "test2 message",
                    enuMessageForBingSearch: "test2 message format",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray.Create("Test2"),
                    properties: ImmutableDictionary<string, string>.Empty.Add("p1Key", "p2Value"),
                    projectId: document.Project.Id),

                new DiagnosticData(
                    id: "test3",
                    category: "Test",
                    message: "test3 message",
                    enuMessageForBingSearch: "test3 message format",
                    severity: DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 2,
                    customTags: ImmutableArray.Create("Test3", "Test3_2"),
                    properties: ImmutableDictionary<string, string>.Empty.Add("p2Key", "p2Value").Add("p1Key", "p1Value"),
                    projectId: document.Project.Id,
                    description: "test3 description",
                    helpLink: "http://test3link"),

            }.ToImmutableArray();

            var utcTime = DateTime.UtcNow;
            var analyzerVersion = VersionStamp.Create(utcTime);
            var version = VersionStamp.Create(utcTime.AddDays(1));

            var key = "project";
            var persistentService = workspace.Services.GetRequiredService<IPersistentStorageService>();
            var serializer = new CodeAnalysis.Workspaces.Diagnostics.DiagnosticDataSerializer(analyzerVersion, version);

            Assert.True(await serializer.SerializeAsync(persistentService, document.Project, document, key, diagnostics, CancellationToken.None).ConfigureAwait(false));
            var recovered = await serializer.DeserializeAsync(persistentService, document.Project, document, key, CancellationToken.None);

            AssertDiagnostics(diagnostics, recovered);
        }

        [WorkItem(6104, "https://github.com/dotnet/roslyn/issues/6104")]
        [Fact]
        public void DiagnosticEquivalence()
        {
#if DEBUG
            var source =
@"class C
{
    static int F(string s) { return 1; }
    static int x = F(new { });
    static int y = F(new { A = 1 });
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false);
            var compilation = CSharpCompilation.Create(GetUniqueName(), new[] { tree }, new[] { MscorlibRef }, options);
            var model = compilation.GetSemanticModel(tree);

            // Each call to GetDiagnostics will bind field initializers
            // (see https://github.com/dotnet/roslyn/issues/6264).
            var diagnostics1 = model.GetDiagnostics().ToArray();
            var diagnostics2 = model.GetDiagnostics().ToArray();

            diagnostics1.Verify(
                // (4,22): error CS1503: Argument 1: cannot convert from '<empty anonymous type>' to 'string'
                //     static int x = F(new { });
                Diagnostic(1503, "new { }").WithArguments("1", "<empty anonymous type>", "string").WithLocation(4, 22),
                // (5,22): error CS1503: Argument 1: cannot convert from '<anonymous type: int A>' to 'string'
                //     static int y = F(new { A = 1 });
                Diagnostic(1503, "new { A = 1 }").WithArguments("1", "<anonymous type: int A>", "string").WithLocation(5, 22));

            Assert.NotSame(diagnostics1[0], diagnostics2[0]);
            Assert.NotSame(diagnostics1[1], diagnostics2[1]);
            Assert.Equal(diagnostics1, diagnostics2);
            Assert.True(AnalyzerHelper.AreEquivalent(diagnostics1, diagnostics2));

            // Verify that not all collections are treated as equivalent.
            diagnostics1 = new[] { diagnostics1[0] };
            diagnostics2 = new[] { diagnostics2[1] };

            Assert.NotEqual(diagnostics1, diagnostics2);
            Assert.False(AnalyzerHelper.AreEquivalent(diagnostics1, diagnostics2));
#endif
        }

        private static void AssertDiagnostics(ImmutableArray<DiagnosticData> items1, ImmutableArray<DiagnosticData> items2)
        {
            Assert.Equal(items1.Length, items2.Length);

            for (var i = 0; i < items1.Length; i++)
            {
                AssertDiagnostics(items1[i], items2[i]);
            }
        }

        private static void AssertDiagnostics(DiagnosticData item1, DiagnosticData item2)
        {
            Assert.Equal(item1.Id, item2.Id);
            Assert.Equal(item1.Category, item2.Category);
            Assert.Equal(item1.Message, item2.Message);
            Assert.Equal(item1.ENUMessageForBingSearch, item2.ENUMessageForBingSearch);
            Assert.Equal(item1.Severity, item2.Severity);
            Assert.Equal(item1.IsEnabledByDefault, item2.IsEnabledByDefault);
            Assert.Equal(item1.WarningLevel, item2.WarningLevel);
            Assert.Equal(item1.DefaultSeverity, item2.DefaultSeverity);

            Assert.Equal(item1.CustomTags.Length, item2.CustomTags.Length);
            for (var j = 0; j < item1.CustomTags.Length; j++)
                Assert.Equal(item1.CustomTags[j], item2.CustomTags[j]);

            Assert.Equal(item1.Properties.Count, item2.Properties.Count);
            Assert.True(item1.Properties.SetEquals(item2.Properties));

            Assert.Equal(item1.ProjectId, item2.ProjectId);
            Assert.Equal(item1.DocumentId, item2.DocumentId);

            Assert.Equal(item1.HasTextSpan, item2.HasTextSpan);
            if (item1.HasTextSpan)
            {
                Assert.Equal(item1.GetTextSpan(), item2.GetTextSpan());
            }

            Assert.Equal(item1.DataLocation?.MappedFilePath, item2.DataLocation?.MappedFilePath);
            Assert.Equal(item1.DataLocation?.MappedStartLine, item2.DataLocation?.MappedStartLine);
            Assert.Equal(item1.DataLocation?.MappedStartColumn, item2.DataLocation?.MappedStartColumn);
            Assert.Equal(item1.DataLocation?.MappedEndLine, item2.DataLocation?.MappedEndLine);
            Assert.Equal(item1.DataLocation?.MappedEndColumn, item2.DataLocation?.MappedEndColumn);

            Assert.Equal(item1.DataLocation?.OriginalFilePath, item2.DataLocation?.OriginalFilePath);
            Assert.Equal(item1.DataLocation?.OriginalStartLine, item2.DataLocation?.OriginalStartLine);
            Assert.Equal(item1.DataLocation?.OriginalStartColumn, item2.DataLocation?.OriginalStartColumn);
            Assert.Equal(item1.DataLocation?.OriginalEndLine, item2.DataLocation?.OriginalEndLine);
            Assert.Equal(item1.DataLocation?.OriginalEndColumn, item2.DataLocation?.OriginalEndColumn);

            Assert.Equal(item1.Description, item2.Description);
            Assert.Equal(item1.HelpLink, item2.HelpLink);
        }

        [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Test), Shared, PartNotDiscoverable]
        public class TestPersistentStorageServiceFactory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestPersistentStorageServiceFactory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new Service();

            public class Service : IPersistentStorageService
            {
                private readonly Storage _instance = new();

                IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
                    => _instance;

                ValueTask<IPersistentStorage> IPersistentStorageService.GetStorageAsync(Solution solution, CancellationToken cancellationToken)
                    => new(_instance);

                internal class Storage : IPersistentStorage
                {
                    private readonly Dictionary<object, Stream> _map = new();

                    public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default)
                    {
                        var stream = _map[name];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default)
                    {
                        var stream = _map[Tuple.Create(project, name)];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default)
                    {
                        var stream = _map[Tuple.Create(document, name)];
                        stream.Position = 0;

                        return Task.FromResult(stream);
                    }

                    public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default)
                    {
                        _map[name] = new MemoryStream();
                        stream.CopyTo(_map[name]);

                        return SpecializedTasks.True;
                    }

                    public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default)
                    {
                        _map[Tuple.Create(project, name)] = new MemoryStream();
                        stream.CopyTo(_map[Tuple.Create(project, name)]);

                        return SpecializedTasks.True;
                    }

                    public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default)
                    {
                        _map[Tuple.Create(document, name)] = new MemoryStream();
                        stream.CopyTo(_map[Tuple.Create(document, name)]);

                        return SpecializedTasks.True;
                    }

                    public void Dispose()
                    {
                    }

                    public ValueTask DisposeAsync()
                    {
                        return ValueTaskFactory.CompletedTask;
                    }
                }
            }
        }
    }
}
