// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

[ExportWorkspaceServiceFactory(typeof(IPreviewDialogService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PreviewDialogService(SVsServiceProvider serviceProvider) : IPreviewDialogService, IWorkspaceServiceFactory
{
    private readonly IVsPreviewChangesService _previewChanges = (IVsPreviewChangesService)serviceProvider.GetService(typeof(SVsPreviewChangesService));
    private readonly IComponentModel _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
    private readonly IVsImageService2 _imageService = (IVsImageService2)serviceProvider.GetService(typeof(SVsImageService));

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => this;

    public Solution PreviewChanges(
        string title,
        string helpString,
        string description,
        string topLevelName,
        Glyph topLevelGlyph,
        Solution newSolution,
        Solution oldSolution,
        bool showCheckBoxes = true)
    {
        var engine = new PreviewEngine(
            title,
            helpString,
            description,
            topLevelName,
            topLevelGlyph,
            newSolution,
            oldSolution,
            _componentModel,
            _imageService,
            showCheckBoxes);
        _previewChanges.PreviewChanges(engine);
        engine.CloseWorkspace();
        return engine.FinalSolution;
    }
}
