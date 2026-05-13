// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Shell;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

[LogIntegrationTest]
public abstract class AbstractRazorEditorTest(ITestOutputHelper testOutput) : AbstractIntegrationTest
{
    private readonly ITestOutputHelper _testOutput = testOutput;
    private ILogger? _testLogger;
    private string? _projectFilePath;

    protected virtual bool ComponentClassificationExpected => true;

    protected virtual string TargetFramework => "net10.0";

    protected virtual string TargetFrameworkElement => $"""<TargetFramework>{TargetFramework}</TargetFramework>""";

    protected virtual string ProjectZipFile => "Microsoft.VisualStudio.Razor.IntegrationTests.TestFiles.BlazorProject.zip";

    private protected virtual ILogger Logger => _testLogger.AssumeNotNull();

    protected string ProjectFilePath => _projectFilePath.AssumeNotNull();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _testLogger = await TestServices.Output.SetupIntegrationTestLoggerAsync(_testOutput, ControlledHangMitigatingCancellationToken);

        _testLogger.LogInformation($"#### Razor integration test initialize.");

        VisualStudioLogging.AddCustomLoggers();

        await TestServices.Shell.ResetEnvironmentAsync(ControlledHangMitigatingCancellationToken);

        _projectFilePath = await CreateAndOpenBlazorProjectAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        // We open the Index.razor file, and wait for 3 RazorComponentElement's to be classified, as that
        // way we know the LSP server is up, running, and has processed both local and library-sourced Components
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, ControlledHangMitigatingCancellationToken);

        // Razor extension doesn't launch until a razor file is opened, so wait for it to equalize
        await TestServices.Workspace.WaitForProjectSystemAsync(ControlledHangMitigatingCancellationToken);

        await EnsureTextViewRolesAsync(ControlledHangMitigatingCancellationToken);
        await EnsureExtensionInstalledAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("</PageTitle>", charsOffset: 1, ControlledHangMitigatingCancellationToken);

        if (ComponentClassificationExpected)
        {
            await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);
        }

        // Making a code change gets us flowing new generated code versions around the system
        // which seems to have a positive effect on Web Tools in particular. Given the relatively
        // fast pace of running integration tests, it's worth taking a slight delay at the start for a more reliable run.
        TestServices.Input.Send("{ENTER}");

        // Close the file we opened, just in case, so the test can start with a clean slate
        await TestServices.Editor.CloseCodeFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.IndexRazorFile, saveFile: false, ControlledHangMitigatingCancellationToken);

        _testLogger.LogInformation($"#### Razor integration test initialize finished.");
    }

    private async Task<string> CreateAndOpenBlazorProjectAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.SolutionExplorer.CloseSolutionAndWaitAsync(cancellationToken);

        var solutionPath = CreateTemporaryPath();

        using var zipStream = typeof(AbstractRazorEditorTest).Assembly.GetManifestResourceStream(ProjectZipFile);
        using var zip = new ZipArchive(zipStream);
        zip.ExtractToDirectory(solutionPath);

        var slnFile = Directory.EnumerateFiles(solutionPath, "*.sln").Single();

        foreach (var projectFile in Directory.EnumerateFiles(solutionPath, "*.csproj", SearchOption.AllDirectories))
        {
            PrepareProjectForFirstOpen(projectFile);
        }

        await TestServices.SolutionExplorer.OpenSolutionAsync(slnFile, cancellationToken);

        return Directory.EnumerateFiles(solutionPath, $"{RazorProjectConstants.BlazorProjectName}.csproj", SearchOption.AllDirectories).Single();
    }

    protected virtual void PrepareProjectForFirstOpen(string projectFileName)
    {
        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines(projectFileName))
        {
            if (line.Contains("<TargetFramework"))
            {
                sb.AppendLine(TargetFrameworkElement);
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        File.WriteAllText(projectFileName, sb.ToString());
    }

    private static string CreateTemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "razor-test", Path.GetRandomFileName());
    }

    public override async Task DisposeAsync()
    {
        _testLogger!.LogInformation($"#### Razor integration test dispose.");

        using (var disposeSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
        {
            await TestServices.Shell.CloseEverythingAsync(disposeSource.Token);
        }

        TestServices.Output.ClearIntegrationTestLogger();

        await base.DisposeAsync();
    }

    private async Task EnsureTextViewRolesAsync(CancellationToken cancellationToken)
    {
        var textView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var contentType = textView.TextSnapshot.ContentType;
        Assert.Equal("Razor", contentType.TypeName);
    }

    private async Task EnsureExtensionInstalledAsync(CancellationToken cancellationToken)
    {
        const string AssemblyName = "Microsoft.CodeAnalysis.Razor.Workspaces";
        using var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(cancellationToken);

        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

        var localAppData = Environment.GetEnvironmentVariable("LocalAppData");
        Assembly? assembly = null;
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            assembly = assemblies.FirstOrDefault((assembly) => assembly.GetName().Name.Equals(AssemblyName));
            if (assembly is null)
            {
                await semaphore.WaitAsync(cancellationToken);
            }

            semaphore.Release();
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
        }

        if (assembly is null)
        {
            throw new NotImplementedException($"Integration test did not load extension");
        }

        if (!assembly.Location.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase))
        {
            var version = assembly.GetName().Version;
            throw new NotImplementedException($"Integration test not running against Experimental Extension assembly: {assembly.Location} version: {version}");
        }

        void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName().Name.Equals(AssemblyName, StringComparison.Ordinal))
            {
                assembly = args.LoadedAssembly;
                semaphore.Release();
            }
        }
    }
}
