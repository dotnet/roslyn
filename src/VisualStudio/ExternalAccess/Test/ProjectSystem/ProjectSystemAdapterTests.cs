// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;
using Moq;
using Xunit;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using LegacyImplementation = Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTests.ProjectSystem;

[UseExportProvider]
public class ProjectSystemAdapterTests
{
    [Fact]
    public void ExternalAccessProjectSystemContractsCanBeMocked()
    {
        var mock = new Mock<IWorkspaceProjectContext>(MockBehavior.Strict);
        mock.SetupGet(context => context.ProjectFilePath).Returns("project.csproj");

        Assert.Equal("project.csproj", mock.Object.ProjectFilePath);
    }

    [Fact]
    public async Task WorkspaceProjectContextAdapterForwardsCallsAndDisposesOnce()
    {
        var context = new FakeWorkspaceProjectContext();
        var adapter = new WorkspaceProjectContextAdapter(context);
        var cancellationToken = new CancellationTokenSource().Token;
        var folders = new[] { "Folder" };

        adapter.ProjectFilePath = "project.csproj";
        adapter.LastDesignTimeBuildSucceeded = true;
        adapter.BinOutputPath = "output.dll";
        adapter.DisplayName = "Project";
        adapter.IsPrimary = true;
        adapter.SetOptions(["/langversion:latest"]);
        adapter.SetProperty("Property", "Value");
        adapter.AddSourceFile("source.cs", false, folders, SourceCodeKind.Script);
        adapter.AddAdditionalFile("additional.txt", false);
        var scope = await adapter.CreateBatchScopeAsync(cancellationToken);

        Assert.Equal("project.csproj", context.ProjectFilePath);
        Assert.True(context.LastDesignTimeBuildSucceeded);
        Assert.Equal("output.dll", context.BinOutputPath);
        Assert.Equal("Project", context.DisplayName);
        Assert.True(context.IsPrimary);
        Assert.Equal("/langversion:latest", context.Options.Single());
        Assert.Equal(("Property", "Value"), context.Property);
        Assert.Equal(("source.cs", false, folders, SourceCodeKind.Script), context.SourceFile);
        Assert.Equal(("additional.txt", false), context.AdditionalFile);
        Assert.Same(context.BatchScope, scope);
        Assert.Equal(cancellationToken, context.BatchCancellationToken);

        adapter.Dispose();
        await adapter.DisposeAsync();
        Assert.Equal(1, context.DisposeCount);
        Assert.Equal(0, context.DisposeAsyncCount);

        var asyncContext = new FakeWorkspaceProjectContext();
        var asyncAdapter = new WorkspaceProjectContextAdapter(asyncContext);
        await asyncAdapter.DisposeAsync();
        await asyncAdapter.DisposeAsync();
        Assert.Equal(0, asyncContext.DisposeCount);
        Assert.Equal(1, asyncContext.DisposeAsyncCount);
    }

    [Fact]
    public async Task WorkspaceProjectContextFactoryAdapterForwardsEvaluationDataAndArguments()
    {
        var context = new FakeWorkspaceProjectContext();
        var factory = new FakeWorkspaceProjectContextFactory(context);
        var adapter = CreateImportingConstructor<WorkspaceProjectContextFactoryAdapter>(factory);
        var data = new TestEvaluationData();
        var hostObject = new object();
        using var cancellationSource = new CancellationTokenSource();

        var result = await adapter.CreateProjectContextAsync(
            Guid.NewGuid(),
            "UniqueName",
            LanguageNames.CSharp,
            data,
            hostObject,
            cancellationSource.Token);

        Assert.Equal("ProjectPath", factory.PropertyValue);
        Assert.Equal("ItemValue", factory.ItemValues.Single());
        Assert.Same(hostObject, factory.HostObject);
        Assert.Equal(cancellationSource.Token, factory.CancellationToken);
        Assert.IsType<WorkspaceProjectContextAdapter>(result);
        Assert.Equal(context.Id, result.Id);
    }

