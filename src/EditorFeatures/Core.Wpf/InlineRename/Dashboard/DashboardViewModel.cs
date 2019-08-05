// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Visibility _renameOverloadsVisibility;

        private DashboardSeverity _severity = DashboardSeverity.None;
        private readonly InlineRenameSession _session;

        private string _searchText;
        private int _resolvableConflictCount;
        private int _unresolvableConflictCount;
        private string _errorText;
        private bool _defaultRenameOverloadFlag;
        private readonly bool _isRenameOverloadsEditable;
        private bool _defaultRenameInStringsFlag;
        private bool _defaultRenameInCommentsFlag;
        private bool _defaultRenameFileFlag;
        private bool _defaultPreviewChangesFlag;


        public DashboardViewModel(InlineRenameSession session)
        {
            Contract.ThrowIfNull(session);
            _session = session;
            _searchText = EditorFeaturesResources.Searching;

            _renameOverloadsVisibility = session.HasRenameOverloads ? Visibility.Visible : Visibility.Collapsed;
            _isRenameOverloadsEditable = !session.ForceRenameOverloads;

            _defaultRenameOverloadFlag = session.OptionSet.GetOption(RenameOptions.RenameOverloads) || session.ForceRenameOverloads;
            _defaultRenameInStringsFlag = session.OptionSet.GetOption(RenameOptions.RenameInStrings);
            _defaultRenameInCommentsFlag = session.OptionSet.GetOption(RenameOptions.RenameInComments);
            _defaultPreviewChangesFlag = session.OptionSet.GetOption(RenameOptions.PreviewChanges);

            _session.ReferenceLocationsChanged += OnReferenceLocationsChanged;
            _session.ReplacementsComputed += OnReplacementsComputed;
            _session.ReplacementTextChanged += OnReplacementTextChanged;

            // Set the flag to true by default if we're showing the option. Use
            // the property so we correctly update the session as well
            DefaultRenameFileFlag = session.OptionSet.GetOption(RenameOptions.RenameFile) || AllowFileRename;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnReferenceLocationsChanged(object sender, ImmutableArray<InlineRenameLocation> renameLocations)
        {
            var totalFilesCount = renameLocations.GroupBy(s => s.Document).Count();
            var totalSpansCount = renameLocations.Length;

            UpdateSearchText(totalSpansCount, totalFilesCount);
        }

        private void OnReplacementsComputed(object sender, IInlineRenameReplacementInfo result)
        {
            var session = (InlineRenameSession)sender;
            _resolvableConflictCount = 0;
            _unresolvableConflictCount = 0;

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
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void AllPropertiesChanged()
        {
            NotifyPropertyChanged(string.Empty);
        }

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

        public bool AllowFileRename => _session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed;
        public bool ShowFileRename => _session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;
        public string FileRenameString => _session.FileRenameInfo switch
        {
            InlineRenameFileRenameInfo.TypeDoesNotMatchFileName => EditorFeaturesResources.Rename_file_name_doesnt_match,
            InlineRenameFileRenameInfo.TypeWithMultipleLocations => EditorFeaturesResources.Rename_file_partial_type,
            _ => EditorFeaturesResources.Rename_file
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
        {
            get { return _errorText != null; }
        }

        public string ErrorText => _errorText;

        public Visibility RenameOverloadsVisibility => _renameOverloadsVisibility;

        public bool IsRenameOverloadsEditable => _isRenameOverloadsEditable;

        public bool DefaultRenameOverloadFlag
        {
            get
            {
                return _defaultRenameOverloadFlag;
            }

            set
            {
                if (IsRenameOverloadsEditable)
                {
                    _defaultRenameOverloadFlag = value;
                    _session.RefreshRenameSessionWithOptionsChanged(RenameOptions.RenameOverloads, value);
                }
            }
        }

        public bool DefaultRenameInStringsFlag
        {
            get
            {
                return _defaultRenameInStringsFlag;
            }

            set
            {
                _defaultRenameInStringsFlag = value;
                _session.RefreshRenameSessionWithOptionsChanged(RenameOptions.RenameInStrings, value);
            }
        }

        public bool DefaultRenameInCommentsFlag
        {
            get
            {
                return _defaultRenameInCommentsFlag;
            }

            set
            {
                _defaultRenameInCommentsFlag = value;
                _session.RefreshRenameSessionWithOptionsChanged(RenameOptions.RenameInComments, value);
            }
        }

        public bool DefaultRenameFileFlag
        {
            get => _defaultRenameFileFlag;
            set
            {
                _defaultRenameFileFlag = value;
                _session.RefreshRenameSessionWithOptionsChanged(RenameOptions.RenameFile, value);
            }
        }

        public bool DefaultPreviewChangesFlag
        {
            get
            {
                return _defaultPreviewChangesFlag;
            }

            set
            {
                _defaultPreviewChangesFlag = value;
                _session.RefreshRenameSessionWithOptionsChanged(RenameOptions.PreviewChanges, value);
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
