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

        /// <summary>
        /// Given an attribute target (like <c>int x</c>) remaps to the actual syntax node that the attribute is placed on
        /// (in this case the FieldDeclarationSyntax for <c>private int x, y;</c>
        /// </summary>
        SyntaxNode RemapAttributeTarget(SyntaxNode target);

        /// <summary>
        /// Given an attribute syntax, return the node that should be considered its owner.  This is generally, but not always
        /// the parent of the AttributeListSyntax the attribute is in.
        /// </summary>
        SyntaxNode GetAttributeOwningNode(SyntaxNode attribute);

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

        public abstract SyntaxNode RemapAttributeTarget(SyntaxNode target);
        public abstract SyntaxNode GetAttributeOwningNode(SyntaxNode attribute);

        public abstract bool IsAttributeList(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node);
        public abstract void AddAttributeTargets(SyntaxNode node, ArrayBuilder<SyntaxNode> targets);

        public abstract bool IsLambdaExpression(SyntaxNode node);

        public abstract void AddAliases(GreenNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global);
        public abstract void AddAliases(CompilationOptions options, ArrayBuilder<(string aliasName, string symbolName)> aliases);

        public abstract bool ContainsGlobalAliases(SyntaxNode root);
    }
}
