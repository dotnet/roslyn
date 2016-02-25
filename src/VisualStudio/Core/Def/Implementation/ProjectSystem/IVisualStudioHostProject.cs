// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        IVsHierarchy Hierarchy { get; }
        Guid Guid { get; }
        string ProjectType { get; }

        Workspace Workspace { get; }
        string ProjectSystemName { get; }

        IVisualStudioHostDocument GetDocumentOrAdditionalDocument(DocumentId id);
        IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath);

        ProjectInfo CreateProjectInfoForCurrentState();

        IReadOnlyList<string> GetFolderNames(uint documentItemID);
        bool ContainsFile(string moniker);

        IVisualStudioHostDocument AddGeneratedDocument(DocumentId id, string filePath);
        void RemoveGeneratedDocument(DocumentId id);
    }
}
