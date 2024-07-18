// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SourceGeneratorItem(
    ProjectId projectId,
    SourceGeneratorIdentity identity,
    string? path) : BaseItem(identity.TypeName), IEquatable<SourceGeneratorItem>
{
    public ProjectId ProjectId { get; } = projectId;
    public SourceGeneratorIdentity Identity { get; } = identity;
    private readonly string? _path = path;

    // TODO: do we need an icon for our use?
    public override ImageMoniker IconMoniker => KnownMonikers.Process;

    public override object GetBrowseObject()
        => new BrowseObject(this);

    public override int GetHashCode()
        => Hash.Combine(this.Name,
           Hash.Combine(this.ProjectId,
           Hash.Combine(_path, this.Identity.GetHashCode())));

    public override bool Equals(object obj)
        => Equals(obj as SourceGeneratorItem);

    public bool Equals(SourceGeneratorItem? other)
    {
        return other != null &&
            this.Name == other.Name &&
            this.ProjectId == other.ProjectId &&
            this.Identity == other.Identity &&
            _path == other._path;
    }
}
