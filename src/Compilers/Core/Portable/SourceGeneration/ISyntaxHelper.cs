// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal interface ISyntaxHelper
    {
        bool IsCaseSensitive { get; }

        bool IsValidIdentifier(string name);

        bool IsCompilationUnit(SyntaxNode node);
        bool IsAnyNamespaceBlock(SyntaxNode node);

        bool IsAttributeList(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode attributeList);

        bool IsAttribute(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode attribute);

        SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode name);

        /// <summary>
        /// <paramref name="node"/> must be a compilation unit or namespace block.
        /// </summary>
        void AddAliases(SyntaxNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global);
    }

    internal abstract class AbstractSyntaxHelper : ISyntaxHelper
    {
        public abstract bool IsCaseSensitive { get; }

        public abstract bool IsValidIdentifier(string name);

        public abstract SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode name);

        public abstract bool IsCompilationUnit(SyntaxNode node);
        public abstract bool IsAnyNamespaceBlock(SyntaxNode node);

        public abstract bool IsAttribute(SyntaxNode node);
        public abstract SyntaxNode GetNameOfAttribute(SyntaxNode attribute);

        public abstract bool IsAttributeList(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode attributeList);

        public abstract void AddAliases(SyntaxNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global);
    }
}
