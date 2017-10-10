// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// The interface implemented by all types of projects within Visual Studio (like regular
    /// projects, Miscellaneous files projects, etc.)
    /// </summary>
    internal interface IVisualStudioHostProject
    {
        ProjectId Id { get; }
        string Language { get; }

        /// <summary>
        /// The <see cref="IVsHierarchy"/> for this project.  NOTE: May be null in Deferred Project Load cases.
        /// </summary>
        IVsHierarchy Hierarchy { get; }
        Guid Guid { get; }

        Microsoft.CodeAnalysis.Workspace Workspace { get; }

        /// <summary>
        /// The public display name of the project. This name is not unique and may be shared
        /// between multiple projects, especially in cases like Venus where the intellisense
        /// projects will match the name of their logical parent project.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The name of the project according to the project system. In "regular" projects this is
        /// equivalent to <see cref="DisplayName"/>, but in Venus cases these will differ. The
        /// ProjectSystemName is the 2_Default.aspx project name, whereas the regular display name
        /// matches the display name of the project the user actually sees in the solution explorer.
        /// These can be assumed to be unique within the Visual Studio workspace.
        /// </summary>
        string ProjectSystemName { get; }

        IVisualStudioHostDocument GetDocumentOrAdditionalDocument(DocumentId id);
        IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath);

        ProjectInfo CreateProjectInfoForCurrentState();

        bool ContainsFile(string moniker);
    }
}
