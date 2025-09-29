// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// A ViewModel over <see cref="DocumentSymbolData"/>. The only items that are mutable on this type are <see
/// cref="IsExpanded"/> and <see cref="IsSelected"/>. It is expected that these can be modified from any thread with
/// INotifyPropertyChanged notifications being marshalled to the correct thread by WPF if there needs to be a change
/// to the visual presentation.
/// </summary>
internal sealed class DocumentSymbolDataViewModel : INotifyPropertyChanged, IEquatable<DocumentSymbolDataViewModel>
{
    public DocumentSymbolData Data { get; }
    public ImmutableArray<DocumentSymbolDataViewModel> Children { get; }

    /// <summary>
    /// Necessary because we cannot convert to this type dynamically in WPF.
    /// </summary>
    public ImageMoniker ImageMoniker => Data.Glyph.GetImageMoniker();

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    } = false;

    public DocumentSymbolDataViewModel(
        DocumentSymbolData data,
        ImmutableArray<DocumentSymbolDataViewModel> children)
    {
        Data = data;
        Children = children;
    }

    private static readonly PropertyChangedEventArgs _isExpandedPropertyChangedEventArgs = new(nameof(IsExpanded));
    private static readonly PropertyChangedEventArgs _isSelectedPropertyChangedEventArgs = new(nameof(IsSelected));

    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, propertyName switch
        {
            nameof(IsExpanded) => _isExpandedPropertyChangedEventArgs,
            nameof(IsSelected) => _isSelectedPropertyChangedEventArgs,
            _ => new PropertyChangedEventArgs(propertyName)
        });

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty(ref bool field, bool value, [CallerMemberName] string propertyName = "")
    {
        // Note: we do not lock here. Worst case is that we fire multiple
        //       NotifyPropertyChanged events which WPF can handle.
        if (field == value)
            return;

        field = value;
        NotifyPropertyChanged(propertyName);
    }

    public override bool Equals(object obj)
        => Equals(obj as DocumentSymbolDataViewModel);

    public bool Equals(DocumentSymbolDataViewModel? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        // If two view models are in the same location (across edits), we consider them the same.  We want to treat
        // things like name edits as just mutating a model, not producing a new one.
        var translatedRangeSpan = this.Data.RangeSpan.TranslateTo(other.Data.RangeSpan.Snapshot, SpanTrackingMode.EdgeInclusive);
        return translatedRangeSpan == other.Data.RangeSpan;
    }

    public override int GetHashCode()
        => Data.GetHashCode();
}
