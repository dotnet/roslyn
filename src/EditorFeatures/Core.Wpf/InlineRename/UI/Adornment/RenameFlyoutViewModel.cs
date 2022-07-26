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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class RenameFlyoutViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly InlineRenameSession _session;
        private readonly bool _registerOleComponent;
        private OleComponent? _oleComponent;
        private bool _disposedValue;
        private bool _isReplacementTextValid = true;
        public event PropertyChangedEventHandler? PropertyChanged;

        public RenameFlyoutViewModel(InlineRenameSession session, TextSpan selectionSpan, bool registerOleComponent)
        {
            _session = session;
            _registerOleComponent = registerOleComponent;
            _session.ReplacementTextChanged += OnReplacementTextChanged;
            _session.ReplacementsComputed += OnReplacementsComputed;
            StartingSelection = selectionSpan;

            ComputeRenameFile();
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

        public InlineRenameSession Session => _session;

        public bool AllowFileRename => _session.FileRenameInfo == InlineRenameFileRenameInfo.Allowed && _isReplacementTextValid;
        public bool ShowFileRename => _session.FileRenameInfo != InlineRenameFileRenameInfo.NotAllowed;

        public string FileRenameString => _session.FileRenameInfo switch
        {
            InlineRenameFileRenameInfo.TypeDoesNotMatchFileName => EditorFeaturesResources.Rename_file_name_doesnt_match,
            InlineRenameFileRenameInfo.TypeWithMultipleLocations => EditorFeaturesResources.Rename_file_partial_type,
            _ => EditorFeaturesResources.Rename_symbols_file
        };

        public bool RenameInCommentsFlag
        {
            get => _session.Options.RenameInComments;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInComments), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInComments = value });
            }
        }

        public bool RenameInStringsFlag
        {
            get => _session.Options.RenameInStrings;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameInStrings), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameInStrings = value });
            }
        }

        public bool RenameFileFlag
        {
            get => _session.Options.RenameFile;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameFile), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameFile = value });
            }
        }

        public bool PreviewChangesFlag
        {
            get => _session.PreviewChanges;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.PreviewChanges), value);
                _session.SetPreviewChanges(value);
            }
        }

        public bool RenameOverloadsFlag
        {
            get => _session.Options.RenameOverloads;
            set
            {
                _session.RenameService.GlobalOptions.SetGlobalOption(new OptionKey(InlineRenameSessionOptionsStorage.RenameOverloads), value);
                _session.RefreshRenameSessionWithOptionsChanged(_session.Options with { RenameOverloads = value });
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

        public bool IsRenameOverloadsVisible
            => _session.HasRenameOverloads;

        public TextSpan StartingSelection { get; }

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
                    _session.ReplacementTextChanged -= OnReplacementTextChanged;
                    _session.ReplacementsComputed -= OnReplacementsComputed;

                    UnregisterOleComponent();
                }

                _disposedValue = true;
            }
        }

        private void ComputeRenameFile()
        {
            // If replacementText is invalid, we won't rename the file.
            RenameFileFlag = _isReplacementTextValid && AllowFileRename && _session.Options.RenameFile;
        }

        private void OnReplacementTextChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(IdentifierText));
        }

        private void OnReplacementsComputed(object sender, IInlineRenameReplacementInfo result)
        {
            if (Set(ref _isReplacementTextValid, result.ReplacementTextValid, "IsReplacementTextValid"))
            {
                ComputeRenameFile();
                NotifyPropertyChanged(nameof(AllowFileRename));
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
    }
}
