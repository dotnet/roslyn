// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    internal class LocalSourceResolver
    {
        internal Options Options { get; }
        internal ImmutableArray<SourceLinkEntry> SourceLinkEntries { get; }
        internal ILogger Logger { get; }

        public LocalSourceResolver(Options options, ImmutableArray<SourceLinkEntry> sourceLinkEntries, ILogger logger)
        {
            Options = options;
            SourceLinkEntries = sourceLinkEntries;
            Logger = logger;
        }

        public SourceText ResolveSource(SourceTextInfo sourceTextInfo)
        {
            var originalFilePath = sourceTextInfo.OriginalSourceFilePath;
            string? onDiskPath = null;
            foreach (var link in SourceLinkEntries)
            {
                if (originalFilePath.StartsWith(link.Prefix, FileNameEqualityComparer.StringComparison))
                {
                    onDiskPath = Path.GetFullPath(Path.Combine(Options.SourcePath, originalFilePath.Substring(link.Prefix.Length)));
                    if (File.Exists(onDiskPath))
                    {
                        break;
                    }
                }
            }

            // if no source links exist to let us prefix the source path,
            // then assume the file path in the pdb points to the on-disk location of the file.
            onDiskPath ??= originalFilePath;

            using var fileStream = File.OpenRead(onDiskPath);
            var sourceText = SourceText.From(fileStream, encoding: sourceTextInfo.SourceTextEncoding, checksumAlgorithm: sourceTextInfo.HashAlgorithm, canBeEmbedded: false);
            if (!sourceText.GetChecksum().SequenceEqual(sourceTextInfo.Hash))
            {
                throw new Exception($@"File ""{onDiskPath}"" has incorrect hash");
            }

            return sourceText;
        }
    }
}
