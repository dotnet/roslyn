﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Editor), Shared]
    internal sealed class FixAllGetFixesService : AbstractFixAllGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FixAllGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;

        protected override Solution? GetChangedSolution(Workspace workspace, Solution currentSolution, Solution? newSolution, string fixAllPreviewChangesTitle, string fixAllTopLevelHeader, Glyph glyph)
        {
            var previewService = workspace.Services.GetRequiredService<IPreviewDialogService>();

            var changedSolution = previewService.PreviewChanges(
                string.Format(EditorFeaturesResources.Preview_Changes_0, fixAllPreviewChangesTitle),
                "vs.codefix.fixall",
                fixAllTopLevelHeader,
                fixAllPreviewChangesTitle,
                glyph,
                newSolution,
                currentSolution);

            return changedSolution;
        }
    }
}
