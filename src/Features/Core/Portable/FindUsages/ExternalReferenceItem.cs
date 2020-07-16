// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindUsages
{
    /// <summary>
    /// Information about a symbol's reference that can be used for display and navigation in an
    /// editor.  These generally reference items outside of the Roslyn <see cref="Solution"/> model
    /// provided by external sources such as online indices.
    /// </summary>
    internal abstract class ExternalReferenceItem
    {
        /// <summary>
        /// The definition this reference corresponds to.
        /// </summary>
        public DefinitionItem Definition { get; }

        public string Repository { get; }
        public ExternalScope Scope { get; }
        public string ProjectName { get; }
        public string DisplayPath { get; }
        public LinePositionSpan Span { get; }
        public string Text { get; }

        public ExternalReferenceItem(
            DefinitionItem definition,
            string repository,
            ExternalScope scope,
            string projectName,
            string displayPath,
            LinePositionSpan span,
            string text)
        {
            Definition = definition;
            Repository = repository;
            Scope = scope;
            ProjectName = projectName;
            DisplayPath = displayPath;
            Span = span;
            Text = text;
        }

        public abstract bool TryNavigateTo(bool isPreview);
    }
}