    [Fact]
    public async Task WorkspaceProjectContextFactoryAdapterIsDiscoverableThroughComposition()
    {
        var composition = TestComposition.Default.AddParts(
            typeof(WorkspaceProjectContextFactoryAdapter),
            typeof(ExportedLegacyWorkspaceProjectContextFactory));
        var exportProvider = composition.ExportProviderFactory.CreateExportProvider();
        var adapter = exportProvider.GetExportedValue<IWorkspaceProjectContextFactory>();
        var legacyFactory = exportProvider.GetExportedValue<ExportedLegacyWorkspaceProjectContextFactory>();
        using var cancellationSource = new CancellationTokenSource();

        var result = await adapter.CreateProjectContextAsync(
            Guid.NewGuid(),
            "UniqueName",
            LanguageNames.CSharp,
            new TestEvaluationData(),
            hostObject: null,
            cancellationSource.Token);

        Assert.IsType<WorkspaceProjectContextAdapter>(result);
        Assert.Equal("ProjectPath", legacyFactory.PropertyValue);
        Assert.Equal("ItemValue", legacyFactory.ItemValues.Single());
        Assert.Equal(cancellationSource.Token, legacyFactory.CancellationToken);
        Assert.Equal(legacyFactory.Context.Id, result.Id);
    }

    [Fact]
    public void CodeModelFactoryAdapterUnwrapsContextAndPassesArgumentsThrough()
    {
        var context = new FakeWorkspaceProjectContext();
        var factory = new FakeCodeModelFactory();
        var adapter = CreateImportingConstructor<CodeModelFactoryAdapter>(factory);
        var project = (EnvDTE.Project)null!;
        var item = (EnvDTE.ProjectItem)null!;
        var wrappedContext = new WorkspaceProjectContextAdapter(context);

        Assert.Null(adapter.GetCodeModel(wrappedContext, project));
        Assert.Null(adapter.GetFileCodeModel(wrappedContext, item));

        Assert.Same(context, factory.CodeModelContext);
        Assert.Same(project, factory.Project);
        Assert.Same(context, factory.FileCodeModelContext);
        Assert.Same(item, factory.ProjectItem);
    }

    [Fact]
    public async Task TempPECompilerAdapterUnwrapsContextAndPassesArgumentsThrough()
    {
        var context = new FakeWorkspaceProjectContext();
        var compiler = new FakeTempPECompiler();
        var adapter = CreateImportingConstructor<TempPECompilerAdapter>(compiler);
        var wrappedContext = new WorkspaceProjectContextAdapter(context);
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Form.cs" };
        using var cancellationSource = new CancellationTokenSource();

        Assert.True(await adapter.CompileAsync(wrappedContext, "output.dll", files, cancellationSource.Token));

        Assert.Same(context, compiler.Context);
        Assert.Equal("output.dll", compiler.OutputFileName);
        Assert.Same(files, compiler.FilesToInclude);
        Assert.Equal(cancellationSource.Token, compiler.CancellationToken);
    }

    [Fact]
    public void EntryPointFinderAdapterPreservesResultsAndFlag()
    {
        var result = Array.Empty<INamedTypeSymbol>();
        var service = new FakeEntryPointFinderService(result);
        var adapter = new EntryPointFinderServiceAdapter(service);
        var compilation = (Compilation)null!;

        Assert.Same(result, adapter.FindEntryPoints(compilation, findFormsOnly: true));
        Assert.Same(compilation, service.Compilation);
        Assert.True(service.FindFormsOnly);
    }

    [Fact]
    public void EntryPointFinderFactoriesPreservePerLanguageAndOptionalServiceBehavior()
    {
        var result = Array.Empty<INamedTypeSymbol>();
        var service = new FakeEntryPointFinderService(result);
        var csharpFactory = CreateImportingConstructor<CSharpEntryPointFinderServiceFactory>();
        var visualBasicFactory = CreateImportingConstructor<VisualBasicEntryPointFinderServiceFactory>();
        var languageServices = new FakeHostLanguageServices(service);

        var csharpService = Assert.IsType<EntryPointFinderServiceAdapter>(csharpFactory.CreateLanguageService(languageServices));
        var visualBasicService = Assert.IsType<EntryPointFinderServiceAdapter>(visualBasicFactory.CreateLanguageService(languageServices));
        Assert.Same(result, csharpService.FindEntryPoints((Compilation)null!, findFormsOnly: false));
        Assert.False(service.FindFormsOnly);
        Assert.Same(result, visualBasicService.FindEntryPoints((Compilation)null!, findFormsOnly: true));
        Assert.True(service.FindFormsOnly);

        var missingService = new FakeHostLanguageServices(service: null);
        Assert.Null(csharpFactory.CreateLanguageService(missingService));
        Assert.Null(visualBasicFactory.CreateLanguageService(missingService));
    }

