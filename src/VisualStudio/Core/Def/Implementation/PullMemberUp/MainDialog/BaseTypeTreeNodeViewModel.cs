﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    /// <summary>
    /// View model used to represent and display the inheritance graph as a tree. This tree is constructed by breadth first searching.
    /// If one type is the common base type of several other types, it will be showed multiple time.
    /// </summary>
    internal class BaseTypeTreeNodeViewModel : SymbolViewModel<INamedTypeSymbol>
    {
        /// <summary>
        /// Base types of this tree node
        /// </summary>
        public ImmutableArray<BaseTypeTreeNodeViewModel> BaseTypeNodes { get; private set; }

        public bool IsExpanded { get; set; }

        /// <summary>
        /// Content of the tooltip.
        /// </summary>
        public string Namespace => string.Format(ServicesVSResources.Namespace_0, Symbol.ContainingNamespace?.ToDisplayString() ?? "global");

        private BaseTypeTreeNodeViewModel(INamedTypeSymbol node, IGlyphService glyphService) : base(node, glyphService)
        {
        }

        /// <summary>
        /// Use breadth first search to create the inheritance tree. Only non-generated types in the solution will be included in the tree.
        /// </summary>
        public static BaseTypeTreeNodeViewModel CreateBaseTypeTree(
            IGlyphService glyphService,
            Solution solution,
            INamedTypeSymbol root,
            CancellationToken cancellationToken)
        {
            var rootTreeNode = new BaseTypeTreeNodeViewModel(root, glyphService) { IsChecked = false, IsExpanded = true };
            var queue = new Queue<BaseTypeTreeNodeViewModel>();
            queue.Enqueue(rootTreeNode);
            while (queue.Any())
            {
                var currentTreeNode = queue.Dequeue();
                var currentTypeSymbol = currentTreeNode.Symbol;

                currentTreeNode.BaseTypeNodes = currentTypeSymbol.Interfaces
                    .Concat(currentTypeSymbol.BaseType)
                    .Where(baseType => baseType != null && MemberAndDestinationValidator.IsDestinationValid(solution, baseType, cancellationToken))
                    .OrderBy(baseType => baseType.ToDisplayString())
                    .Select(baseType => new BaseTypeTreeNodeViewModel(baseType, glyphService) { IsChecked = false, IsExpanded = true })
                    .ToImmutableArray();

                foreach (var node in currentTreeNode.BaseTypeNodes)
                {
                    queue.Enqueue(node);
                }
            }

            return rootTreeNode;
        }
    }
}
