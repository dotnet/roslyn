// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    public sealed partial class CSharpCompilation
    {
        private ImmutableArray<SyntaxNode> GetTypesWithAttributeName(string attributeName)
        {
            var typeDecls = ArrayBuilder<TypeDeclarationSyntax>.GetInstance();
            // Only descend into nodes that can contain type delcarations
            Func<SyntaxNode, bool> shouldDescend = node => node is CompilationUnitSyntax ||
                                                           node is NamespaceDeclarationSyntax ||
                                                           node is TypeDeclarationSyntax;

            foreach (var tree in SyntaxTrees)
            {
                var nodes = tree.GetRoot().DescendantNodes(shouldDescend);
                foreach (var node in nodes)
                {
                    var typeDecl = node as TypeDeclarationSyntax;
                    if (typeDecl != null)
                    {
                        var attrs = typeDecl.AttributeLists.SelectMany(list => list.Attributes);
                        foreach (var attr in attrs)
                        {
                            if (attr.Name.GetUnqualifiedName().ToString() == attributeName)
                            {
                                typeDecls.Add(typeDecl);
                            }
                        }
                    }
                }
            }

            return ImmutableArray<SyntaxNode>.CastUp(typeDecls.ToImmutableOrEmptyAndFree());
        }

        internal override SourceGeneratorTypeContext GetSourceGeneratorTypeContext(
            ArrayBuilder<SyntaxTree> builder,
            string attributeName,
            string path,
            bool writeToDisk)
        {
            var matchingTypes = GetTypesWithAttributeName(attributeName);
            var parseOptions = CommonParseOptions(matchingTypes);

            return new Context(builder,
                               matchingTypes,
                               this,
                               path,
                               parseOptions,
                               writeToDisk);
        }

        private sealed class Context : SourceGeneratorTypeContext
        {
            public Context(ArrayBuilder<SyntaxTree> builder,
                           ImmutableArray<SyntaxNode> matchingTypes,
                           Compilation compilation,
                           string path,
                           ParseOptions parseOptions,
                           bool writeToDisk)
                : base(builder,
                       matchingTypes,
                       compilation,
                       path,
                       parseOptions,
                       writeToDisk) { }

            internal override SyntaxTree CreateSyntaxTree(string source,
                                                          ParseOptions options,
                                                          string path) =>
                SyntaxFactory.ParseSyntaxTree(source, options, path);
        }
    }
}
