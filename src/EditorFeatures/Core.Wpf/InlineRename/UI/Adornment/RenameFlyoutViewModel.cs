// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EditorFeatures.Lightup;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class RenameFlyoutViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly bool _registerOleComponent;
        private readonly IGlobalOptionService _globalOptionService;
        private OleComponent? _oleComponent;
        private bool _disposedValue;
        private bool _isReplacementTextValid = true;
        public event PropertyChangedEventHandler? PropertyChanged;

        public RenameFlyoutViewModel(
            InlineRenameSession session,
            TextSpan selectionSpan,
            bool registerOleComponent,
            IGlobalOptionService globalOptionService,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
            Lazy<ISmartRenameSessionFactoryWrapper>? smartRenameSessionFactory)
#pragma warning restore CS0618 
        {
            Session = session;
            _registerOleComponent = registerOleComponent;
            _globalOptionService = globalOptionService;
            Session.ReplacementTextChanged += OnReplacementTextChanged;
            Session.ReplacementsComputed += OnReplacementsComputed;
            Session.ReferenceLocationsChanged += OnReferenceLocationsChanged;
            StartingSelection = selectionSpan;
            InitialTrackingSpan = session.TriggerSpan.CreateTrackingSpan(SpanTrackingMode.EdgeInclusive);
            var smartRenameSession = smartRenameSessionFactory?.Value.CreateSmartRenameSession(Session.TriggerSpan);
            if (smartRenameSession is not null)
            {
                SmartRenameViewModel = new SmartRenameViewModel(globalOptionService, threadingContext, listenerProvider, smartRenameSession.Value, this);
            }

            RegisterOleComponent();
        }

        public SmartRenameViewModel? SmartRenameViewModel { get; }

        public string IdentifierText
        {
            get => Session.ReplacementText;
            set
            {
                if (value != Session.ReplacementText)
                {
                    Session.ApplyReplacementText(value, propagateEditImmediately: true, updateSelection: false);
                    NotifyPropertyChanged(nameof(IdentifierText));
                }
            }
        }

        public InlineRenameSession Session { get; }

        public ITrackingSpan InitialTrackingSpan { get; }

        public bool AllowFileRename => Session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed && _isReplacementTextValid;
        public bool ShowFileRename => Session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;

        public string FileRenameString => Session.FileRenameInfo switch
        {
            InlineRenameFileRenameInfo.TypeDoesNotMatchFileName => EditorFeaturesResources.Rename_file_name_doesnt_match,
            InlineRenameFileRenameInfo.TypeWithMultipleLocations => EditorFeaturesResources.Rename_file_partial_type,
            _ => EditorFeaturesResources.Rename_symbols_file
        };

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set => Set(ref _searchText, value);
        }

        private string? _statusText;
        public string? StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        private Severity _statusSeverity;
        public Severity StatusSeverity
        {
            get => _statusSeverity;
            set
            {
                if (Set(ref _statusSeverity, value))
                {
                    NotifyPropertyChanged(nameof(ShowStatusText));
                    NotifyPropertyChanged(nameof(StatusImageMoniker));
                }
            }
        }

        public bool ShowStatusText => _statusSeverity != Severity.None;
        public bool ShowSearchText => _statusSeverity != Severity.Error;

        public ImageMoniker StatusImageMoniker => _statusSeverity switch
        {
            Severity.Error => KnownMonikers.StatusError,
            Severity.Warning => KnownMonikers.StatusWarning,
            _ => ImageLibrary.InvalidImageMoniker
        };

        public bool RenameInCommentsFlag
        {
            get => Session.Options.RenameInComments;
            set
            {
                _globalOptionService.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameInComments = value });
            }
        }

        public bool RenameInStringsFlag
        {
            get => Session.Options.RenameInStrings;
            set
            {
                _globalOptionService.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameInStrings = value });
            }
        }

        public bool RenameFileFlag
        {
            get => Session.Options.RenameFile;
            set
            {
                _globalOptionService.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameFile = value });
            }
        }

        public bool PreviewChangesFlag
        {
            get => Session.PreviewChanges;
            set
            {
                _globalOptionService.SetGlobalOption(InlineRenameSessionOptionsStorage.PreviewChanges, value);
                Session.SetPreviewChanges(value);
            }
        }

        public bool RenameOverloadsFlag
        {
            get => Session.Options.RenameOverloads;
            set
            {
                _globalOptionService.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameOverloads = value });
            }
        }

        public bool IsCollapsed
        {
            get => _globalOptionService.GetOption(InlineRenameUIOptionsStorage.CollapseUI);
            set
            {
                if (value != IsCollapsed)
                {
                    _globalOptionService.SetGlobalOption(InlineRenameUIOptionsStorage.CollapseUI, value);
                    NotifyPropertyChanged(nameof(IsCollapsed));
                    NotifyPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public bool IsExpanded
        {
            get => !IsCollapsed;
            set => IsCollapsed = !value;
        }

        public bool IsRenameOverloadsEditable
            => !Session.MustRenameOverloads;

        public bool IsRenameOverloadsVisible
            => Session.HasRenameOverloads;

        public TextSpan StartingSelection { get; }

        public bool Submit()
        {
            if (StatusSeverity == Severity.Error)
            {
                return false;
            }

            SmartRenameViewModel?.Commit(IdentifierText);
            if (_globalOptionService.ShouldCommitAsynchronously())
            {
                _ = Session.CommitAsync(previewChanges: false);
            }
            else
            {
                Session.Commit();
            }

            return true;
        }

        public void Cancel()
        {
            SmartRenameViewModel?.Cancel();
            Session.Cancel();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Shell routes commands based on focused tool window. Since we're outside of a tool window,
        /// Editor can end up intercepting commands and TYPECHARs sent to us, even when we're focused,
        /// so hook in and intercept each message for WPF.
        /// </summary>
        public void RegisterOleComponent()
        {
            // In unit testing we won't have an OleComponentManager available, so 
            // calls to OleComponent.CreateHostedComponent will throw
            if (!_registerOleComponent)
            {
                return;
            }

            Debug.Assert(_oleComponent is null);

            _oleComponent = OleComponent.CreateHostedComponent("Microsoft CodeAnalysis Inline Rename");
            _oleComponent.PreTranslateMessage += OnPreTranslateMessage;
            _oleComponent.BeginTracking();
        }

        private void UnregisterOleComponent()
        {
            if (_oleComponent is not null)
            {
                _oleComponent.EndTracking();
                _oleComponent.PreTranslateMessage -= OnPreTranslateMessage;
                _oleComponent.Dispose();
                _oleComponent = null;
            }
        }

        private void OnPreTranslateMessage(object sender, PreTranslateMessageEventArgs e)
        {
            var msg = e.Message;
            if (ComponentDispatcher.RaiseThreadMessage(ref msg) || IsSuppressedMessage(msg))
            {
                e.MessageConsumed = true;
            }

            // When the adornment is focused, we register an OleComponent to divert window messages
            // away from the editor and back to WPF to enable proper handling of arrows, backspace,
            // delete, etc. Unfortunately, anything not handled by WPF is then propagated back to the
            // shell command system where it is handled by the open editor window.
            // To avoid unhandled arrow commands from being handled by editor,
            // we mark them as handled so long as the adornment is focused.
            static bool IsSuppressedMessage(MSG msg)
                => msg.message switch
                {
                    0x0100 or // WM_KEYDOWN
                    0x0101    // WM_KEYUP
                        => msg.wParam.ToInt32() switch
                        {
                            >= 0x0025 and <= 0x0028 => true, // VK_LEFT, VK_UP, VK_RIGHT, and VK_DOWN

                            0x0021 or       // VK_PRIOR (Page Up)
                            0x0022 or       // VK_NEXT (Page Down)
                            0x0023 or       // VK_END
                            0x0024 or       // VK_HOME
                            0x0D00 or       // VK_RETURN
                            0x0009 => true, // VK_TAB

                            _ => false
                        },

                    _ => false
                };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Session.ReplacementTextChanged -= OnReplacementTextChanged;
                    Session.ReplacementsComputed -= OnReplacementsComputed;

                    if (SmartRenameViewModel is not null)
                    {
                        SmartRenameViewModel.Dispose();
                    }

                    UnregisterOleComponent();
                }

                _disposedValue = true;
            }
        }

        private void OnReplacementTextChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(IdentifierText));
        }

        private void OnReplacementsComputed(object sender, IInlineRenameReplacementInfo result)
        {
            if (Set(ref _isReplacementTextValid, result.ReplacementTextValid, "IsReplacementTextValid"))
            {
                NotifyPropertyChanged(nameof(AllowFileRename));

                if (!_isReplacementTextValid && !string.IsNullOrEmpty(IdentifierText))
                {
                    StatusText = EditorFeaturesResources.The_new_name_is_not_a_valid_identifier;
                    StatusSeverity = Severity.Error;
                    return;
                }

                var resolvableConflicts = 0;
                var unresolvedConflicts = 0;
                foreach (var replacementKind in result.GetAllReplacementKinds())
                {
                    switch (replacementKind)
                    {
                        case InlineRenameReplacementKind.UnresolvedConflict:
                            unresolvedConflicts++;
                            break;

                        case InlineRenameReplacementKind.ResolvedReferenceConflict:
                        case InlineRenameReplacementKind.ResolvedNonReferenceConflict:
                            resolvableConflicts++;
                            break;
                    }
                }

                if (unresolvedConflicts > 0)
                {
                    StatusText = string.Format(EditorFeaturesResources._0_unresolvable_conflict_s, unresolvedConflicts);
                    StatusSeverity = Severity.Error;
                    return;
                }

                if (resolvableConflicts > 0)
                {
                    StatusText = string.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, resolvableConflicts);
                    StatusSeverity = Severity.Warning;
                    return;
                }

                StatusText = null;
                StatusSeverity = Severity.None;
            }
        }

        private void OnReferenceLocationsChanged(object sender, ImmutableArray<InlineRenameLocation> renameLocations)
        {
            // Collapse the same edits across multiple instances of the same linked-file.
            var fileCount = renameLocations.GroupBy(s => s.Document.FilePath).Count();
            var referenceCount = renameLocations.Select(loc => (loc.Document.FilePath, loc.TextSpan)).Distinct().Count();

            if (referenceCount == 1 && fileCount == 1)
            {
                SearchText = EditorFeaturesResources.Rename_will_update_1_reference_in_1_file;
            }
            else if (fileCount == 1)
            {
                SearchText = string.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, referenceCount);
            }
            else
            {
                SearchText = string.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_files, referenceCount, fileCount);
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T newValue, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            NotifyPropertyChanged(name);
            return true;
        }

        public enum Severity
        {
            None,
            Warning,
            Error
        }
    }
}
