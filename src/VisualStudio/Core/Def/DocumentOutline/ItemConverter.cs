// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal class ItemConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is ImmutableArray<DocumentSymbolItemViewModel> children &&
                values[1] is SortOption sortOption)
            {
                return sortOption switch
                {
                    SortOption.Name => children.Sort(NameComparer.Instance),
                    SortOption.Type => children.Sort(TypeComparer.Instance),
                    SortOption.Location => children.Sort(LocationComparer.Instance),
                    _ => children
                };
            }

            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        private class NameComparer : IComparer<DocumentSymbolItemViewModel>
        {
            public static NameComparer Instance { get; } = new();

            public int Compare(DocumentSymbolItemViewModel x, DocumentSymbolItemViewModel y)
                => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
        }

        private class LocationComparer : IComparer<DocumentSymbolItemViewModel>
        {
            public static LocationComparer Instance { get; } = new();

            public int Compare(DocumentSymbolItemViewModel x, DocumentSymbolItemViewModel y)
                => x.StartPosition - y.StartPosition;
        }

        private class TypeComparer : IComparer<DocumentSymbolItemViewModel>
        {
            public static TypeComparer Instance { get; } = new();

            public int Compare(DocumentSymbolItemViewModel x, DocumentSymbolItemViewModel y)
            {
                if (x.SymbolKind == y.SymbolKind)
                    return NameComparer.Instance.Compare(x, y);

                return x.SymbolKind - y.SymbolKind;
            }
        }
    }
}
