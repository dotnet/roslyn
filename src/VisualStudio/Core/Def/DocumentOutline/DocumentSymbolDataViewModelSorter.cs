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

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Sorts immutable collections of <see cref="DocumentSymbolDataViewModel"/>s 
    /// </summary>
    internal class DocumentSymbolDataViewModelSorter : MarkupExtension, IMultiValueConverter
    {
        public static DocumentSymbolDataViewModelSorter Instance { get; } = new();

        public ImmutableArray<DocumentSymbolDataViewModel> Sort(ImmutableArray<DocumentSymbolDataViewModel> items, SortOption sortOption)
            => (ImmutableArray<DocumentSymbolDataViewModel>)Convert(new object[] { items, sortOption }, typeof(ImmutableArray<DocumentSymbolDataViewModel>), null, CultureInfo.CurrentCulture);

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is ImmutableArray<DocumentSymbolDataViewModel> children &&
                values[1] is SortOption sortOption)
            {
                return sortOption switch
                {
                    SortOption.Name => children.Sort(NameComparer.Instance),
                    SortOption.Type => children.Sort(TypeComparer.Instance),
                    SortOption.Location => children.Sort(LocationComparer.Instance),
                    _ => throw ExceptionUtilities.UnexpectedValue(sortOption)
                };
            }

            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        public override object ProvideValue(IServiceProvider serviceProvider)
            => Instance;

        private class NameComparer : IComparer<DocumentSymbolDataViewModel>
        {
            public static NameComparer Instance { get; } = new();

            public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
                => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
        }

        private class LocationComparer : IComparer<DocumentSymbolDataViewModel>
        {
            public static LocationComparer Instance { get; } = new();

            public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
                => x.StartPosition - y.StartPosition;
        }

        private class TypeComparer : IComparer<DocumentSymbolDataViewModel>
        {
            public static TypeComparer Instance { get; } = new();

            public int Compare(DocumentSymbolDataViewModel x, DocumentSymbolDataViewModel y)
                => x.SymbolKind == y.SymbolKind
                    ? NameComparer.Instance.Compare(x, y)
                    : x.SymbolKind - y.SymbolKind;
        }
    }
}
