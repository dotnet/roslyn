// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;

namespace Microsoft.CodeAnalysis.Editor.InlineRename.Adornment
{
    internal class InlineRenameAdornmentViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly InlineRenameSession _session;
        private OleComponent? _oleComponent;
        private bool _disposedValue;
        public event PropertyChangedEventHandler? PropertyChanged;

        public InlineRenameAdornmentViewModel(InlineRenameSession session)
        {
            _session = session;
            _session.ReplacementTextChanged += OnReplacementTextChanged;

            _previewChangesFlag = _session.PreviewChanges;
            _renameFileFlag = _session.Options.RenameFile;
            _renameInStringsFlag = _session.Options.RenameInStrings;
            _renameInCommentsFlag = _session.Options.RenameInComments;
            _renameOverloadsFlag = _session.Options.RenameOverloads;

            RegisterOleComponent();
        }

        public string IdentifierText
        {
            get => _session.ReplacementText;
            set
            {
                if (value != _session.ReplacementText)
                {
                    _session.ApplyReplacementText(value, propagateEditImmediately: false);
                    NotifyPropertyChanged(nameof(IdentifierText));
                }
            }
        }

        public bool AllowFileRename => _session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed;
        public bool ShowFileRename => _session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;

        public string FileRenameString => _session.FileRenameInfo switch
        {
            InlineRenameFileRenameInfo.TypeDoesNotMatchFileName => EditorFeaturesResources.Rename_file_name_doesnt_match,
            InlineRenameFileRenameInfo.TypeWithMultipleLocations => EditorFeaturesResources.Rename_file_partial_type,
            _ => EditorFeaturesResources.Rename_symbols_file
        };

        private bool _renameInCommentsFlag;
        public bool RenameInCommentsFlag
        {
            get => _renameInCommentsFlag;
            set
            {
                if (Set(ref _renameInCommentsFlag, value))
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInComments), value);
                    _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInComments = value });
                }
            }
        }

        private bool _renameInStringsFlag;
        public bool RenameInStringsFlag
        {
            get => _renameInStringsFlag;
            set
            {
                if (Set(ref _renameInStringsFlag, value))
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInStrings), value);
                    _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInStrings = value });
                }
            }
        }

        private bool _renameFileFlag;
        public bool RenameFileFlag
        {
            get => _renameFileFlag;
            set
            {
                if (Set(ref _renameFileFlag, value))
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameFile), value);
                    _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameFile = value });
                }
            }
        }

        private bool _previewChangesFlag;
        public bool PreviewChangesFlag
        {
            get => _previewChangesFlag;
            set
            {
                if (Set(ref _previewChangesFlag, value))
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.PreviewChanges), value);
                    _session.SetPreviewChanges(value);
                }
            }
        }

        private bool _renameOverloadsFlag;
        public bool RenameOverloadsFlag
        {
            get => _renameOverloadsFlag;
            set
            {
                if (Set(ref _renameOverloadsFlag, value))
                {
                    _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameOverloads), value);
                    _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameOverloads = value });
                }
            }
        }

        private bool _isCollapsed;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (Set(ref _isCollapsed, value))
                {
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
            => !_session.MustRenameOverloads;

        public void Submit()
        {
            _session.Commit();
        }

        public void Cancel()
        {
            _session.Cancel();
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
                    _session.ReplacementTextChanged -= OnReplacementTextChanged;

                    UnregisterOleComponent();
                }

                _disposedValue = true;
            }
        }

        private void OnReplacementTextChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(IdentifierText));
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
    }
}
