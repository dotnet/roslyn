﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        internal struct SyntaxTreeInfo
        {
            public readonly string FilePath;
            public readonly ParseOptions Options;
            public readonly ValueSource<TextAndVersion> TextSource;
            public readonly Encoding Encoding;
            public readonly int Length;
            public readonly ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions;

            public SyntaxTreeInfo(
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource,
                Encoding encoding,
                int length,
                ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions)
            {
                RoslynDebug.Assert(diagnosticOptions is object);

                FilePath = filePath ?? string.Empty;
                Options = options;
                TextSource = textSource;
                Encoding = encoding;
                Length = length;
                DiagnosticOptions = diagnosticOptions;
            }

            internal bool TryGetText([MaybeNullWhen(false)]out SourceText text)
            {
                if (TextSource.TryGetValue(out var textAndVersion))
                {
                    text = textAndVersion.Text;
                    return true;
                }

                // Suppressing nullable warning due to https://github.com/dotnet/roslyn/issues/40266
                text = null!;
                return false;
            }

            internal async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
            {
                var textAndVersion = await TextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return textAndVersion.Text;
            }

            internal SyntaxTreeInfo WithFilePath(string path)
            {
                return new SyntaxTreeInfo(
                    path,
                    Options,
                    TextSource,
                    Encoding,
                    Length,
                    DiagnosticOptions);
            }

            internal SyntaxTreeInfo WithOptionsAndLength(ParseOptions options, int length)
            {
                return new SyntaxTreeInfo(
                    FilePath,
                    options,
                    TextSource,
                    Encoding,
                    length,
                    DiagnosticOptions);
            }

            internal SyntaxTreeInfo WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic> options)
            {
                RoslynDebug.Assert(options is object);
                return new SyntaxTreeInfo(
                    FilePath,
                    Options,
                    TextSource,
                    Encoding,
                    Length,
                    options);
            }
        }

        internal sealed class RecoverableSyntaxRoot<TRoot> : WeaklyCachedRecoverableValueSource<TRoot>
            where TRoot : SyntaxNode
        {
            private ITemporaryStreamStorage? _storage;

            private readonly IRecoverableSyntaxTree<TRoot> _containingTree;
            private readonly AbstractSyntaxTreeFactoryService _service;

            public RecoverableSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                TRoot root,
                IRecoverableSyntaxTree<TRoot> containingTree)
                : base(new ConstantValueSource<TRoot>(containingTree.CloneNodeAsRoot(root)))
            {
                _service = service;
                _containingTree = containingTree;
            }

            private RecoverableSyntaxRoot(
                RecoverableSyntaxRoot<TRoot> originalRoot,
                IRecoverableSyntaxTree<TRoot> containingTree)
                : base(originalRoot)
            {
                Contract.ThrowIfNull(originalRoot._storage);

                _service = originalRoot._service;
                _storage = originalRoot._storage;
                _containingTree = containingTree;
            }

            public RecoverableSyntaxRoot<TRoot> WithSyntaxTree(IRecoverableSyntaxTree<TRoot> containingTree)
            {
                // at this point, we should either have strongly held root or _storage should not be null
                if (TryGetValue(out var root))
                {
                    // we have strongly held root
                    return new RecoverableSyntaxRoot<TRoot>(_service, root, containingTree);
                }
                else
                {
                    // we have _storage here. _storage != null is checked inside
                    return new RecoverableSyntaxRoot<TRoot>(this, containingTree);
                }
            }

            protected override async Task SaveAsync(TRoot root, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                // tree will be always held alive in memory, but nodes come and go. serialize nodes to storage
                using var stream = SerializableBytes.CreateWritableStream();
                root.SerializeTo(stream, cancellationToken);
                stream.Position = 0;

                _storage = _service.LanguageServices.WorkspaceServices.GetRequiredService<ITemporaryStorageService>().CreateTemporaryStreamStorage(cancellationToken);
                await _storage.WriteStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            protected override async Task<TRoot> RecoverAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (RoslynEventSource.LogInformationalBlock(FunctionId.Workspace_Recoverable_RecoverRootAsync, _containingTree.FilePath, cancellationToken))
                {
                    using var stream = await _storage.ReadStreamAsync(cancellationToken).ConfigureAwait(false);
                    return RecoverRoot(stream, cancellationToken);
                }
            }

            protected override TRoot Recover(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (RoslynEventSource.LogInformationalBlock(FunctionId.Workspace_Recoverable_RecoverRoot, _containingTree.FilePath, cancellationToken))
                {
                    using var stream = _storage.ReadStream(cancellationToken);
                    return RecoverRoot(stream, cancellationToken);
                }
            }

            private TRoot RecoverRoot(Stream stream, CancellationToken cancellationToken)
            {
                return _containingTree.CloneNodeAsRoot((TRoot)_service.DeserializeNodeFrom(stream, cancellationToken));
            }
        }
    }

    internal interface IRecoverableSyntaxTree<TRoot> where TRoot : SyntaxNode
    {
        string FilePath { get; }

        TRoot CloneNodeAsRoot(TRoot root);
    }
}
