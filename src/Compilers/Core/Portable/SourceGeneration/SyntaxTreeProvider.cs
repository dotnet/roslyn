// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides syntax trees for source texts in a compilation. This provider can be attached to a generator driver;
    /// see <see cref="GeneratorDriverState.SyntaxTreeProvider"/>.
    /// </summary>
    public abstract class SyntaxTreeProvider
    {
        /// <summary>
        /// Attempts to get a <see cref="SyntaxTree"/> for a <see cref="SourceText"/>.
        /// </summary>
        /// <remarks>
        /// When this method returns <see langword="true"/>, callers should verify that the resulting
        /// <see cref="SyntaxTree.Options"/> and <see cref="SyntaxTree.FilePath"/> match the values expected to be used
        /// for parsing the source text.
        /// </remarks>
        /// <param name="sourceText">The source text.</param>
        /// <param name="tree">A syntax tree for the source text; otherwise, <see langword="null"/> if no syntax tree is
        /// available for this source text.</param>
        /// <returns><see langword="true"/> if a syntax tree was available for the source text; otherwise,
        /// <see langword="false"/>.</returns>
        public abstract bool TryGetSyntaxTree(SourceText sourceText, [NotNullWhen(true)] out SyntaxTree? tree);

        /// <summary>
        /// Associates a specific <see cref="SyntaxTree"/> with a <see cref="SourceText"/>.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="tree">The syntax tree to associate with the source text.</param>
        public abstract void AddOrUpdate(SourceText text, SyntaxTree tree);
    }
}
