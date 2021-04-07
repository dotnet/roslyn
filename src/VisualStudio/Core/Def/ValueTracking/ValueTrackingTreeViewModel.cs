// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class ValueTrackingTreeViewModel : INotifyPropertyChanged
    {
        public ValueTrackingTreeViewModel(IClassificationFormatMap classificationFormatMap, ClassificationTypeMap classificationTypeMap, IEditorFormatMapService _formatMapService)
        {
            ClassificationFormatMap = classificationFormatMap;
            ClassificationTypeMap = classificationTypeMap;
            FormatMapService = _formatMapService;

            var properties = FormatMapService.GetEditorFormatMap("text")
                                          .GetProperties(ReferenceHighlightTag.TagId);

            HighlightBrush = properties["Background"] as Brush;
        }

        private Brush? _highlightBrush;
        public Brush? HighlightBrush
        {
            get => _highlightBrush;
            set => SetProperty(ref _highlightBrush, value);
        }

        public IClassificationFormatMap ClassificationFormatMap { get; }
        public ClassificationTypeMap ClassificationTypeMap { get; }
        public IEditorFormatMapService FormatMapService { get; }
        public ObservableCollection<ValueTrackingTreeItemViewModel> Roots { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            NotifyPropertyChanged(name);
        }

        private void NotifyPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
