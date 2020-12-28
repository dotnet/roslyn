// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides semantic models for syntax trees in a compilation.
    /// This provider can be attached to a compilation, see <see cref="Compilation.SemanticModelProvider"/>.
    /// </summary>
    internal abstract class SemanticModelProvider
    {
        /// <summary>
        /// Gets a <see cref="SemanticModel"/> for the given <paramref name="tree"/> that belongs to the given <paramref name="compilation"/>.
        /// </summary>
        public abstract SemanticModel GetSemanticModel(SyntaxTree tree, Compilation compilation, bool ignoreAccessibility = false);
    }
}
