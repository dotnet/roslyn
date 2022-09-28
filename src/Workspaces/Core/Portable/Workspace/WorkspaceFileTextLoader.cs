// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <see cref="FileTextLoader"/> that uses workspace services (i.e. <see cref="ITextFactoryService"/>) to load file content.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal class WorkspaceFileTextLoader : FileTextLoader
    {
        private readonly ITextFactoryService _textFactory;

        internal WorkspaceFileTextLoader(SolutionServices services, string path, Encoding? defaultEncoding)
#pragma warning disable RS0030 // Do not used banned APIs
            : base(path, defaultEncoding)
#pragma warning restore
        {
            _textFactory = services.GetRequiredService<ITextFactoryService>();
        }

        private protected override SourceText CreateText(Stream stream, LoadTextOptions options, CancellationToken cancellationToken)
            => _textFactory.CreateText(stream, DefaultEncoding, options.ChecksumAlgorithm, cancellationToken);
    }
}
