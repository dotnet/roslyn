// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator(LanguageNames.CSharp)]
    internal class TestServicesSourceGenerator : IIncrementalGenerator
    {
        private const string SourceSuffix = ".g.cs";

        private const string IVsTextManagerExtensionsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextManagerExtensions
    {
        public static Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
            => textManager.GetActiveViewAsync(joinableTaskFactory, mustHaveFocus: true, buffer: null, cancellationToken);

        public static async Task<IVsTextView> GetActiveViewAsync(this IVsTextManager textManager, JoinableTaskFactory joinableTaskFactory, bool mustHaveFocus, IVsTextBuffer? buffer, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(fMustHaveFocus: mustHaveFocus ? 1 : 0, pBuffer: buffer, ppView: out var vsTextView));

            return vsTextView;
        }
    }
}
";

        private const string IVsTextViewExtensionsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    internal static partial class IVsTextViewExtensions
    {
        public static async Task<IWpfTextViewHost> GetTextViewHostAsync(this IVsTextView textView, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(((IVsUserData)textView).GetData(DefGuidList.guidIWpfTextViewHost, out var wpfTextViewHost));
            return (IWpfTextViewHost)wpfTextViewHost;
        }
    }
}
";

        private const string EditorInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;

    [TestService]
    internal partial class EditorInProcess
    {
        public async Task<IWpfTextView> GetActiveTextViewAsync(CancellationToken cancellationToken)
            => (await GetActiveTextViewHostAsync(cancellationToken)).TextView;

        private async Task<IWpfTextViewHost> GetActiveTextViewHostAsync(CancellationToken cancellationToken)
        {
            var activeVsTextView = await GetActiveVsTextViewAsync(cancellationToken);
            return await activeVsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
        }

        private async Task<IVsTextView> GetActiveVsTextViewAsync(CancellationToken cancellationToken)
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text
            // view.
            await WaitForApplicationIdleAsync(cancellationToken);

            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            return await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
        }
    }
}
";

        private const string ShellInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    [TestService]
    internal partial class ShellInProcess
    {
        public new Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {
            return base.GetRequiredGlobalServiceAsync<TService, TInterface>(cancellationToken);
        }

        public new Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {
            return base.GetComponentModelServiceAsync<TService>(cancellationToken);
        }

        public async Task<string> GetActiveWindowCaptionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
            ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
            var windowFrame = (IVsWindowFrame)windowFrameObj;

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
            return $""{captionObj}"";
        }

        public async Task<Version> GetVersionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

            var fullVersion = versionProperty?.ToString() ?? string.Empty;
            var firstSpace = fullVersion.IndexOf(' ');
            if (firstSpace >= 0)
            {
                // e.g. ""17.1.31907.60 MAIN""
                fullVersion = fullVersion.Substring(0, firstSpace);
            }

            if (Version.TryParse(fullVersion, out var version))
            {
                return version;
            }

            throw new NotSupportedException($""Unexpected version format: {versionProperty}"");
        }
    }
}
";

        private const string SolutionExplorerInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    [TestService]
    internal partial class SolutionExplorerInProcess
    {
        public async Task<bool> IsSolutionOpenAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        /// <summary>
        /// Close the currently open solution without saving.
        /// </summary>
        public async Task CloseSolutionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            if (!await IsSolutionOpenAsync(cancellationToken))
            {
                return;
            }

            using SemaphoreSlim semaphore = new SemaphoreSlim(1);
            await RunWithSolutionEventsAsync(
                async solutionEvents =>
                {
                    await semaphore.WaitAsync(cancellationToken);

                    void HandleAfterCloseSolution(object sender, EventArgs e)
                        => semaphore.Release();

                    solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
                    try
                    {
                        ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                        await semaphore.WaitAsync(cancellationToken);
                    }
                    finally
                    {
                        solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
                    }
                },
                cancellationToken);
        }

        private sealed partial class SolutionEvents : IVsSolutionEvents
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(JoinableTaskFactory joinableTaskFactory, IVsSolution solution)
            {
                Application.Current.Dispatcher.VerifyAccess();

                _joinableTaskFactory = joinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler? AfterCloseSolution;

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                AfterCloseSolution?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }
    }
}
";

        private const string WorkspaceInProcessSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    [TestService]
    internal partial class WorkspaceInProcess
    {
    }
}
";

        private const string SolutionExplorerInProcessSolutionEventsDisposeSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;

    internal partial class SolutionExplorerInProcess
    {
        private async Task RunWithSolutionEventsAsync(Func<SolutionEvents, Task> actionAsync, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);
            await actionAsync(solutionEvents);
        }

        private sealed partial class SolutionEvents : IDisposable
        {
            public void Dispose()
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                    ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
                });
            }
        }
    }
}
";

        private const string SolutionExplorerInProcessSolutionEventsDisposeAsyncSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncDisposable = System.IAsyncDisposable;

    internal partial class SolutionExplorerInProcess
    {
        private async Task RunWithSolutionEventsAsync(Func<SolutionEvents, Task> actionAsync, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            await using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);
            await actionAsync(solutionEvents);
        }

        private sealed partial class SolutionEvents : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
            }
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using Microsoft.VisualStudio.OperationProgress;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public async Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
        {
            var operationProgressStatus = await GetRequiredGlobalServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(cancellationToken);
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemPartialSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException(""Visual Studio 2019 version 16.0 includes SVsOperationProgress, but does not include IVsOperationProgressStatusService. Update Microsoft.VisualStudio.Shell.Framework to 16.1 or newer to support waiting for project system."");
        }
    }
}
";

        private const string WorkspaceInProcessWaitForProjectSystemLegacySource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
