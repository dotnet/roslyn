// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SourceGeneratorItem(
    ProjectId projectId,
    string name,
    SourceGeneratorIdentity identity,
    string? path) : BaseItem(name), IEquatable<SourceGeneratorItem>
{
    public ProjectId ProjectId { get; } = projectId;
    public SourceGeneratorIdentity Identity { get; } = identity;
    public string? Path { get; } = path;

    // TODO: do we need an icon for our use?
    public override ImageMoniker IconMoniker => KnownMonikers.Process;

    public override object GetBrowseObject()
        => new BrowseObject(this);

    public override int GetHashCode()
        => throw new NotImplementedException();

    public override bool Equals(object obj)
        => Equals(obj as SourceGeneratorItem);

    public bool Equals(SourceGeneratorItem? other)
    {
        return other != null &&
            this.Name == other.Name &&
            this.ProjectId == other.ProjectId &&
            this.Identity == other.Identity &&
            this.Path == other.Path;
    }
}
