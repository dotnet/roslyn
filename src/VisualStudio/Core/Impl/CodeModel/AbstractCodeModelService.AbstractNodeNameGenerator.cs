// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal partial class AbstractCodeModelService : ICodeModelService
    {
        protected abstract AbstractNodeNameGenerator CreateNodeNameGenerator();

        protected abstract class AbstractNodeNameGenerator
        {
            protected abstract bool IsNameableNode(SyntaxNode node);
            protected abstract void AppendNodeName(StringBuilder builder, SyntaxNode node);

            protected static void AppendDotIfNeeded(StringBuilder builder)
            {
                if (builder.Length > 0 &&
                    char.IsLetterOrDigit(builder[builder.Length - 1]))
                {
                    builder.Append('.');
                }
            }

            protected static void AppendArity(StringBuilder builder, int arity)
            {
                if (arity > 0)
                {
                    builder.Append("`" + arity);
                }
            }

            public string GenerateName(SyntaxNode node)
            {
                Debug.Assert(IsNameableNode(node));

                var builder = new StringBuilder();

                var ancestors = node.Ancestors().ToArray();
                for (var i = ancestors.Length - 1; i >= 0; i--)
                {
                    var ancestor = ancestors[i];

                    // We skip "unnameable" nodes to ensure that we don't add empty names
                    // for nodes like the compilation unit or field declarations.
                    if (IsNameableNode(ancestor))
                    {
                        AppendNodeName(builder, ancestor);
                    }
                }

                AppendNodeName(builder, node);

                return builder.ToString();
            }
        }
    }
}
