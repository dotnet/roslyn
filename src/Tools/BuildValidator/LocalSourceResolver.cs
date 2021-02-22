// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    internal record ResolvedSource(
        string? OnDiskPath,
        SourceText SourceText,
        SourceFileInfo SourceFileInfo)
    {
        public string DisplayPath => OnDiskPath ?? ("[embedded]" + SourceFileInfo.SourceFilePath);
    }

    internal class LocalSourceResolver
    {
        private readonly Options _options;
        private readonly ILogger _logger;

        public LocalSourceResolver(Options options, ILoggerFactory loggerFactory)
        {
            _options = options;
            _logger = loggerFactory.CreateLogger<LocalSourceResolver>();
        }

        public ResolvedSource ResolveSource(SourceFileInfo sourceFileInfo, ImmutableArray<SourceLink> sourceLinks, Encoding encoding)
        {
            var pdbDocumentPath = sourceFileInfo.SourceFilePath;
            if (sourceFileInfo.EmbeddedText is { } embeddedText)
            {
                return new ResolvedSource(OnDiskPath: null, embeddedText, sourceFileInfo);
            }
            else
            {
                string? onDiskPath = null;
                foreach (var link in sourceLinks)
                {
                    if (sourceFileInfo.SourceFilePath.StartsWith(link.Prefix))
                    {
                        onDiskPath = Path.GetFullPath(Path.Combine(_options.SourcePath, pdbDocumentPath.Substring(link.Prefix.Length)));
                        if (File.Exists(onDiskPath))
                        {
                            break;
                        }
                    }
                }

                // if no source links exist to let us prefix the source path,
                // then assume the file path in the pdb points to the on-disk location of the file.
                onDiskPath ??= pdbDocumentPath;

                using var fileStream = File.OpenRead(onDiskPath);
                var sourceText = SourceText.From(fileStream, encoding: encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256, canBeEmbedded: false);
                if (!sourceText.GetChecksum().AsSpan().SequenceEqual(sourceFileInfo.Hash))
                {
                    _logger.LogError($@"File ""{onDiskPath}"" has incorrect hash");
                }
                return new ResolvedSource(onDiskPath, sourceText, sourceFileInfo);
            }

            throw new FileNotFoundException(pdbDocumentPath);
        }
    }
}
