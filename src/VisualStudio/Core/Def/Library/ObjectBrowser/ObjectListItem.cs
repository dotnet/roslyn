// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal abstract class ObjectListItem
{
    protected ObjectListItem(
        ProjectId projectId,
        StandardGlyphGroup glyphGroup,
        StandardGlyphItem glyphItem = StandardGlyphItem.GlyphItemPublic,
        bool isHidden = false)
    {
        ProjectId = projectId;

        GlyphIndex = glyphGroup < StandardGlyphGroup.GlyphGroupError
            ? (ushort)((int)glyphGroup + (int)glyphItem)
            : (ushort)glyphGroup;

        IsHidden = isHidden;
    }

    internal void SetParentList(ObjectList parentList)
    {
        Debug.Assert(ParentList == null);
        ParentList = parentList;
    }

    public virtual bool SupportsGoToDefinition
    {
        get { return false; }
    }

    public virtual bool SupportsFindAllReferences
    {
        get { return false; }
    }

    public abstract string DisplayText { get; }

    public abstract string FullNameText { get; }

    public abstract string SearchText { get; }

    public override string ToString()
        => DisplayText;

    public ObjectList ParentList { get; private set; }

    public ObjectListKind ParentListKind
    {
        get
        {
            return ParentList != null
                ? ParentList.Kind
                : ObjectListKind.None;
        }
    }

    public ProjectId ProjectId { get; }

    public async Task<Compilation> GetCompilationAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        var project = workspace.CurrentSolution.GetProject(ProjectId);
        if (project == null)
            return null;

        return await project.GetCompilationAsync(cancellationToken).ConfigureAwait(true);
    }

    public ushort GlyphIndex { get; }

    public bool IsHidden { get; }
}
