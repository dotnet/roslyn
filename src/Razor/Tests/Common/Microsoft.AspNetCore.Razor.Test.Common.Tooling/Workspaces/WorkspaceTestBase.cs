// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor.Workspaces;

using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

public abstract class WorkspaceTestBase(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private bool _initialized;
    private HostServices? _hostServices;
    private Workspace? _workspace;
    private IWorkspaceProvider? _workspaceProvider;
    private LanguageServerFeatureOptions? _languageServerFeatureOptions;

    protected HostServices HostServices
    {
        get
        {
            EnsureInitialized();
            return _hostServices;
        }
    }

    protected Workspace Workspace
    {
        get
        {
            EnsureInitialized();
            return _workspace;
        }
    }

    private protected IWorkspaceProvider WorkspaceProvider
    {
        get
        {
            EnsureInitialized();
            return _workspaceProvider;
        }
    }

    private protected LanguageServerFeatureOptions LanguageServerFeatureOptions
    {
        get
        {
            EnsureInitialized();
            return _languageServerFeatureOptions;
        }
    }

    protected virtual void ConfigureWorkspace(AdhocWorkspace workspace)
    {
    }

    protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
    }

    [MemberNotNull(
        nameof(_hostServices),
        nameof(_workspace),
        nameof(_workspaceProvider),
        nameof(_languageServerFeatureOptions))]
    private void EnsureInitialized()
    {
        if (_initialized)
        {
            _hostServices.AssumeNotNull();
            _workspace.AssumeNotNull();
            _workspaceProvider.AssumeNotNull();
            _languageServerFeatureOptions.AssumeNotNull();
            return;
        }

        _hostServices = MefHostServices.DefaultHost;
        _workspace = TestWorkspace.Create(_hostServices, ConfigureWorkspace);
        AddDisposable(_workspace);
        _workspaceProvider = new TestWorkspaceProvider(_workspace);
        _languageServerFeatureOptions = TestLanguageServerFeatureOptions.Instance;
        _initialized = true;
    }

    /// <summary>
    ///  Calls <see cref="Workspace.TryApplyChanges(Solution)"/> and waits for <see cref="Workspace.WorkspaceChanged"/>
    ///  to stop firing events.
    /// </summary>
    protected Task<bool> UpdateWorkspaceAsync(Solution solution)
    {
        return Task.Run(
            async () =>
            {
                var currentCount = 0;

                using var _ = Workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);

                if (!Workspace.TryApplyChanges(solution))
                {
                    return false;
                }

                int lastCount;

                do
                {
                    lastCount = currentCount;
                    await Task.Delay(50);
                }
                while (lastCount != currentCount);

                return true;

                void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
                {
                    currentCount++;
                }
            },
            DisposalToken);
    }

    private sealed class TestWorkspaceProvider(Workspace workspace) : IWorkspaceProvider
    {
        public Workspace GetWorkspace() => workspace;
    }
}
