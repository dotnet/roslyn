// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            public SyntaxTreeInfo(string filePath, ParseOptions options, ValueSource<TextAndVersion> textSource, Encoding encoding, int length)
            {
                FilePath = filePath ?? string.Empty;
                Options = options;
                TextSource = textSource;
                Encoding = encoding;
                Length = length;
            }

            internal bool TryGetText(out SourceText text)
            {
                if (this.TextSource.TryGetValue(out var textAndVersion))
                {
                    text = textAndVersion.Text;
                    return true;
                }
                else
                {
                    text = null;
                    return false;
                }
            }

            internal async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
            {
                var textAndVersion = await this.TextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return textAndVersion.Text;
            }

            internal SyntaxTreeInfo WithFilePath(string path)
            {
                return new SyntaxTreeInfo(path, this.Options, this.TextSource, this.Encoding, this.Length);
            }

            internal SyntaxTreeInfo WithOptionsAndLength(ParseOptions options, int length)
            {
                return new SyntaxTreeInfo(this.FilePath, options, this.TextSource, this.Encoding, length);
            }
        }

        internal sealed class RecoverableSyntaxRoot<TRoot> : RecoverableWeakValueSource<TRoot>
            where TRoot : SyntaxNode
        {
            private ITemporaryStreamStorage _storage;

            private readonly IRecoverableSyntaxTree<TRoot> _containingTree;
            private readonly AbstractSyntaxTreeFactoryService _service;

            /// <summary>
            /// If we get a very large tree, it may be the case that we can't actually persist
            /// it to a stream without blowing the stack.  In this case, we hold onto the tree 
            /// so that it can be recovered properly. Because ths value will also be inside
            /// <see cref="RecoverableWeakValueSource{T}._weakInstance"/>, the strong reference
            /// here will prevent it from going away.  Future calls to get the value will then
            /// always succeed.
            /// </summary>
            private TRoot _rootStrongReference;

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
                _service = originalRoot._service;
                _storage = originalRoot._storage;
                _containingTree = containingTree;
            }

            public RecoverableSyntaxRoot<TRoot> WithSyntaxTree(IRecoverableSyntaxTree<TRoot> containingTree)
            {
                if (this.TryGetValue(out var root))
                {
                    var result = new RecoverableSyntaxRoot<TRoot>(_service, root, containingTree);
                    result._storage = _storage;
                    return result;
                }
                else
                {
                    return new RecoverableSyntaxRoot<TRoot>(this, containingTree);
                }
            }

            protected override async Task SaveAsync(TRoot root, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_storage == null); // Cannot save more than once

                // tree will be always held alive in memory, but nodes come and go. serialize nodes to storage
                using (var stream = SerializableBytes.CreateWritableStream())
                {
                    try
                    {
                        root.SerializeTo(stream, cancellationToken);
                    }
                    catch (Exception e) when (e is ObjectWriter.RecursionDepthExceeded || StackGuard.IsInsufficientExecutionStackException(e))
                    {
                        // If we couldn't serialize out this tree (because it was too large). Then 
                        // just hold onto it in memory.  This will keep it alive in the weak references
                        // above us.
                        _rootStrongReference = root;
                        return;
                    }

                    stream.Position = 0;

                    _storage = _service.LanguageServices.WorkspaceServices.GetService<ITemporaryStorageService>().CreateTemporaryStreamStorage(cancellationToken);
                    await _storage.WriteStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override async Task<TRoot> RecoverAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (var stream = await _storage.ReadStreamAsync(cancellationToken).ConfigureAwait(false))
                {
                    return RecoverRoot(stream, cancellationToken);
                }
            }

            protected override TRoot Recover(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_storage);

                using (var stream = _storage.ReadStream(cancellationToken))
                {
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
        TRoot CloneNodeAsRoot(TRoot root);
    }
}