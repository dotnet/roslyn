// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.NavigationBar;
using Microsoft.VisualStudio.TextManager.Interop;
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
            private readonly IOptionService _optionService;
            private readonly IThreadingContext _threadingContext;
            private readonly WorkspaceRegistration _workspaceRegistration;

            private INavigationBarController? _navigationBarController;
            private IVsDropdownBarClient? _dropdownBarClient;

            public VsCodeWindowManager(TLanguageService languageService, IVsCodeWindow codeWindow)
            {
                _languageService = languageService;
                _codeWindow = codeWindow;

                var workspace = languageService.Package.ComponentModel.GetService<VisualStudioWorkspace>();
                _optionService = workspace.Services.GetRequiredService<IOptionService>();

                _threadingContext = languageService.Package.ComponentModel.GetService<IThreadingContext>();

                _sink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
                _optionService.OptionChanged += OnOptionChanged;

                ErrorHandler.ThrowOnFailure(_codeWindow.GetBuffer(out var buffer));
                var textContainer = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer).AsTextContainer();
                _workspaceRegistration = CodeAnalysis.Workspace.GetWorkspaceRegistration(textContainer);
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;
            }

            private void OnWorkspaceRegistrationChanged(object sender, System.EventArgs e)
            {
                _threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    // This event may not be triggered on the main thread, but adding and removing the navbar
                    // must be done from the main thread.
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    _navigationBarController?.SetWorkspace(_workspaceRegistration.Workspace);

                    // Trigger a check to see if the dropdown should be added / removed now that the buffer is in a different workspace.
                    AddOrRemoveDropdown();
                });
            }

            private void SetupView(IVsTextView view)
            {
                _languageService.SetupNewTextView(view);
            }

            private void TeardownView(IVsTextView view)
            {
            }

            private void OnOptionChanged(object sender, OptionChangedEventArgs e)
            {
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
                var document = _languageService.EditorAdaptersFactoryService.GetDataBuffer(buffer).AsTextContainer().GetRelatedDocuments().FirstOrDefault();
                if (document?.GetLanguageService<INavigationBarItemService>() == null)
                {
                    // Remove the existing dropdown bar if it is ours.
                    if (IsOurDropdownBar(dropdownManager, _dropdownBarClient, out var _))
                    {
                        RemoveDropdownBar(dropdownManager);
                    }

                    return;
                }

                var enabled = _optionService.GetOption(NavigationBarOptions.ShowNavigationBar, _languageService.RoslynLanguageName);
                if (enabled)
                {
                    if (IsOurDropdownBar(dropdownManager, _dropdownBarClient, out var existingDropdownBar))
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

                static bool IsOurDropdownBar(IVsDropdownBarManager dropdownBarManager, IVsDropdownBarClient? dropdownBarClient, out IVsDropdownBar? existingDropdownBar)
                {
                    existingDropdownBar = GetDropdownBar(dropdownBarManager);
                    if (existingDropdownBar != null)
                    {
                        if (dropdownBarClient != null &&
                            dropdownBarClient == GetDropdownBarClient(existingDropdownBar))
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
                newController.SetWorkspace(_workspaceRegistration.Workspace);
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

                AddOrRemoveDropdown();

                return VSConstants.S_OK;
            }

            public int OnCloseView(IVsTextView view)
            {
                TeardownView(view);

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
                _optionService.OptionChanged -= OnOptionChanged;
                _workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;

                if (_codeWindow is IVsDropdownBarManager dropdownManager)
                {
                    RemoveDropdownBar(dropdownManager);
                }

                return VSConstants.S_OK;
            }
        }
    }
}
