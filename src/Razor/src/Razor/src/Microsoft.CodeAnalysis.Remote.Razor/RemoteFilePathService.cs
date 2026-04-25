// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Export(typeof(IFilePathService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteFilePathService() : AbstractFilePathService
{
    public override Uri GetRazorDocumentUri(Uri virtualDocumentUri)
    {
        if (IsVirtualCSharpFile(virtualDocumentUri))
        {
            throw new InvalidOperationException("Can not get a Razor document from a generated document Uri in cohosting");
        }

        return base.GetRazorDocumentUri(virtualDocumentUri);
    }
}
