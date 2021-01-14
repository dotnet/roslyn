// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class CachingSyntaxTreeProvider : SyntaxTreeProvider
    {
        public static readonly CachingSyntaxTreeProvider Instance = new();

        private static readonly ConditionalWeakTable<SourceText, SyntaxTree> s_syntaxTrees = new();

        private CachingSyntaxTreeProvider()
        {
        }

        public override bool TryGetSyntaxTree(SourceText sourceText, [NotNullWhen(true)] out SyntaxTree? tree)
        {
            return s_syntaxTrees.TryGetValue(sourceText, out tree);
        }

        public override void AddOrUpdate(SourceText text, SyntaxTree tree)
        {
#if NETCOREAPP
            s_syntaxTrees.AddOrUpdate(text, tree);
#else
            s_syntaxTrees.Remove(text);
            s_syntaxTrees.Add(text, tree);
#endif
        }
    }
}
