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
        internal static ObservableCollection<DocumentSymbolViewModel> GetDocumentSymbols(DocumentSymbol[]? body)
        {
            var documentSymbolModels = new ObservableCollection<DocumentSymbolViewModel>();
            if (body is null || body.Length == 0)
            {
                return documentSymbolModels;
            }

            for (var i = 0; i < body.Length; i++)
            {
                var documentSymbol = body[i];
                var ds = new DocumentSymbolViewModel(documentSymbol);
                var children = documentSymbol.Children;
                if (children is not null)
                {
                    ds = AddNodes(ds, children);
                }

                documentSymbolModels.Add(ds);
            }

            return Sort(documentSymbolModels, SortOption.Order);
        }

        internal static DocumentSymbolViewModel AddNodes(DocumentSymbolViewModel newNode, DocumentSymbol[] children)
        {
            var newChildren = new ObservableCollection<DocumentSymbolViewModel>();

            if (children is null || children.Length == 0)
            {
                return newNode;
            }
            else
            {
                for (var i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    var newChild = new DocumentSymbolViewModel(child);
                    if (child.Children is not null)
                    {
                        newChild = AddNodes(newChild, child.Children);
                    }

                    newChildren.Add(newChild);
                }

                newNode.Children = newChildren;
                return newNode;
            }
        }

        internal static ObservableCollection<DocumentSymbolViewModel> Sort(ObservableCollection<DocumentSymbolViewModel> documentSymbolModels, SortOption sortOption)
        {
            if (documentSymbolModels.Count <= 1)
            {
                return documentSymbolModels;
            }

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

            return new ObservableCollection<DocumentSymbolViewModel>(sortedDocumentSymbolModels);
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
