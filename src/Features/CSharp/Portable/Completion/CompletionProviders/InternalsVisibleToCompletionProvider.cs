// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : AbstractInternalsVisibleToCompletionProvider
    {
        private static readonly AttributeNodeExtractor ExtractAttribute = new AttributeNodeExtractor();
        private static readonly AttributeConstructorArgumentExtractor ExtractAttributeConstructorArgument = new AttributeConstructorArgumentExtractor();

        protected override IImmutableList<SyntaxNode> GetAssemblyScopedAttributeSyntaxNodesOfDocument(SyntaxNode documentRoot)
        {
            var result = (documentRoot as CSharpSyntaxNode).Accept(ExtractAttribute);
            return result == null
                ? ImmutableList<SyntaxNode>.Empty
                : result.ToImmutableList();
        }

        protected override SyntaxNode GetConstructorArgumentOfInternalsVisibleToAttribute(SyntaxNode internalsVisibleToAttribute)
            => (internalsVisibleToAttribute as CSharpSyntaxNode).Accept(ExtractAttributeConstructorArgument);

        private class AttributeNodeExtractor : CSharpSyntaxVisitor<IEnumerable<SyntaxNode>>
        {
            public override IEnumerable<SyntaxNode> VisitCompilationUnit(CompilationUnitSyntax node)
            {
                foreach (var attributeList in node.AttributeLists)
                {
                    foreach (var attribute in attributeList.Accept(this))
                    {
                        yield return attribute;
                    }
                }
            }

            public override IEnumerable<SyntaxNode> VisitAttributeList(AttributeListSyntax node)
                => node.Attributes;
        }

        private class AttributeConstructorArgumentExtractor : CSharpSyntaxVisitor<SyntaxNode>
        {
            public override SyntaxNode VisitAttribute(AttributeSyntax node)
                => node.ArgumentList.Accept(this);

            public override SyntaxNode VisitAttributeArgumentList(AttributeArgumentListSyntax node)
            {
                //InternalsVisibleTo has only one constructor argument. 
                //https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.internalsvisibletoattribute(v=vs.110).aspx
                //We can assume that this is the assemblyName argument.
                foreach (var argument in node.Arguments)
                {
                    if (argument.NameEquals == null) // Ignore attribute properties
                    {
                        return argument.Accept(this);
                    }
                }

                return default(SyntaxNode);
            }

            public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node)
                => node.Expression;
        }
    }
}
