// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// Sorts immutable collections of <see cref="DocumentSymbolDataViewModel"/>s 
/// </summary>
internal sealed class DocumentSymbolDataViewModelSorter : MarkupExtension, IMultiValueConverter
{
    public static DocumentSymbolDataViewModelSorter Instance { get; } = new();

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is ImmutableArray<DocumentSymbolDataViewModel> children &&
            values[1] is SortOption sortOption)
        {
            return children.Sort(GetComparer(sortOption));
        }

        return values[0];
    }

    public static IComparer<DocumentSymbolDataViewModel> GetComparer(SortOption sortOption)
        => sortOption switch
        {
            SortOption.Name => NameComparer.Instance,
            SortOption.Type => TypeComparer.Instance,
            SortOption.Location => LocationComparer.Instance,
            _ => throw ExceptionUtilities.UnexpectedValue(sortOption)
        };

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    public override object ProvideValue(IServiceProvider serviceProvider)
        => Instance;

    private sealed class NameComparer : IComparer<DocumentSymbolDataViewModel>
    {
        public static NameComparer Instance { get; } = new();

        public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
            => StringComparer.OrdinalIgnoreCase.Compare(x.Data.Name, y.Data.Name);
    }

    private sealed class LocationComparer : IComparer<DocumentSymbolDataViewModel>
    {
        public static LocationComparer Instance { get; } = new();

        public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
            => x.Data.RangeSpan.Start - y.Data.RangeSpan.Start;
    }

    private sealed class TypeComparer : IComparer<DocumentSymbolDataViewModel>
    {
        public static TypeComparer Instance { get; } = new();

        public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
            => x.Data.SymbolKind == y.Data.SymbolKind
                ? NameComparer.Instance.Compare(x, y)
                : x.Data.SymbolKind - y.Data.SymbolKind;
    }
}
