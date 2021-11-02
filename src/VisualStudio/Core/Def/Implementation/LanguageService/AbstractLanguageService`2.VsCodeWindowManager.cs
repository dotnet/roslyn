// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.NavigationBar;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService>
    {
        internal class VsCodeWindowManager : IVsCodeWindowManager, IVsCodeWindowEvents
        {
            private readonly TLanguageService _languageService;
            private readonly IVsCodeWindow _codeWindow;
            private readonly ComEventSink _sink;
            private readonly IThreadingContext _threadingContext;
            private readonly IAsynchronousOperationListener _asynchronousOperationListener;
            private readonly IGlobalOptionService _globalOptions;

            private IDisposable? _navigationBarController;
            private IVsDropdownBarClient? _dropdownBarClient;

            public VsCodeWindowManager(TLanguageService languageService, IVsCodeWindow codeWindow)
            {
                _languageService = languageService;
                _codeWindow = codeWindow;

                var componentModel = languageService.Package.ComponentModel;
                _threadingContext = componentModel.GetService<IThreadingContext>();
                _globalOptions = componentModel.GetService<IGlobalOptionService>();

                var listenerProvider = componentModel.GetService<IAsynchronousOperationListenerProvider>();
                _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.NavigationBar);

                _sink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
                _globalOptions.OptionChanged += GlobalOptionChanged;
            }

            private void OnWorkspaceRegistrationChanged(object sender, System.EventArgs e)
            {
                var token = _asynchronousOperationListener.BeginAsyncOperation(nameof(OnWorkspaceRegistrationChanged));

                // Fire and forget to update the navbar based on the workspace registration
                // to avoid blocking the caller and possible deadlocks workspace registration changed events under lock.
                UpdateWorkspaceAsync().CompletesAsyncOperation(token).Forget();
            }

            private async Task UpdateWorkspaceAsync()
            {
                // This event may not be triggered on the main thread, but adding and removing the navbar
                // must be done from the main thread.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Trigger a check to see if the dropdown should be added / removed now that the buffer is in a different workspace.
                AddOrRemoveDropdown();
            }

            private void SetupView(IVsTextView view)
                => _languageService.SetupNewTextView(view);

            private void GlobalOptionChanged(object sender, OptionChangedEventArgs e)
            {
                if (e.Language != _languageService.RoslynLanguageName ||
                    e.Option != NavigationBarViewOptions.ShowNavigationBar)
                {
                    return;
                }

                AddOrRemoveDropdown();
            }

            private void AddOrRemoveDropdown()
            {
                if (_codeWindow is not IVsDropdownBarManager dropdownManager)
                {
                    return;
                }

                if (ErrorHandler.Failed(_codeWindow.GetBuffer(out var buffer)) || buffer == null)
                {
                    return;
                }

                var textBuffer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer);
                var document = textBuffer?.AsTextContainer()?.GetRelatedDocuments().FirstOrDefault();
                // TODO - Remove the TS check once they move the liveshare navbar to LSP.  Then we can also switch to LSP
                // for the local navbar implementation.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1163360
                if (textBuffer?.IsInLspEditorContext() == true && document!.Project!.Language != InternalLanguageNames.TypeScript)
                {
                    // Remove the existing dropdown bar if it is ours.
                    if (IsOurDropdownBar(dropdownManager, out var _))
                    {
                        RemoveDropdownBar(dropdownManager);
                    }

                    return;
                }

                var enabled = _globalOptions.GetOption(NavigationBarViewOptions.ShowNavigationBar, _languageService.RoslynLanguageName);
                if (enabled)
                {
                    if (IsOurDropdownBar(dropdownManager, out var existingDropdownBar))
                    {
                        // The dropdown bar is already one of ours, do nothing.
                        return;
                    }

                    if (existingDropdownBar != null)
                    {
                        // Not ours, so remove the old one so that we can add ours.
                        RemoveDropdownBar(dropdownManager);
                    }
                    else
                    {
                        Contract.ThrowIfFalse(_navigationBarController == null, "We shouldn't have a controller manager if there isn't a dropdown");
                        Contract.ThrowIfFalse(_dropdownBarClient == null, "We shouldn't have a dropdown client if there isn't a dropdown");
                    }

                    AdddropdownBar(dropdownManager);
                }
                else
                {
                    RemoveDropdownBar(dropdownManager);
                }

                bool IsOurDropdownBar(IVsDropdownBarManager dropdownBarManager, out IVsDropdownBar? existingDropdownBar)
                {
                    existingDropdownBar = GetDropdownBar(dropdownBarManager);
                    if (existingDropdownBar != null)
                    {
                        if (_dropdownBarClient != null &&
                            _dropdownBarClient == GetDropdownBarClient(existingDropdownBar))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private static IVsDropdownBar GetDropdownBar(IVsDropdownBarManager dropdownManager)
            {
                ErrorHandler.ThrowOnFailure(dropdownManager.GetDropdownBar(out var existingDropdownBar));
                return existingDropdownBar;
            }

            private static IVsDropdownBarClient GetDropdownBarClient(IVsDropdownBar dropdownBar)
            {
                ErrorHandler.ThrowOnFailure(dropdownBar.GetClient(out var dropdownBarClient));
                return dropdownBarClient;
            }

            private void AdddropdownBar(IVsDropdownBarManager dropdownManager)
            {
                if (ErrorHandler.Failed(_codeWindow.GetBuffer(out var buffer)))
                {
                    return;
                }

                var navigationBarClient = new NavigationBarClient(dropdownManager, _codeWindow, _languageService.SystemServiceProvider, _languageService.Workspace);
                var textBuffer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer);
                var controllerFactoryService = _languageService.Package.ComponentModel.GetService<INavigationBarControllerFactoryService>();
                var newController = controllerFactoryService.CreateController(navigationBarClient, textBuffer);
                var hr = dropdownManager.AddDropdownBar(cCombos: 3, pClient: navigationBarClient);

                if (ErrorHandler.Failed(hr))
                {
                    newController.Dispose();
                    ErrorHandler.ThrowOnFailure(hr);
                }

                _navigationBarController = newController;
                _dropdownBarClient = navigationBarClient;
                return;
            }

            private void RemoveDropdownBar(IVsDropdownBarManager dropdownManager)
            {
                if (ErrorHandler.Succeeded(dropdownManager.RemoveDropdownBar()))
                {
                    if (_navigationBarController != null)
                    {
                        _navigationBarController.Dispose();
                        _navigationBarController = null;
                    }

                    _dropdownBarClient = null;
                }
            }

            public int AddAdornments()
            {
                int hr;
                if (ErrorHandler.Failed(hr = _codeWindow.GetPrimaryView(out var primaryView)))
                {
                    Debug.Fail("GetPrimaryView failed in IVsCodeWindowManager.AddAdornments");
                    return hr;
                }

                SetupView(primaryView);
                if (ErrorHandler.Succeeded(_codeWindow.GetSecondaryView(out var secondaryView)))
                {
                    SetupView(secondaryView);
                }

                AddOrRemoveDropdown();

                return VSConstants.S_OK;
            }

            public int OnCloseView(IVsTextView view)
            {
                return VSConstants.S_OK;
            }

            public int OnNewView(IVsTextView view)
            {
                SetupView(view);

                return VSConstants.S_OK;
            }

            public int RemoveAdornments()
            {
                _sink.Unadvise();

                if (_codeWindow is IVsDropdownBarManager dropdownManager)
                {
                    RemoveDropdownBar(dropdownManager);
                }

                return VSConstants.S_OK;
            }
        }
    }
}
