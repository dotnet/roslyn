// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal abstract partial class ReferenceChange : AbstractChange
{
    private readonly ProjectId _projectId;
    private readonly string _projectName;
    private readonly bool _isAddedReference;

    protected ReferenceChange(ProjectId projectId, string projectName, bool isAddedReference, PreviewEngine engine)
        : base(engine)
    {
        _projectId = projectId;
        _projectName = projectName;
        _isAddedReference = isAddedReference;
    }

    public static void AppendReferenceChanges(IEnumerable<ProjectChanges> projectChangesList, PreviewEngine engine, ArrayBuilder<AbstractChange> builder)
    {
        foreach (var projectChanges in projectChangesList)
        {
            var projectId = projectChanges.ProjectId;
            var oldSolution = projectChanges.OldProject.Solution;
            var newSolution = projectChanges.NewProject.Solution;
            var projectName = oldSolution.GetProject(projectId).Name;

            // Metadata references
            var addedMetadataReferenceChanges = projectChanges
                .GetAddedMetadataReferences()
                .Select(r => new MetadataReferenceChange(r, projectId, projectName, isAdded: true, engine: engine));
            builder.AddRange(addedMetadataReferenceChanges);

            var removedMetadataReferenceChanges = projectChanges
                .GetRemovedMetadataReferences()
                .Select(r => new MetadataReferenceChange(r, projectId, projectName, isAdded: false, engine: engine));
            builder.AddRange(removedMetadataReferenceChanges);

            // Project references
            var addedProjectReferenceChanges = projectChanges
                .GetAddedProjectReferences()
                .Select(r => new ProjectReferenceChange(r, newSolution.GetProject(r.ProjectId).Name, projectId, projectName, isAdded: true, engine: engine));
            builder.AddRange(addedProjectReferenceChanges);

            var removedProjectReferenceChanges = projectChanges
                .GetRemovedProjectReferences()
                .Select(r => new ProjectReferenceChange(r, oldSolution.GetProject(r.ProjectId).Name, projectId, projectName, isAdded: false, engine: engine));
            builder.AddRange(removedProjectReferenceChanges);

            // Analyzer references
            var addedAnalyzerReferenceChanges = projectChanges
                .GetAddedAnalyzerReferences()
                .Select(r => new AnalyzerReferenceChange(r, projectId, projectName, isAdded: true, engine: engine));
            builder.AddRange(addedAnalyzerReferenceChanges);

            var removedAnalyzerReferenceChanges = projectChanges
                .GetRemovedAnalyzerReferences()
                .Select(r => new AnalyzerReferenceChange(r, projectId, projectName, isAdded: false, engine: engine));
            builder.AddRange(removedAnalyzerReferenceChanges);
        }
    }

    protected ProjectId ProjectId { get { return _projectId; } }
    internal bool IsAddedReference { get { return _isAddedReference; } }
    protected string ProjectName { get { return _projectName; } }

    protected abstract string GetDisplayText();
    internal abstract Solution AddToSolution(Solution solution);
    internal abstract Solution RemoveFromSolution(Solution solution);

    public override int GetText(out VSTREETEXTOPTIONS tto, out string pbstrText)
    {
        var displayText = GetDisplayText();
        if (IsAddedReference)
        {
            pbstrText = ServicesVSResources.bracket_plus_bracket + displayText;
        }
        else
        {
            pbstrText = ServicesVSResources.bracket_bracket + displayText;
        }

        tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
        return VSConstants.S_OK;
    }

    public sealed override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string pbstrText)
    {
        eTipType = VSTREETOOLTIPTYPE.TIPTYPE_DEFAULT;
        pbstrText = null;
        return VSConstants.E_FAIL;
    }

    public sealed override int OnRequestSource(object pIUnknownTextView)
    {
        // When adding a project reference `Children` can be null, so check before looking at `Changes`
        if (pIUnknownTextView != null && Children?.Changes != null && Children.Changes.Length > 0)
        {
            engine.SetTextView(pIUnknownTextView);
            UpdatePreview();
        }

        return VSConstants.S_OK;
    }

    public sealed override void UpdatePreview()
    {
        // Don't need any preview updates for reference changes.
    }

    internal sealed override void GetDisplayData(VSTREEDISPLAYDATA[] pData)
        => pData[0].Image = pData[0].SelectedImage = (ushort)StandardGlyphGroup.GlyphReference;
}
