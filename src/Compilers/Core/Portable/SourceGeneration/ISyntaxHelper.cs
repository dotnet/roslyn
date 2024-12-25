// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxHelper
    {
        bool IsCaseSensitive { get; }

        bool IsValidIdentifier(string name);

        bool IsAnyNamespaceBlock(SyntaxNode node);

        bool IsAttributeList(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node);

        void AddAttributeTargets(SyntaxNode node, ArrayBuilder<SyntaxNode> targets);

        bool IsAttribute(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

        bool IsLambdaExpression(SyntaxNode node);

        string GetUnqualifiedIdentifierOfName(SyntaxNode node);

        /// <summary>
        /// <paramref name="node"/> must be a compilation unit or namespace block.
        /// </summary>
        void AddAliases(GreenNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global);
        void AddAliases(CompilationOptions options, ArrayBuilder<(string aliasName, string symbolName)> aliases);

        bool ContainsGlobalAliases(SyntaxNode root);
    }

    internal abstract class AbstractSyntaxHelper : ISyntaxHelper
    {
        public abstract bool IsCaseSensitive { get; }

        public abstract bool IsValidIdentifier(string name);

        public abstract string GetUnqualifiedIdentifierOfName(SyntaxNode name);

        public abstract bool IsAnyNamespaceBlock(SyntaxNode node);

        public abstract bool IsAttribute(SyntaxNode node);
        public abstract SyntaxNode GetNameOfAttribute(SyntaxNode node);

        public abstract bool IsAttributeList(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node);
        public abstract void AddAttributeTargets(SyntaxNode node, ArrayBuilder<SyntaxNode> targets);

        public abstract bool IsLambdaExpression(SyntaxNode node);

        public abstract void AddAliases(GreenNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global);
        public abstract void AddAliases(CompilationOptions options, ArrayBuilder<(string aliasName, string symbolName)> aliases);

        public abstract bool ContainsGlobalAliases(SyntaxNode root);
    }
}
