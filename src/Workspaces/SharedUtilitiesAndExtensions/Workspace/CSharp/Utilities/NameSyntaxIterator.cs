﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
