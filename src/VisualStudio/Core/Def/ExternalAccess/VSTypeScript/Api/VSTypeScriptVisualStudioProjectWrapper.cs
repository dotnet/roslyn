// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal struct VSTypeScriptVisualStudioProjectWrapper
    {
        public VSTypeScriptVisualStudioProjectWrapper(VisualStudioProject underlyingObject)
            => Project = underlyingObject;

        public bool IsDefault => Project == null;

        public ProjectId? Id => Project?.Id;

        public string? DisplayName
        {
            get => Project?.DisplayName;
            set
            {
                if (Project != null && value != null)
                {
                    Project.DisplayName = value;
                }
            }
        }

        public void AddSourceFile(string fullPath)
            => Project?.AddSourceFile(fullPath, SourceCodeKind.Regular);

        public void AddSourceTextContainer(SourceTextContainer sourceTextContainer, string fullPath)
            => Project?.AddSourceTextContainer(sourceTextContainer, fullPath, SourceCodeKind.Regular);

        public void RemoveSourceFile(string fullPath)
            => Project?.RemoveSourceFile(fullPath);

        public void RemoveSourceTextContainer(SourceTextContainer sourceTextContainer)
            => Project?.RemoveSourceTextContainer(sourceTextContainer);

        public void RemoveFromWorkspace()
            => Project?.RemoveFromWorkspace();

        internal VisualStudioProject? Project { get; }
    }
}
