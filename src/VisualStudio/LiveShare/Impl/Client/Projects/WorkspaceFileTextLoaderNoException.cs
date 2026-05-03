// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects;

/// <summary>
/// This is a FileTextLoader which no-ops if the file is not available on disk. This is the common case for
/// Cascade and throwing exceptions slows down GetText operations significantly enough to have visible UX impact.
/// </summary>
internal sealed class WorkspaceFileTextLoaderNoException : WorkspaceFileTextLoader
{
    public WorkspaceFileTextLoaderNoException(SolutionServices services, string path, Encoding defaultEncoding)
        : base(services, path, defaultEncoding)
    {
    }

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        if (!File.Exists(Path))
        {
            return Task.FromResult(TextAndVersion.Create(SourceText.From("", encoding: null, options.ChecksumAlgorithm), VersionStamp.Create()));
        }

        return base.LoadTextAndVersionAsync(options, cancellationToken);
    }
}
