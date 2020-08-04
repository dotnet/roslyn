// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    internal struct VSTypeScriptContainedLanguageWrapper
    {
        private readonly ContainedLanguage _underlyingObject;

        public VSTypeScriptContainedLanguageWrapper(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            Guid languageServiceGuid)
        {
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var filePath = ContainedLanguage.GetFilePathFromHierarchyAndItemId(hierarchy, itemid);

            _underlyingObject = new ContainedLanguage(
                bufferCoordinator,
                componentModel,
                workspace,
                project.Id,
                project.VisualStudioProject,
                filePath,
                languageServiceGuid,
                vbHelperFormattingRule: null);
        }

        public VSTypeScriptContainedLanguageWrapper(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            Workspace workspace,
            IVsHierarchy hierarchy,
            uint itemid,
            Guid languageServiceGuid)
        {
            var filePath = ContainedLanguage.GetFilePathFromHierarchyAndItemId(hierarchy, itemid);
            var projectId = ProjectId.CreateNewId($"Project for {filePath}");
            workspace.OnProjectAdded(ProjectInfo.Create(projectId, VersionStamp.Default, filePath, string.Empty, "TypeScript"));

            _underlyingObject = new ContainedLanguage(
                bufferCoordinator,
                componentModel,
                workspace,
                projectId,
                null,
                filePath,
                languageServiceGuid,
                vbHelperFormattingRule: null);
        }

        public bool IsDefault => _underlyingObject == null;

        public void DisconnectHost()
            => _underlyingObject.SetHost(null);
    }
}