";

        private const string TestServiceAttributeSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class TestServiceAttribute : Attribute
    {
    }
}
";

        private const string ErrorHandlerSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio
{
    using System.Runtime.InteropServices;

    internal static class ErrorHandler
    {
        public static bool Succeeded(int hr)
            => hr >= 0;

        public static bool Failed(int hr)
            => hr < 0;

        public static int ThrowOnFailure(int hr)
        {
            if (Failed(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return hr;
        }
    }
}
";

        private const string VSConstantsSource = @"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio
{
    using System;

    internal static partial class VSConstants
    {
        // General HRESULTS

        /// <summary>HRESULT for FALSE (not an error).</summary>
        public const int S_FALSE = 0x00000001;
        /// <summary>HRESULT for generic success.</summary>
        public const int S_OK = 0x00000000;

        /// <summary>
        /// These element IDs are the only element IDs that can be used with the selection service.
        /// </summary>
        public enum VSSELELEMID
        {
            SEID_UndoManager = 0,
            SEID_WindowFrame = 1,
            SEID_DocumentFrame = 2,
            SEID_StartupProject = 3,
            SEID_PropertyBrowserSID = 4,
            SEID_UserContext = 5,
            SEID_ResultList = 6,
            SEID_LastWindowFrame = 7,
        }
    }
}
";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(context =>
            {
                context.AddSource($"IVsTextViewExtensions{SourceSuffix}", IVsTextViewExtensionsSource);
                context.AddSource($"IVsTextManagerExtensions{SourceSuffix}", IVsTextManagerExtensionsSource);
                context.AddSource($"EditorInProcess1{SourceSuffix}", EditorInProcessSource);
                context.AddSource($"SolutionExplorerInProcess1{SourceSuffix}", SolutionExplorerInProcessSource);
                context.AddSource($"ShellInProcess1{SourceSuffix}", ShellInProcessSource);
                context.AddSource($"WorkspaceInProcess1{SourceSuffix}", WorkspaceInProcessSource);
                context.AddSource($"TestServiceAttribute{SourceSuffix}", TestServiceAttributeSource);
            });

            var services = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, cancellationToken) =>
                {
                    if (node is AttributeSyntax attribute)
                    {
                        var unqualifiedName = GetUnqualifiedName(attribute.Name).Identifier.ValueText;
                        return unqualifiedName is "TestService" or "TestServiceAttribute";
                    }

                    return false;
                },
                transform: static (context, cancellationToken) =>
                {
                    var attribute = (AttributeSyntax)context.Node;

                    Accessibility accessibility;
                    string? serviceName;
                    string? baseTypeName;
                    string? implementingTypeName;
                    var target = attribute.Parent?.Parent;
                    if (target is ClassDeclarationSyntax classDeclarationSyntax
                        && context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken) is { } namedType)
                    {
                        accessibility = namedType.DeclaredAccessibility;
                        baseTypeName = namedType.BaseType is null or { SpecialType: SpecialType.System_Object }
                            ? null
                            : namedType.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        implementingTypeName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (namedType.Name.EndsWith("InProcess"))
                        {
                            serviceName = namedType.Name.Substring(0, namedType.Name.Length - "InProcess".Length);
                        }
                        else
                        {
                            serviceName = namedType.Name;
                        }
                    }
                    else
                    {
                        accessibility = Accessibility.NotApplicable;
                        implementingTypeName = null;
                        baseTypeName = null;
                        serviceName = null;
                    }

                    if (serviceName is null || implementingTypeName is null)
                    {
                        return null;
                    }

                    return new ServiceDataModel(accessibility, serviceName, baseTypeName, implementingTypeName);
                });

            var referenceDataModel = context.CompilationProvider.Select(
                static (compilation, cancellationToken) =>
                {
                    var hasSAsyncServiceProvider = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider") is not null;
                    var hasThreadHelperJoinableTaskContext = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Shell.ThreadHelper") is { } threadHelper
                        && threadHelper.GetMembers("JoinableTaskContext").Any(member => member.Kind == SymbolKind.Property);
                    var canCancelJoinTillEmptyAsync = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Threading.JoinableTaskCollection") is { } joinableTaskCollection
                        && joinableTaskCollection.GetMembers("JoinTillEmptyAsync").Any(member => member is IMethodSymbol { Parameters.Length: 1 });
                    var hasJoinableTaskFactoryWithPriority = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Threading.DispatcherExtensions") is not null;
                    var hasAsyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1") is not null;
                    var hasErrorHandler = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.ErrorHandler") is not null;
                    var hasOperationProgress = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.OperationProgress.SVsOperationProgress") is not null;
                    var hasOperationProgressStatusService = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.OperationProgress.IVsOperationProgressStatusService") is not null;

                    return new ReferenceDataModel(hasSAsyncServiceProvider, hasThreadHelperJoinableTaskContext, canCancelJoinTillEmptyAsync, hasJoinableTaskFactoryWithPriority, hasAsyncEnumerable, hasErrorHandler, hasOperationProgress, hasOperationProgressStatusService);
                });

            context.RegisterSourceOutput(
                referenceDataModel,
                static (context, referenceDataModel) =>
                {
                    var usings = new List<string>
                    {
                        "System",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "System.Windows",
                        "System.Windows.Threading",
                        "global::Xunit",
                    };

                    if (!referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("global::Xunit.Harness");
                    }

                    usings.Add("global::Xunit.Threading");
                    usings.Add("Microsoft.VisualStudio.ComponentModelHost");
                    usings.Add("Microsoft.VisualStudio.Shell");

                    if (referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("Microsoft.VisualStudio.Shell.Interop");
                    }

                    usings.Add("Microsoft.VisualStudio.Threading");

                    string getServiceImpl;
                    if (referenceDataModel.HasSAsyncServiceProvider)
                    {
                        usings.Add("IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider");
                        usings.Add("Task = System.Threading.Tasks.Task");
                        getServiceImpl = @"            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var serviceProvider = (IAsyncServiceProvider?)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SAsyncServiceProvider)).WithCancellation(cancellationToken);
            Assumes.Present(serviceProvider);

            var @interface = (TInterface?)await serviceProvider!.GetServiceAsync(typeof(TService)).WithCancellation(cancellationToken);
            Assumes.Present(@interface);
            return @interface!;";
                    }
                    else
                    {
                        usings.Add("Task = System.Threading.Tasks.Task");
                        getServiceImpl = @"            await TaskScheduler.Default;

            var @interface = await GetServiceCoreAsync(JoinableTaskFactory, cancellationToken).WithCancellation(cancellationToken);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            return @interface ?? throw new InvalidOperationException();

            static async Task<TInterface?> GetServiceCoreAsync(JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
            {
                await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return (TInterface?)GlobalServiceProvider.ServiceProvider.GetService(typeof(TService));
            }";
                    }

                    var inProcComponentSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{string.Join(Environment.NewLine, usings.Select(u => $"    using {u};"))}

    internal abstract class InProcComponent : IAsyncLifetime
    {{
        protected InProcComponent(TestServices testServices)
        {{
            TestServices = testServices ?? throw new ArgumentNullException(nameof(testServices));
        }}

        public TestServices TestServices {{ get; }}

        protected JoinableTaskFactory JoinableTaskFactory => TestServices.JoinableTaskFactory;

        Task IAsyncLifetime.InitializeAsync()
        {{
            return InitializeCoreAsync();
        }}

        Task IAsyncLifetime.DisposeAsync()
        {{
            return Task.CompletedTask;
        }}

        protected virtual Task InitializeCoreAsync()
        {{
            return Task.CompletedTask;
        }}

        protected async Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {{
{getServiceImpl}
        }}

        protected async Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {{
            var componentModel = await GetRequiredGlobalServiceAsync<SComponentModel, IComponentModel>(cancellationToken);
            return componentModel.GetService<TService>();
        }}

        /// <summary>
        /// Waiting for the application to 'idle' means that it is done pumping messages (including WM_PAINT).
        /// </summary>
        /// <param name=""cancellationToken"">The cancellation token that the operation will observe.</param>
        /// <returns>A <see cref=""Task""/> representing the asynchronous operation.</returns>
        internal static async Task WaitForApplicationIdleAsync(CancellationToken cancellationToken)
        {{
            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.ApplicationIdle);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            await Task.Factory.StartNew(
                () => {{ }},
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler);
        }}
    }}
}}
";

                    context.AddSource($"InProcComponent{SourceSuffix}", inProcComponentSource);

                    string shellInProcessEnumerateWindowsImpl;
                    if (referenceDataModel.HasAsyncEnumerable)
                    {
                        shellInProcessEnumerateWindowsImpl = @"    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    internal partial class ShellInProcess
    {
        public async IAsyncEnumerable<IVsWindowFrame> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                {
                    yield break;
                }

                for (var i = 0; i < fetched; i++)
                {
                    yield return frameBuffer[i];
                }
            }
        }
    }";
                    }
                    else
                    {
                        shellInProcessEnumerateWindowsImpl = @"    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    internal partial class ShellInProcess
    {
        public async Task<ReadOnlyCollection<IVsWindowFrame>> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var result = new List<IVsWindowFrame>();
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                {
                    break;
                }

                result.AddRange(frameBuffer.Take((int)fetched));
            }

            return result.AsReadOnly();
        }
    }";
                    }

                    var shellInProcessEnumerateWindowsSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{shellInProcessEnumerateWindowsImpl}
}}
";
                    context.AddSource($"ShellInProcess_EnumerateWindowsAsync{SourceSuffix}", shellInProcessEnumerateWindowsSource);

                    if (referenceDataModel.HasAsyncEnumerable)
                    {
                        context.AddSource($"SolutionExplorerInProcess.SolutionEvents_IAsyncDisposable{SourceSuffix}", SolutionExplorerInProcessSolutionEventsDisposeAsyncSource);
                    }
                    else
                    {
                        context.AddSource($"SolutionExplorerInProcess.SolutionEvents_IDisposable{SourceSuffix}", SolutionExplorerInProcessSolutionEventsDisposeSource);
                    }

                    var usings2 = new List<string>
                    {
                        "System",
                        "System.Diagnostics.CodeAnalysis",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "System.Windows",
                    };

                    if (referenceDataModel.HasJoinableTaskFactoryWithPriority)
                    {
                        usings2.Add("System.Windows.Threading");
                    }

                    usings2.Add("global::Xunit");

                    if (!referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        usings2.Add("global::Xunit.Harness");
                    }

                    usings2.Add("global::Xunit.Sdk");

                    if (referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        usings2.Add("Microsoft.VisualStudio.Shell");
                    }
                    else
                    {
                        usings2.Add("Microsoft.VisualStudio.Shell.Interop");
                    }

                    usings2.Add("Microsoft.VisualStudio.Threading");
                    usings2.Add("Task = System.Threading.Tasks.Task");

                    string joinableTaskContextInitializer;
                    if (referenceDataModel.HasThreadHelperJoinableTaskContext)
                    {
                        joinableTaskContextInitializer = "            JoinableTaskContext = ThreadHelper.JoinableTaskContext;";
                    }
                    else
                    {
                        joinableTaskContextInitializer = @"            if (GlobalServiceProvider.ServiceProvider.GetService(typeof(SVsTaskSchedulerService)) is IVsTaskSchedulerService2 taskSchedulerService)
            {
                JoinableTaskContext = (JoinableTaskContext)taskSchedulerService.GetAsyncTaskContext();
            }
            else
            {
                JoinableTaskContext = new JoinableTaskContext();
            }";
                    }

                    var joinTillEmpty = referenceDataModel.CanCancelJoinTillEmptyAsync
                        ? "await _joinableTaskCollection.JoinTillEmptyAsync(CleanupCancellationToken);"
                        : "await _joinableTaskCollection.JoinTillEmptyAsync().WithCancellation(CleanupCancellationToken);";

                    var createFactory = referenceDataModel.HasJoinableTaskFactoryWithPriority
                        ? "_joinableTaskFactory = value.CreateFactory(_joinableTaskCollection).WithPriority(Application.Current.Dispatcher, DispatcherPriority.Background);"
                        : "_joinableTaskFactory = value.CreateFactory(_joinableTaskCollection);";

                    var abstractIdeIntegrationTestSource = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
{string.Join(Environment.NewLine, usings2.Select(u => $"    using {u};"))}

    /// <summary>
    /// Provides a base class for Visual Studio integration tests.
    /// </summary>
    /// <remarks>
    /// The following is the xunit execution order:
    ///
    /// <list type=""number"">
    /// <item><description>Instance constructor</description></item>
    /// <item><description><see cref=""IAsyncLifetime.InitializeAsync""/></description></item>
    /// <item><description><see cref=""BeforeAfterTestAttribute.Before""/></description></item>
    /// <item><description>Test method</description></item>
    /// <item><description><see cref=""BeforeAfterTestAttribute.After""/></description></item>
    /// <item><description><see cref=""IAsyncLifetime.DisposeAsync""/></description></item>
    /// <item><description><see cref=""IDisposable.Dispose""/></description></item>
    /// </list>
    /// </remarks>
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {{
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        public static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(4);

        /// <summary>
        /// A timeout used to avoid hangs during test cleanup. This is separate from <see cref=""HangMitigatingTimeout""/>
        /// to provide tests an opportunity to clean up state even if failure occurred due to timeout.
        /// </summary>
        private static readonly TimeSpan CleanupHangMitigatingTimeout = TimeSpan.FromMinutes(2);

        private readonly CancellationTokenSource _hangMitigatingCancellationTokenSource;
        private readonly CancellationTokenSource _cleanupCancellationTokenSource;

        private JoinableTaskContext? _joinableTaskContext;
        private JoinableTaskCollection? _joinableTaskCollection;
        private JoinableTaskFactory? _joinableTaskFactory;

        private TestServices? _testServices;

        /// <summary>
        /// Initializes a new instance of the <see cref=""AbstractIdeIntegrationTest""/> class.
        /// </summary>
        protected AbstractIdeIntegrationTest()
        {{
            Assert.True(Application.Current.Dispatcher.CheckAccess());

{joinableTaskContextInitializer}

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(HangMitigatingTimeout);
            _cleanupCancellationTokenSource = new CancellationTokenSource();
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskContext""/> context for use in integration tests.
        /// </summary>
        [NotNull]
        protected JoinableTaskContext? JoinableTaskContext
        {{
            get
            {{
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }}

            private set
            {{
                if (value == _joinableTaskContext)
                {{
                    return;
                }}

                if (value is null)
                {{
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }}
                else
                {{
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    {createFactory}
                }}
            }}
        }}

        [NotNull]
        private protected TestServices? TestServices
        {{
            get
            {{
                return _testServices ?? throw new InvalidOperationException();
            }}

            private set
            {{
                _testServices = value;
            }}
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskFactory""/> for use in integration tests.
        /// </summary>
        protected JoinableTaskFactory JoinableTaskFactory
            => _joinableTaskFactory ?? throw new InvalidOperationException();

        /// <summary>
        /// Gets a cancellation token for use in integration tests to avoid CI timeouts.
        /// </summary>
        protected CancellationToken HangMitigatingCancellationToken
            => _hangMitigatingCancellationTokenSource.Token;

        /// <remarks>
        /// ⚠️ Note that this token will not be cancelled prior to the call to <see cref=""DisposeAsync""/> (which starts
        /// the cancellation timer). Derived types are not likely to make use of this, so it's marked
        /// <see langword=""private""/>.
        /// </remarks>
        private CancellationToken CleanupCancellationToken
            => _cleanupCancellationTokenSource.Token;

        /// <inheritdoc/>
        public virtual async Task InitializeAsync()
        {{
            TestServices = await CreateTestServicesAsync();
        }}

        /// <summary>
        /// This method implements <see cref=""IAsyncLifetime.DisposeAsync""/>, and is used for releasing resources
        /// created by <see cref=""IAsyncLifetime.InitializeAsync""/>. This method is only called if
        /// <see cref=""InitializeAsync""/> completes successfully.
        /// </summary>
        public virtual async Task DisposeAsync()
        {{
            _cleanupCancellationTokenSource.CancelAfter(CleanupHangMitigatingTimeout);

            await TestServices.SolutionExplorer.CloseSolutionAsync(CleanupCancellationToken);

            if (_joinableTaskCollection is object)
            {{
                {joinTillEmpty}
            }}

            JoinableTaskContext = null;
        }}

        /// <summary>
        /// This method provides the implementation for <see cref=""IDisposable.Dispose""/>.
        /// This method is called via the <see cref=""IDisposable""/> interface if the constructor completes successfully.
        /// The <see cref=""InitializeAsync""/> may or may not have completed successfully.
        /// </summary>
        public virtual void Dispose()
        {{
            _hangMitigatingCancellationTokenSource.Dispose();
            _cleanupCancellationTokenSource.Dispose();
        }}

        private protected virtual async Task<TestServices> CreateTestServicesAsync()
            => await TestServices.CreateAsync(JoinableTaskFactory);
    }}
}}
";
                    context.AddSource($"AbstractIdeIntegrationTest{SourceSuffix}", abstractIdeIntegrationTestSource);

                    if (!referenceDataModel.HasErrorHandler)
                    {
                        context.AddSource($"ErrorHandler{SourceSuffix}", ErrorHandlerSource);
                        context.AddSource($"VSConstants{SourceSuffix}", VSConstantsSource);
                    }

                    if (referenceDataModel.HasOperationProgress)
                    {
                        if (referenceDataModel.HasOperationProgressStatusService)
                        {
                            context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemSource);
                        }
                        else
                        {
                            context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemPartialSource);
                        }
                    }
                    else
                    {
                        context.AddSource($"WorkspaceInProcess.WaitForProjectSystemAsync{SourceSuffix}", WorkspaceInProcessWaitForProjectSystemLegacySource);
                    }
                });

            context.RegisterSourceOutput(
                services,
                static (context, service) =>
                {
                    if (service is null)
                    {
                        return;
                    }

                    var accessibility = service.Accessibility is Accessibility.Public ? "public" : "internal";
                    var namespaceName = service.ImplementingTypeName.Substring("global::".Length, service.ImplementingTypeName.LastIndexOf('.') - "global::".Length);
                    var typeName = service.ImplementingTypeName.Substring(service.ImplementingTypeName.LastIndexOf('.') + 1);
                    var baseTypeName = service.BaseTypeName ?? "global::Microsoft.VisualStudio.Extensibility.Testing.InProcComponent";
                    var usings = string.Empty;
                    if (namespaceName != "Microsoft.VisualStudio.Extensibility.Testing"
                        && !namespaceName.StartsWith("Microsoft.VisualStudio.Extensibility.Testing."))
                    {
                        usings = @"
    using Microsoft.VisualStudio.Extensibility.Testing;

";
                    }

                    var partialService = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace {namespaceName}
{{{usings}
    {accessibility} partial class {typeName} : {baseTypeName}
    {{
        public {typeName}(TestServices testServices)
            : base(testServices)
        {{
        }}
    }}
}}
";

                    context.AddSource($"{typeName}{SourceSuffix}", partialService);
                });

            context.RegisterSourceOutput(
                services.Collect(),
                static (context, services) =>
                {
                    var initializers = new List<string>();
                    var properties = new List<string>();
                    var asyncInitializers = new List<string>();
                    foreach (var service in services)
                    {
                        if (service is null)
                        {
                            continue;
                        }

                        initializers.Add($"{service.ServiceName} = new {service.ImplementingTypeName}(this);");

                        var accessibility = service.Accessibility is Accessibility.Public ? "public" : "internal";
                        properties.Add($"{accessibility} {service.ImplementingTypeName} {service.ServiceName} {{ get; }}");

                        asyncInitializers.Add($"await ((IAsyncLifetime){service.ServiceName}).InitializeAsync();");
                    }

                    var testServices = $@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{{
    using System.Threading.Tasks;
    using global::Xunit;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// Provides access to helpers for common integration test functionality.
    /// </summary>
    public sealed class TestServices
    {{
        private TestServices(JoinableTaskFactory joinableTaskFactory)
        {{
            JoinableTaskFactory = joinableTaskFactory;

{string.Join("\r\n", initializers.Select(initializer => "            " + initializer))}
        }}

        /// <summary>
        /// Gets the <see cref=""Threading.JoinableTaskFactory""/> for use in integration tests.
        /// </summary>
        public JoinableTaskFactory JoinableTaskFactory {{ get; }}

{string.Join("\r\n", properties.Select(property => "        " + property))}

        internal static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {{
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }}

        private async Task InitializeAsync()
        {{
{string.Join("\r\n", asyncInitializers.Select(initializer => "            " + initializer))}
        }}
    }}
}}
";

                    context.AddSource($"TestServices{SourceSuffix}", testServices);
                });
        }

        private static SimpleNameSyntax GetUnqualifiedName(NameSyntax name)
        {
            return name switch
            {
                SimpleNameSyntax simpleName => simpleName,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right,
                AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
                _ => throw new ArgumentException($"Unsupported syntax kind: {name.Kind()}", nameof(name)),
            };
        }

        private sealed record ServiceDataModel(
            Accessibility Accessibility,
            string ServiceName,
            string? BaseTypeName,
            string ImplementingTypeName);

        private sealed record ReferenceDataModel(
            bool HasSAsyncServiceProvider,
            bool HasThreadHelperJoinableTaskContext,
            bool CanCancelJoinTillEmptyAsync,
            bool HasJoinableTaskFactoryWithPriority,
            bool HasAsyncEnumerable,
            bool HasErrorHandler,
            bool HasOperationProgress,
            bool HasOperationProgressStatusService);
    }
}
