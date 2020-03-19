// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public ExternalReferenceItem(
            DefinitionItem definition,
            string projectName,
            object text,
            string displayPath)
        {
            Definition = definition;
            ProjectName = projectName;
            Text = text;
            DisplayPath = displayPath;
        }

        public string ProjectName { get; }
        /// <remarks>
        /// Must be of type Microsoft.VisualStudio.Text.Adornments.ImageElement or
        /// Microsoft.VisualStudio.Text.Adornments.ContainerElement or
        /// Microsoft.VisualStudio.Text.Adornments.ClassifiedTextElement or System.String
        /// </remarks> 
        public object Text { get; }
        public string DisplayPath { get; }

        public abstract bool TryNavigateTo(Workspace workspace, bool isPreview);
    }
}
