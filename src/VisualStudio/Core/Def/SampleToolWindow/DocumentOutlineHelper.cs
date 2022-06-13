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
            var docSymbols = new List<DocumentSymbolViewModel>();
            if (body is not null && body.Length > 0)
            {
                for (var i = 0; i < body.Length; i++)
                {
                    var documentSymbol = body[i];
                    var ds = new DocumentSymbolViewModel(
                        documentSymbol.Name,
                        documentSymbol.Kind,
                        documentSymbol.Range.Start.Line,
                        documentSymbol.Range.Start.Character,
                        documentSymbol.Range.End.Line,
                        documentSymbol.Range.End.Character);
                    var children = documentSymbol.Children;
                    if (children is not null)
                    {
                        ds = AddNodes(ds, children);
                    }

                    docSymbols.Add(ds);
                }
            }

            docSymbols = docSymbols.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar).ToList();
            for (var i = 0; i < docSymbols.Count; i++)
            {
                docSymbols[i].Children = Sort(docSymbols[i].Children, SortOption.Order);
            }

            return docSymbols;
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
                    var newChild = new DocumentSymbolViewModel(
                        child.Name,
                        child.Kind,
                        child.Range.Start.Line,
                        child.Range.Start.Character,
                        child.Range.End.Line,
                        child.Range.End.Character);
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

        internal static ObservableCollection<DocumentSymbolViewModel> Sort(ObservableCollection<DocumentSymbolViewModel> docSymbols, SortOption sortOption)
        {
            if (docSymbols.Count == 0)
            {
                return docSymbols;
            }

            var result = new List<DocumentSymbolViewModel>();
            switch (sortOption)
            {
                case SortOption.Name:
                    result = docSymbols.OrderBy(x => x.Name).ToList();
                    break;
                case SortOption.Order:
                    result = docSymbols.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar).ToList();
                    break;
                case SortOption.Type:
                    result = docSymbols.OrderBy(x => x.SymbolKind).ThenBy(x => x.Name).ToList();
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
