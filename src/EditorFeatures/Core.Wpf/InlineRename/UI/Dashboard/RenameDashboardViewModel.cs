// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InlineRename;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class RenameDashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IThreadingContext _threadingContext;
        private RenameDashboardSeverity _severity = RenameDashboardSeverity.None;
        private int _resolvableConflictCount;
        private int _unresolvableConflictCount;
        private bool _isReplacementTextValid;
        private bool _commitNotStart;

        public RenameDashboardViewModel(InlineRenameSession session, IThreadingContext threadingContext)
        {
            Session = session;
            SearchText = EditorFeaturesResources.Searching;

            Session.ReferenceLocationsChanged += OnReferenceLocationsChanged;
            Session.ReplacementsComputed += OnReplacementsComputed;
            Session.ReplacementTextChanged += OnReplacementTextChanged;
            Session.CommitStateChange += CommitStateChange;

            // Set the flag to true by default if we're showing the option.
            _isReplacementTextValid = true;
            _commitNotStart = true;
            _threadingContext = threadingContext;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnReferenceLocationsChanged(object sender, ImmutableArray<InlineRenameLocation> renameLocations)
        {
            var totalFilesCount = renameLocations.GroupBy(s => s.Document).Count();
            var totalSpansCount = renameLocations.Length;

            UpdateSearchText(totalSpansCount, totalFilesCount);
        }

        private void OnIsReplacementTextValidChanged(bool isReplacementTextValid)
        {
            if (isReplacementTextValid == _isReplacementTextValid)
            {
                return;
            }

            _isReplacementTextValid = isReplacementTextValid;
            NotifyPropertyChanged(nameof(AllowFileRename));
        }

        private void OnReplacementsComputed(object sender, IInlineRenameReplacementInfo result)
        {
            var session = (InlineRenameSession)sender;
            _resolvableConflictCount = 0;
            _unresolvableConflictCount = 0;
            OnIsReplacementTextValidChanged(result.ReplacementTextValid);
            if (result.ReplacementTextValid)
            {
                ErrorText = null;
                foreach (var resolution in result.GetAllReplacementKinds())
                {
                    switch (resolution)
                    {
                        case InlineRenameReplacementKind.ResolvedReferenceConflict:
                        case InlineRenameReplacementKind.ResolvedNonReferenceConflict:
                            _resolvableConflictCount++;
                            break;
                        case InlineRenameReplacementKind.UnresolvedConflict:
                            _unresolvableConflictCount++;
                            break;
                    }
                }
            }
            else
            {
                ErrorText = string.IsNullOrEmpty(session.ReplacementText)
                    ? null
                    : EditorFeaturesResources.The_new_name_is_not_a_valid_identifier;
            }

            UpdateSeverity();
            AllPropertiesChanged();
        }

        private void OnReplacementTextChanged(object sender, EventArgs args)
        {
            // When the new name changes, we need to update the display of the new name or the
            // instructional text, depending on whether the new name and the original name are
            // distinct.
            NotifyPropertyChanged(nameof(ShouldShowInstructions));
            NotifyPropertyChanged(nameof(ShouldShowNewName));
            NotifyPropertyChanged(nameof(NewNameDescription));
        }

        private void NotifyPropertyChanged([CallerMemberName] string name = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void AllPropertiesChanged()
            => NotifyPropertyChanged(string.Empty);

        private void UpdateSearchText(int referenceCount, int fileCount)
        {
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

            NotifyPropertyChanged("SearchText");
        }

        private void UpdateSeverity()
        {
            if (ErrorText != null ||
                _unresolvableConflictCount > 0)
            {
                _severity = RenameDashboardSeverity.Error;
            }
            else if (_resolvableConflictCount > 0)
            {
                _severity = RenameDashboardSeverity.Info;
            }
            else
            {
                _severity = RenameDashboardSeverity.None;
            }
        }

        private void CommitStateChange(object sender, bool commitStart)
            => CommitNotStart = !commitStart;

        public bool CommitNotStart
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _commitNotStart;
            }
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                if (_commitNotStart != value)
                {
                    _commitNotStart = value;
                    // Disable/Enable these checkbox in UI based on if commit is in-progress or not
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsRenameOverloadsEditable));
                    NotifyPropertyChanged(nameof(AllowFileRename));
                }
            }
        }

        public InlineRenameSession Session { get; }

        public RenameDashboardSeverity Severity => _severity;

        public bool AllowFileRename => Session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed && _isReplacementTextValid && CommitNotStart;
        public bool ShowFileRename => Session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;
        public string FileRenameString => Session.FileRenameInfo switch
        {
            InlineRenameFileRenameInfo.TypeDoesNotMatchFileName => EditorFeaturesResources.Rename_file_name_doesnt_match,
            InlineRenameFileRenameInfo.TypeWithMultipleLocations => EditorFeaturesResources.Rename_file_partial_type,
            _ => EditorFeaturesResources.Rename_symbols_file
        };

        public string HeaderText
        {
            get
            {
                return string.Format(EditorFeaturesResources.Rename_colon_0, Session.OriginalSymbolName);
            }
        }

        public string NewNameDescription
        {
            get
            {
                return string.Format(EditorFeaturesResources.New_name_colon_0, Session.ReplacementText);
            }
        }

        public bool ShouldShowInstructions
        {
            get
            {
                return Session.OriginalSymbolName == Session.ReplacementText;
            }
        }

        public bool ShouldShowNewName
        {
            get
            {
                return !ShouldShowInstructions;
            }
        }

        public string SearchText { get; private set; }

        public bool HasResolvableConflicts
        {
            get { return _resolvableConflictCount > 0; }
        }

        public string ResolvableConflictText
        {
            get
            {
                return _resolvableConflictCount >= 1
                    ? string.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, _resolvableConflictCount)
                    : null;
            }
        }

        public bool HasUnresolvableConflicts
        {
            get { return _unresolvableConflictCount > 0; }
        }

        public string UnresolvableConflictText
        {
            get
            {
                return _unresolvableConflictCount >= 1
                   ? string.Format(EditorFeaturesResources._0_unresolvable_conflict_s, _unresolvableConflictCount)
                   : null;
            }
        }

        public bool HasError
            => ErrorText != null;

        public string ErrorText { get; private set; }

        public Visibility RenameOverloadsVisibility
            => Session.HasRenameOverloads ? Visibility.Visible : Visibility.Collapsed;

        public bool IsRenameOverloadsEditable
            => !Session.MustRenameOverloads && CommitNotStart;

        public bool DefaultRenameOverloadFlag
        {
            get => Session.Options.RenameOverloads;

            set
            {
                if (IsRenameOverloadsEditable)
                {
                    Session.RenameService.GlobalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, value);
                    Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameOverloads = value });
                }
            }
        }

        public bool DefaultRenameInStringsFlag
        {
            get => Session.Options.RenameInStrings;

            set
            {
                Session.RenameService.GlobalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameInStrings = value });
            }
        }

        public bool DefaultRenameInCommentsFlag
        {
            get => Session.Options.RenameInComments;

            set
            {
                Session.RenameService.GlobalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameInComments = value });
            }
        }

        public bool DefaultRenameFileFlag
        {
            get => Session.Options.RenameFile;
            set
            {
                Session.RenameService.GlobalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, value);
                Session.RefreshRenameSessionWithOptionsChanged(Session.Options with { RenameFile = value });
            }
        }

        public bool DefaultPreviewChangesFlag
        {
            get => Session.PreviewChanges;

            set
            {
                Session.RenameService.GlobalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.PreviewChanges, value);
                Session.SetPreviewChanges(value);
            }
        }

        public string OriginalName => Session.OriginalSymbolName;

        public void Dispose()
        {
            Session.ReplacementTextChanged -= OnReplacementTextChanged;
            Session.ReferenceLocationsChanged -= OnReferenceLocationsChanged;
            Session.ReplacementsComputed -= OnReplacementsComputed;
            Session.CommitStateChange -= CommitStateChange;
        }
    }
}
