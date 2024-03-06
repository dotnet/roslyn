// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

internal sealed class QuickInfoHyperLink : IEquatable<QuickInfoHyperLink?>
{
    private readonly Workspace _workspace;

    public QuickInfoHyperLink(Workspace workspace, Uri uri)
    {
        _workspace = workspace;
        Uri = uri;

        NavigationAction = OpenLink;
    }

    public Action NavigationAction { get; }

    public Uri Uri { get; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as QuickInfoHyperLink);
    }

    public bool Equals(QuickInfoHyperLink? other)
    {
        return EqualityComparer<Uri?>.Default.Equals(Uri, other?.Uri);
    }

    public override int GetHashCode()
    {
        return Uri.GetHashCode();
    }

    private void OpenLink()
    {
        var navigateToLinkService = _workspace.Services.GetRequiredService<INavigateToLinkService>();
        _ = navigateToLinkService.TryNavigateToLinkAsync(Uri, CancellationToken.None);
    }

    internal readonly struct TestAccessor
    {
        public static Action CreateNavigationAction(Uri uri)
        {
            // The workspace is not validated by tests
            return new QuickInfoHyperLink(null!, uri).NavigationAction;
        }
    }
}
