// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking;

internal sealed class ValueTrackingTreeViewModel : INotifyPropertyChanged
{
    private Brush? _highlightBrush;
    public Brush? HighlightBrush
    {
        get => _highlightBrush;
        set => SetProperty(ref _highlightBrush, value);
    }

    public IClassificationFormatMap ClassificationFormatMap { get; }
    public ClassificationTypeMap ClassificationTypeMap { get; }
    public IEditorFormatMapService FormatMapService { get; }
    public ObservableCollection<TreeItemViewModel> Roots { get; } = [];
    public string AutomationName => ServicesVSResources.Value_Tracking;

    private TreeViewItemBase? _selectedItem;
    public TreeViewItemBase? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _selectedItemFile = "";
    public string SelectedItemFile
    {
        get => _selectedItemFile;
        set => SetProperty(ref _selectedItemFile, value);
    }

    private int _selectedItemLine;
    public int SelectedItemLine
    {
        get => _selectedItemLine;
        set => SetProperty(ref _selectedItemLine, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private int _loadingCount;
    public int LoadingCount
    {
        get => _loadingCount;
        set => SetProperty(ref _loadingCount, value);
    }

    public bool ShowDetails => SelectedItem is TreeItemViewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ValueTrackingTreeViewModel(IClassificationFormatMap classificationFormatMap, ClassificationTypeMap classificationTypeMap, IEditorFormatMapService formatMapService)
    {
        ClassificationFormatMap = classificationFormatMap;
        ClassificationTypeMap = classificationTypeMap;
        FormatMapService = formatMapService;

        var editorMap = FormatMapService.GetEditorFormatMap("text");
        SetHighlightBrush(editorMap);

        editorMap.FormatMappingChanged += (s, e) =>
        {
            SetHighlightBrush(editorMap);
        };

        PropertyChanged += Self_PropertyChanged;
    }

    private void SetHighlightBrush(IEditorFormatMap editorMap)
    {
        var properties = editorMap.GetProperties(ReferenceHighlightTag.TagId);
        HighlightBrush = properties["Background"] as Brush;
    }

    private void Self_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedItem))
        {
            if (SelectedItem is not null)
            {
                SelectedItem.IsNodeSelected = true;

                if (SelectedItem is TreeItemViewModel itemWithInfo)
                {
                    SelectedItemFile = itemWithInfo?.FileName ?? "";
                    SelectedItemLine = itemWithInfo?.LineNumber ?? 0;
                }
                else
                {
                    SelectedItemFile = string.Empty;
                    SelectedItemLine = 0;
                }
            }

            NotifyPropertyChanged(nameof(ShowDetails));
        }

        if (e.PropertyName == nameof(LoadingCount))
        {
            IsLoading = LoadingCount > 0;
        }
    }

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
