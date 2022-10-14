// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService
    {
        internal readonly struct SyntaxTreeInfo
        {
            public readonly string FilePath;
            public readonly ParseOptions Options;
            public readonly ITextAndVersionSource TextSource;
            public readonly LoadTextOptions LoadTextOptions;
            public readonly Encoding Encoding;
            public readonly int Length;
            public readonly bool ContainsDirectives;

            public SyntaxTreeInfo(
                string filePath,
                ParseOptions options,
                ITextAndVersionSource textSource,
                LoadTextOptions loadTextOptions,
                Encoding encoding,
                int length,
                bool containsDirectives)
            {
                FilePath = filePath ?? string.Empty;
                Options = options;
                TextSource = textSource;
                LoadTextOptions = loadTextOptions;
                Encoding = encoding;
                Length = length;
                ContainsDirectives = containsDirectives;
            }

            internal bool TryGetText([NotNullWhen(true)] out SourceText? text)
            {
                if (TextSource.TryGetValue(LoadTextOptions, out var textAndVersion))
                {
                    text = textAndVersion.Text;
                    return true;
                }

                text = null;
                return false;
            }

            internal SourceText GetText(CancellationToken cancellationToken)
                => TextSource.GetValue(LoadTextOptions, cancellationToken).Text;

            internal async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
            {
                var textAndVersion = await TextSource.GetValueAsync(LoadTextOptions, cancellationToken).ConfigureAwait(false);
                return textAndVersion.Text;
            }

            internal SyntaxTreeInfo WithFilePath(string path)
            {
                return new SyntaxTreeInfo(
                    path,
                    Options,
                    TextSource,
                    LoadTextOptions,
                    Encoding,
                    Length,
                    ContainsDirectives);
            }

            internal SyntaxTreeInfo WithOptionsAndLengthAndContainsDirectives(ParseOptions options, int length, bool containsDirectives)
            {
                return new SyntaxTreeInfo(
                    FilePath,
                    options,
                    TextSource,
                    LoadTextOptions,
                    Encoding,
                    length,
                    containsDirectives);
            }

            internal SyntaxTreeInfo WithOptions(ParseOptions options)
            {
                return new SyntaxTreeInfo(
                    FilePath,
                    options,
                    TextSource,
                    LoadTextOptions,
                    Encoding,
                    Length,
                    ContainsDirectives);
            }
        }
    }
}
