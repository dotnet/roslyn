// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    /// <summary>Provides a set of helper functions for accessing services in the Visual Studio host process.</summary>
    /// <remarks>This methods should be executed Visual Studio host via the <see cref="VisualStudioInstance.ExecuteOnHostProcess"/> method.</remarks>
    internal static class RemotingHelper
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");
        private static readonly Guid RoslynPackageId = new Guid("6cf2e545-6109-4730-8883-cf43d7aec3e1");

        private static readonly string[] SupportedLanguages = new string[] { LanguageNames.CSharp, LanguageNames.VisualBasic };

        public static string ActiveTextViewContents
        {
            get
            {
                return InvokeOnUIThread(ActiveTextView.TextSnapshot.GetText);
            }

            set
            {
                InvokeOnUIThread(() => {
                    var textSnapshot = ActiveTextView.TextSnapshot;
                    var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                    ActiveTextView.TextBuffer.Replace(replacementSpan, value);
                });
            }
        }

        public static IWpfTextViewMargin GetTextViewMargin(string marginName) => InvokeOnUIThread(() => ActiveTextViewHost.GetTextViewMargin(marginName));

        public static ReadOnlyCollection<ICompletionSession> ActiveTextViewCompletionSessions => CompletionBroker.GetSessions(ActiveTextView);

        public static IComponentModel ComponentModel => GetGlobalService<IComponentModel>(typeof(SComponentModel));

        public static IInteractiveWindow CSharpInteractiveWindow => CSharpVsInteractiveWindow.InteractiveWindow;

        public static VisualStudioWorkspace VisualStudioWorkspace => ComponentModel.GetService<VisualStudioWorkspace>();

        private static ITextView ActiveTextView => ActiveTextViewHost.TextView;

        private static IWpfTextViewHost ActiveTextViewHost
        {
            get
            {
                // The active text view might not have finished composing yet, waiting for the application to 'idle'
                // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
                WaitForApplicationIdle();

                var activeVsTextView = (IVsUserData)(VsTextManagerActiveView);

                var wpfTextViewId = IWpfTextViewId;
                object wpfTextViewHost = null;

                var hresult = activeVsTextView.GetData(ref wpfTextViewId, out wpfTextViewHost);
                Marshal.ThrowExceptionForHR(hresult);

                return (IWpfTextViewHost)(wpfTextViewHost);
            }
        }

        private static ICompletionBroker CompletionBroker => ComponentModel.GetService<ICompletionBroker>();

        private static IVsInteractiveWindow CSharpVsInteractiveWindow => InvokeOnUIThread(() => CSharpVsInteractiveWindowProvider.Open(0, true));

        private static CSharpVsInteractiveWindowProvider CSharpVsInteractiveWindowProvider => ComponentModel.GetService<CSharpVsInteractiveWindowProvider>();

        private static Application CurrentApplication => Application.Current;

        private static Dispatcher CurrentApplicationDispatcher => CurrentApplication.Dispatcher;

        private static ExportProvider DefaultComponentModelExportProvider => ComponentModel.DefaultExportProvider;

        public static DTE DTE => GetGlobalService<DTE>(typeof(SDTE));

        private static ServiceProvider GlobalServiceProvider => ServiceProvider.GlobalProvider;

        private static HostWorkspaceServices VisualStudioWorkspaceServices => VisualStudioWorkspace.Services;

        private static IVsShell VsShell => GetGlobalService<IVsShell>(typeof(SVsShell));

        private static IVsTextManager VsTextManager => GetGlobalService<IVsTextManager>(typeof(SVsTextManager));

        private static IVsTextView VsTextManagerActiveView
        {
            get
            {
                IVsTextView vsTextView = null;

                var hresult = VsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out vsTextView);
                Marshal.ThrowExceptionForHR(hresult);

                return vsTextView;
            }
        }

        private static TestingOnly_WaitingService WaitingService => DefaultComponentModelExportProvider.GetExport<TestingOnly_WaitingService>().Value;

        public static void ActivateMainWindow() => InvokeOnUIThread(() =>
        {
            var activeVisualStudioWindow = (IntPtr)(IntegrationHelper.RetryRpcCall(() => DTE.ActiveWindow.HWnd));

            if (activeVisualStudioWindow == IntPtr.Zero)
            {
                activeVisualStudioWindow = (IntPtr)(IntegrationHelper.RetryRpcCall(() => DTE.MainWindow.HWnd));
            }

            IntegrationHelper.SetForegroundWindow(activeVisualStudioWindow);
        });

        public static void CleanupWaitingService()
        {
            var asynchronousOperationWaiterExports = DefaultComponentModelExportProvider.GetExports<IAsynchronousOperationWaiter>();

            if (!asynchronousOperationWaiterExports.Any())
            {
                throw new InvalidOperationException("The test waiting service could not be located.");
            }

            WaitingService.EnableActiveTokenTracking(true);
        }

        public static void CleanupWorkspace()
        {
            LoadRoslynPackage();
            VisualStudioWorkspace.TestHookPartialSolutionsDisabled = true;
        }

        public static void WaitForSystemIdle() => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);

        public static void WaitForApplicationIdle() => CurrentApplicationDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

        private static T GetGlobalService<T>(Type serviceType) => InvokeOnUIThread(() => (T)(GlobalServiceProvider.GetService(serviceType)));

        public static void InvokeOnUIThread(Action action) => CurrentApplicationDispatcher.Invoke(action);

        public static T InvokeOnUIThread<T>(Func<T> action) => CurrentApplicationDispatcher.Invoke(action);

        private static void LoadRoslynPackage()
        {
            var roslynPackageGuid = RoslynPackageId;
            IVsPackage roslynPackage = null;

            var hresult = VsShell.LoadPackage(ref roslynPackageGuid, out roslynPackage);
            Marshal.ThrowExceptionForHR(hresult);
        }
    }
}
