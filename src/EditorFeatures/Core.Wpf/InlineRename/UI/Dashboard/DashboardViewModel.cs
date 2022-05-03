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
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly InlineRenameSession _session;

        private DashboardSeverity _severity = DashboardSeverity.None;
        private string _searchText;
        private int _resolvableConflictCount;
        private int _unresolvableConflictCount;
        private string _errorText;
        private bool _isReplacementTextValid;

        public DashboardViewModel(InlineRenameSession session)
        {
            _session = session;
            _searchText = EditorFeaturesResources.Searching;

            _session.ReferenceLocationsChanged += OnReferenceLocationsChanged;
            _session.ReplacementsComputed += OnReplacementsComputed;
            _session.ReplacementTextChanged += OnReplacementTextChanged;

            // Set the flag to true by default if we're showing the option.
            _isReplacementTextValid = true;
            ComputeDefaultRenameFileFlag();
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
            ComputeDefaultRenameFileFlag();
            NotifyPropertyChanged(nameof(AllowFileRename));
        }

        private void ComputeDefaultRenameFileFlag()
        {
            // If replacementText is invalid, we won't rename the file.
            DefaultRenameFileFlag = _isReplacementTextValid && AllowFileRename && _session.Options.RenameFile;
        }

        private void OnReplacementsComputed(object sender, IInlineRenameReplacementInfo result)
        {
            var session = (InlineRenameSession)sender;
            _resolvableConflictCount = 0;
            _unresolvableConflictCount = 0;
            OnIsReplacementTextValidChanged(result.ReplacementTextValid);
            if (result.ReplacementTextValid)
            {
                _errorText = null;
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
                _errorText = string.IsNullOrEmpty(session.ReplacementText)
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
                _searchText = EditorFeaturesResources.Rename_will_update_1_reference_in_1_file;
            }
            else if (fileCount == 1)
            {
                _searchText = string.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, referenceCount);
            }
            else
            {
                _searchText = string.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_files, referenceCount, fileCount);
            }

            NotifyPropertyChanged("SearchText");
        }

        private void UpdateSeverity()
        {
            if (_errorText != null ||
                _unresolvableConflictCount > 0)
            {
                _severity = DashboardSeverity.Error;
            }
            else if (_resolvableConflictCount > 0)
            {
                _severity = DashboardSeverity.Info;
            }
            else
            {
                _severity = DashboardSeverity.None;
            }
        }

        public InlineRenameSession Session => _session;

        public DashboardSeverity Severity => _severity;

        public bool AllowFileRename => _session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed && _isReplacementTextValid;
        public bool ShowFileRename => _session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;
        public string FileRenameString => _session.FileRenameInfo switch
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

        public string SearchText => _searchText;

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
            => _errorText != null;

        public string ErrorText => _errorText;

        public Visibility RenameOverloadsVisibility
            => _session.HasRenameOverloads ? Visibility.Visible : Visibility.Collapsed;

        public bool IsRenameOverloadsEditable
            => !_session.MustRenameOverloads;

        public bool DefaultRenameOverloadFlag
        {
            get => _session.Options.RenameOverloads;

            set
            {
                if (IsRenameOverloadsEditable)
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameOverloads), value);
                    _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameOverloads = value });
                }
            }
        }

        public bool DefaultRenameInStringsFlag
        {
            get => _session.Options.RenameInStrings;

            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInStrings), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInStrings = value });
            }
        }

        public bool DefaultRenameInCommentsFlag
        {
            get => _session.Options.RenameInComments;

            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInComments), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInComments = value });
            }
        }

        public bool DefaultRenameFileFlag
        {
            get => _session.Options.RenameFile;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameFile), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameFile = value });
            }
        }

        public bool DefaultPreviewChangesFlag
        {
            get => _session.PreviewChanges;

            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.PreviewChanges), value);
                _session.SetPreviewChanges(value);
            }
        }

        public string OriginalName => _session.OriginalSymbolName;

        public void Dispose()
        {
            _session.ReplacementTextChanged -= OnReplacementTextChanged;
            _session.ReferenceLocationsChanged -= OnReferenceLocationsChanged;
            _session.ReplacementsComputed -= OnReplacementsComputed;
        }
    }
}
