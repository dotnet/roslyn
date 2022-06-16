// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static class DocumentOutlineHelper
    {
        internal static List<DocumentSymbolViewModel> GetDocumentSymbols(DocumentSymbol[]? documentSymbols)
        {
            var documentSymbolModels = new List<DocumentSymbolViewModel>();
            if (documentSymbols is null || documentSymbols.Length == 0)
            {
                return documentSymbolModels;
            }

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolModel = new DocumentSymbolViewModel(documentSymbol);
                documentSymbolModel.Children = GetDocumentSymbols(documentSymbol.Children);
                documentSymbolModels.Add(documentSymbolModel);
            }

            return Sort(documentSymbolModels, SortOption.Order);
        }

        internal static List<DocumentSymbolViewModel> Sort(List<DocumentSymbolViewModel> documentSymbolModels, SortOption sortOption)
        {
            var sortedDocumentSymbolModels = sortOption switch
            {

                SortOption.Name => documentSymbolModels.OrderBy(x => x.Name),
                SortOption.Order => documentSymbolModels.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar),
                SortOption.Type => documentSymbolModels.OrderBy(x => x.SymbolKind).ThenBy(x => x.Name),
                _ => throw new NotImplementedException()
            };

            foreach (var documentSymbolModel in sortedDocumentSymbolModels)
            {
                documentSymbolModel.Children = Sort(documentSymbolModel.Children, sortOption);
            }

            return new List<DocumentSymbolViewModel>(sortedDocumentSymbolModels);
        }

        internal static bool SearchNodeTree(DocumentSymbolViewModel tree, string search)
        {
            if (tree.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var found = false;
            foreach (var childItem in tree.Children)
            {
                found = found || SearchNodeTree(childItem, search);
            }

            return found;
        }
    }
}