    [Fact]
    public void EntryPointFinderLanguageServicesAreDiscoverableThroughComposition()
    {
        var composition = TestComposition.Default.AddParts(
            typeof(CSharpEntryPointFinderServiceFactory),
            typeof(VisualBasicEntryPointFinderServiceFactory),
            typeof(ExportedCSharpLegacyEntryPointFinderService),
            typeof(ExportedVisualBasicLegacyEntryPointFinderService));
        using var workspace = new AdhocWorkspace(composition.GetHostServices(), ServiceLayer.Host);

        var csharpService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IEntryPointFinderService>();
        var visualBasicService = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<IEntryPointFinderService>();

        Assert.IsType<EntryPointFinderServiceAdapter>(csharpService);
        Assert.IsType<EntryPointFinderServiceAdapter>(visualBasicService);
    }

    private static T CreateImportingConstructor<T>(params object?[] arguments)
        => (T)typeof(T).GetConstructors().Single().Invoke(arguments);

    private sealed class TestEvaluationData : EvaluationData
    {
        public override string GetPropertyValue(string name)
        {
            Assert.Equal("Property", name);
            return "ProjectPath";
        }

        public override ImmutableArray<string> GetItemValues(string name)
        {
            Assert.Equal("Item", name);
            return ["ItemValue"];
        }
    }

    private sealed class FakeWorkspaceProjectContext : Legacy.IWorkspaceProjectContext
    {
        public string DisplayName { get; set; } = "";
        public string? ProjectFilePath { get; set; }
        public Guid Guid { get; set; }
        public bool LastDesignTimeBuildSucceeded { get; set; }
        public string? BinOutputPath { get; set; }
        public bool IsPrimary { get; set; }
        public ProjectId Id { get; } = ProjectId.CreateNewId();
        public ImmutableArray<string> Options { get; private set; }
        public (string Name, string Value) Property { get; private set; }
        public (string Path, bool InContext, IEnumerable<string>? Folders, SourceCodeKind Kind) SourceFile { get; private set; }
        public (string Path, bool InContext) AdditionalFile { get; private set; }
        public FakeAsyncDisposable BatchScope { get; } = new();
        public CancellationToken BatchCancellationToken { get; private set; }
        public int DisposeCount { get; private set; }
        public int DisposeAsyncCount { get; private set; }

        public void SetOptions(string commandLineForOptions)
            => throw new NotImplementedException();

        public void SetOptions(ImmutableArray<string> arguments)
            => Options = arguments;

        public void SetProperty(string name, string value)
            => Property = (name, value);

        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
        }

        public void RemoveMetadataReference(string referencePath)
        {
        }

        public void AddProjectReference(Legacy.IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
        }

        public void RemoveProjectReference(Legacy.IWorkspaceProjectContext project)
        {
        }

        public void AddAnalyzerReference(string referencePath)
        {
        }

        public void RemoveAnalyzerReference(string referencePath)
        {
        }

        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string>? folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            => SourceFile = (filePath, isInCurrentContext, folderNames, sourceCodeKind);

