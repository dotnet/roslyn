// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class NameSyntaxIterator : IEnumerable<NameSyntax>
    {
        private readonly NameSyntax _name;

        public NameSyntaxIterator(NameSyntax name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public IEnumerator<NameSyntax> GetEnumerator()
        {
            var nodes = new LinkedList<NameSyntax>();

            var currentNode = _name;
            while (true)
            {
                if (currentNode.Kind() == SyntaxKind.QualifiedName)
                {
                    var qualifiedName = currentNode as QualifiedNameSyntax;
                    nodes.AddFirst(qualifiedName.Right);
                    currentNode = qualifiedName.Left;
                }
                else
                {
                    nodes.AddFirst(currentNode);
                    break;
                }
            }

            return nodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
