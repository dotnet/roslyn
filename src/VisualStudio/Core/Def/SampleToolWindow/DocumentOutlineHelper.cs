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
        internal static List<DocumentSymbolViewModel> GetDocumentSymbols(DocumentSymbol[]? body)
        {
            var documentSymbolModels = new List<DocumentSymbolViewModel>();
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

            documentSymbolModels = documentSymbolModels.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar).ToList();
            for (var i = 0; i < documentSymbolModels.Count; i++)
            {
                documentSymbolModels[i].Children = Sort(documentSymbolModels[i].Children, SortOption.Order);
            }

            return documentSymbolModels;
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
            if (documentSymbolModels.Count == 0)
            {
                return documentSymbolModels;
            }

            var result = new List<DocumentSymbolViewModel>();
            switch (sortOption)
            {
                case SortOption.Name:
                    result = documentSymbolModels.OrderBy(x => x.Name).ToList();
                    break;
                case SortOption.Order:
                    result = documentSymbolModels.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar).ToList();
                    break;
                case SortOption.Type:
                    result = documentSymbolModels.OrderBy(x => x.SymbolKind).ThenBy(x => x.Name).ToList();
                    break;
            }

            for (var i = 0; i < result.Count; i++)
            {
                result[i].Children = Sort(result[i].Children, sortOption);
            }

            return new ObservableCollection<DocumentSymbolViewModel>(result);
        }

        internal static bool SearchNodeTree(DocumentSymbolViewModel tree, string search)
        {
            if (tree.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            else
            {
                var found = false;
                foreach (var childItem in tree.Children)
                {
                    found = found || SearchNodeTree(childItem, search);
                }

                return found;
            }
        }
    }
}
