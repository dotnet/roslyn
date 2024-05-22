// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal abstract class ObjectListItem
{
    private readonly ProjectId _projectId;
    private ObjectList _parentList;
    private readonly ushort _glyphIndex;
    private readonly bool _isHidden;

    protected ObjectListItem(
        ProjectId projectId,
        StandardGlyphGroup glyphGroup,
        StandardGlyphItem glyphItem = StandardGlyphItem.GlyphItemPublic,
        bool isHidden = false)
    {
        _projectId = projectId;

        _glyphIndex = glyphGroup < StandardGlyphGroup.GlyphGroupError
            ? (ushort)((int)glyphGroup + (int)glyphItem)
            : (ushort)glyphGroup;

        _isHidden = isHidden;
    }

    internal void SetParentList(ObjectList parentList)
    {
        Debug.Assert(_parentList == null);
        _parentList = parentList;
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

    public ObjectList ParentList
    {
        get { return _parentList; }
    }

    public ObjectListKind ParentListKind
    {
        get
        {
            return _parentList != null
                ? _parentList.Kind
                : ObjectListKind.None;
        }
    }

    public ProjectId ProjectId
    {
        get
        {
            return _projectId;
        }
    }

    public Compilation GetCompilation(Workspace workspace)
    {
        var project = workspace.CurrentSolution.GetProject(_projectId);
        if (project == null)
        {
            return null;
        }

        return project
            .GetCompilationAsync(CancellationToken.None)
            .WaitAndGetResult_ObjectBrowser(CancellationToken.None);
    }

    public ushort GlyphIndex
    {
        get { return _glyphIndex; }
    }

    public bool IsHidden
    {
        get { return _isHidden; }
    }
}
