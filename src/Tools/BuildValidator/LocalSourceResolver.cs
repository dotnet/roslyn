// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    internal class LocalSourceResolver
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public LocalSourceResolver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LocalSourceResolver>();
            _httpClient = new HttpClient();
        }

        public Task<(string SourceFilePath, SourceText SourceText)> ResolveSourceAsync(SourceFileInfo sourceFileInfo, ImmutableArray<SourceLink> sourceLinks, Encoding encoding)
        {
            var name = sourceFileInfo.SourceFilePath;

            // TODO: we need to try to fetch an embedded source from the PDB as a first resort
            // user projects will need to fetch AssemblyInfo.cs, source generator outputs, etc.
            if (sourceFileInfo.EmbeddedText is { } embeddedText)
            {
                return Task.FromResult((name, embeddedText));
            }
            else if (!File.Exists(name))
            {
                return ResolveHttpSourceAsync(sourceFileInfo, sourceLinks, encoding);
            }
            else
            {
                using var fileStream = File.OpenRead(name);
                var sourceText = SourceText.From(fileStream, encoding: encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256, canBeEmbedded: false);
                if (!sourceText.GetChecksum().AsSpan().SequenceEqual(sourceFileInfo.Hash))
                {
                    _logger.LogError($@"File ""{name}"" has incorrect hash");
                }
                return Task.FromResult((name, sourceText));
            }

            throw new FileNotFoundException(name);
        }

        private async Task<(string SourceFilePath, SourceText SourceText)> ResolveHttpSourceAsync(SourceFileInfo sourceFileInfo, ImmutableArray<SourceLink> sourceLinks, Encoding encoding)
        {
            if (!sourceLinks.IsDefault)
            {
                foreach (var link in sourceLinks)
                {
                    if (sourceFileInfo.SourceFilePath.StartsWith(link.Prefix))
                    {
                        var webPath = link.Replace + sourceFileInfo.SourceFilePath.Substring(link.Prefix.Length);
                        // TODO: retry? handle 404s?
                        var bytes = await _httpClient.GetByteArrayAsync(webPath);
                        // TODO: why don't we use the checksum algorithm from the SourceFileInfo
                        return (sourceFileInfo.SourceFilePath, SourceText.From(bytes, bytes.Length, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256, canBeEmbedded: false));
                    }
                }
            }

            throw new FileNotFoundException($@"Could not find a source link matching file ""{sourceFileInfo.SourceFilePath}""");
        }
    }
}