        public void RemoveSourceFile(string filePath)
        {
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            => AdditionalFile = (filePath, isInCurrentContext);

        public void AddAdditionalFile(string filePath, IEnumerable<string> folderNames, bool isInCurrentContext = true)
        {
        }

        public void RemoveAdditionalFile(string filePath)
        {
        }

        public void AddDynamicFile(string filePath, IEnumerable<string>? folderNames = null)
        {
        }

        public void RemoveDynamicFile(string filePath)
        {
        }

        public void AddAnalyzerConfigFile(string filePath)
        {
        }

        public void RemoveAnalyzerConfigFile(string filePath)
        {
        }

        public ValueTask<IAsyncDisposable> CreateBatchScopeAsync(CancellationToken cancellationToken)
        {
            BatchCancellationToken = cancellationToken;
            return ValueTask.FromResult<IAsyncDisposable>(BatchScope);
        }

        public void ReorderSourceFiles(IEnumerable<string> filePaths)
        {
        }

        public void Dispose()
            => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class FakeWorkspaceProjectContextFactory(FakeWorkspaceProjectContext context) : Legacy.IWorkspaceProjectContextFactory
    {
        public string? PropertyValue { get; private set; }
        public ImmutableArray<string> ItemValues { get; private set; }
        public object? HostObject { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public ImmutableArray<string> EvaluationPropertyNames => [];
        public ImmutableArray<string> EvaluationItemNames => [];

        public Task<Legacy.IWorkspaceProjectContext> CreateProjectContextAsync(
            Guid id,
            string uniqueName,
            string languageName,
            Legacy.EvaluationData data,
            object? hostObject,
            CancellationToken cancellationToken)
        {
            PropertyValue = data.GetPropertyValue("Property");
            ItemValues = data.GetItemValues("Item");
            HostObject = hostObject;
            CancellationToken = cancellationToken;
            return Task.FromResult<Legacy.IWorkspaceProjectContext>(context);
        }
    }

    [Export(typeof(Legacy.IWorkspaceProjectContextFactory))]
    [Export(typeof(ExportedLegacyWorkspaceProjectContextFactory))]
    [Shared, PartNotDiscoverable]
    private sealed class ExportedLegacyWorkspaceProjectContextFactory : Legacy.IWorkspaceProjectContextFactory
    {
        public FakeWorkspaceProjectContext Context { get; } = new();
        public string? PropertyValue { get; private set; }
        public ImmutableArray<string> ItemValues { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public ImmutableArray<string> EvaluationPropertyNames => [];
        public ImmutableArray<string> EvaluationItemNames => [];

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedLegacyWorkspaceProjectContextFactory()
        {
        }

        public Task<Legacy.IWorkspaceProjectContext> CreateProjectContextAsync(
            Guid id,
            string uniqueName,
            string languageName,
            Legacy.EvaluationData data,
            object? hostObject,
            CancellationToken cancellationToken)
        {
            PropertyValue = data.GetPropertyValue("Property");
            ItemValues = data.GetItemValues("Item");
            CancellationToken = cancellationToken;
            return Task.FromResult<Legacy.IWorkspaceProjectContext>(Context);
        }
    }

    [ExportLanguageService(typeof(LegacyImplementation.IEntryPointFinderService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private sealed class ExportedCSharpLegacyEntryPointFinderService : LegacyImplementation.IEntryPointFinderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedCSharpLegacyEntryPointFinderService()
        {
        }

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
            => [];
    }

    [ExportLanguageService(typeof(LegacyImplementation.IEntryPointFinderService), LanguageNames.VisualBasic), Shared, PartNotDiscoverable]
    private sealed class ExportedVisualBasicLegacyEntryPointFinderService : LegacyImplementation.IEntryPointFinderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExportedVisualBasicLegacyEntryPointFinderService()
        {
        }

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
            => [];
    }

    private sealed class FakeCodeModelFactory : Legacy.ICodeModelFactory
    {
        public Legacy.IWorkspaceProjectContext? CodeModelContext { get; private set; }
        public EnvDTE.Project? Project { get; private set; }
        public Legacy.IWorkspaceProjectContext? FileCodeModelContext { get; private set; }
        public EnvDTE.ProjectItem? ProjectItem { get; private set; }

        public EnvDTE.FileCodeModel GetFileCodeModel(Legacy.IWorkspaceProjectContext context, EnvDTE.ProjectItem item)
        {
            FileCodeModelContext = context;
            ProjectItem = item;
            return null!;
        }

        public EnvDTE.CodeModel GetCodeModel(Legacy.IWorkspaceProjectContext context, EnvDTE.Project project)
        {
            CodeModelContext = context;
            Project = project;
            return null!;
        }
    }

    private sealed class FakeTempPECompiler : Legacy.ITempPECompiler
    {
        public Legacy.IWorkspaceProjectContext? Context { get; private set; }
        public string? OutputFileName { get; private set; }
        public ISet<string>? FilesToInclude { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<bool> CompileAsync(Legacy.IWorkspaceProjectContext context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken)
        {
            Context = context;
            OutputFileName = outputFileName;
            FilesToInclude = filesToInclude;
            CancellationToken = cancellationToken;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeEntryPointFinderService(IEnumerable<INamedTypeSymbol> results) : LegacyImplementation.IEntryPointFinderService
    {
        public Compilation? Compilation { get; private set; }
        public bool FindFormsOnly { get; private set; }

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
        {
            Compilation = compilation;
            FindFormsOnly = findFormsOnly;
            return results;
        }
    }

    private sealed class FakeHostLanguageServices(LegacyImplementation.IEntryPointFinderService? service) : HostLanguageServices
    {
        public override HostWorkspaceServices WorkspaceServices
            => throw new NotImplementedException();

        public override string Language
            => LanguageNames.CSharp;

        public override TLanguageService GetService<TLanguageService>()
            => (TLanguageService)(object)service!;
    }
}
