// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
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

            private INavigationBarController? _navigationBarController;
            private IVsDropdownBarClient? _dropdownBarClient;
            private IOptionService? _optionService;
            private WorkspaceRegistration? _workspaceRegistration;

            public VsCodeWindowManager(TLanguageService languageService, IVsCodeWindow codeWindow)
            {
                _languageService = languageService;
                _codeWindow = codeWindow;

                _threadingContext = languageService.Package.ComponentModel.GetService<IThreadingContext>();

                var listenerProvider = languageService.Package.ComponentModel.GetService<IAsynchronousOperationListenerProvider>();
                _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.NavigationBar);

                _sink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
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

                // If the workspace registration is missing, addornments have been removed.
                if (_workspaceRegistration == null)
                {
                    return;
                }

                // There's a new workspace, so make sure we unsubscribe from the old workspace option changes and subscribe to new.
                UpdateOptionChangedSource(_workspaceRegistration.Workspace);

                _navigationBarController?.SetWorkspace(_workspaceRegistration.Workspace);

                // Trigger a check to see if the dropdown should be added / removed now that the buffer is in a different workspace.
                AddOrRemoveDropdown();
            }

            private void UpdateOptionChangedSource(Workspace? newWorkspace)
            {
                if (_optionService != null)
                {
                    _optionService.OptionChanged -= OnOptionChanged;
                    _optionService = null;
                }

                var optionService = newWorkspace?.Services.GetService<IOptionService>();
                if (optionService != null)
                {
                    _optionService = optionService;
                    _optionService.OptionChanged += OnOptionChanged;
                }
            }

            private void SetupView(IVsTextView view)
                => _languageService.SetupNewTextView(view);

            private void OnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                // If the workspace registration is missing, addornments have been removed.
                if (_workspaceRegistration == null)
                {
                    return;
                }

                if (e.Language != _languageService.RoslynLanguageName ||
                    e.Option != NavigationBarOptions.ShowNavigationBar)
                {
                    return;
                }

                AddOrRemoveDropdown();
            }

            private void AddOrRemoveDropdown()
            {
                if (!(_codeWindow is IVsDropdownBarManager dropdownManager))
                {
                    return;
                }

                if (ErrorHandler.Failed(_codeWindow.GetBuffer(out var buffer)))
                {
                    return;
                }

                // Temporary solution until the editor provides a proper way to resolve the correct navbar.
                // Tracked in https://github.com/dotnet/roslyn/issues/40989
                var document = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer)?.AsTextContainer().GetRelatedDocuments().FirstOrDefault();
                if (document?.GetLanguageService<INavigationBarItemService>() == null)
                {
                    // Remove the existing dropdown bar if it is ours.
                    if (IsOurDropdownBar(dropdownManager, out var _))
                    {
                        RemoveDropdownBar(dropdownManager);
                    }

                    return;
                }

                var enabled = _optionService?.GetOption(NavigationBarOptions.ShowNavigationBar, _languageService.RoslynLanguageName);
                if (enabled == true)
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
                newController.SetWorkspace(_workspaceRegistration?.Workspace);
                var hr = dropdownManager.AddDropdownBar(cCombos: 3, pClient: navigationBarClient);

                if (ErrorHandler.Failed(hr))
                {
                    newController.Disconnect();
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
                        _navigationBarController.Disconnect();
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

                ErrorHandler.ThrowOnFailure(_codeWindow.GetBuffer(out var buffer));
                var textContainer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer).AsTextContainer();
                _workspaceRegistration = CodeAnalysis.Workspace.GetWorkspaceRegistration(textContainer);
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;

                UpdateOptionChangedSource(_workspaceRegistration.Workspace);

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

                if (_optionService != null)
                {
                    _optionService.OptionChanged -= OnOptionChanged;
                    _optionService = null;
                }

                if (_workspaceRegistration != null)
                {
                    _workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;
                    _workspaceRegistration = null;
                }

                if (_codeWindow is IVsDropdownBarManager dropdownManager)
                {
                    RemoveDropdownBar(dropdownManager);
                }

                return VSConstants.S_OK;
            }
        }
    }
}
