// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
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
            public readonly int Length;

            public SyntaxTreeInfo(string filePath, ParseOptions options, ValueSource<TextAndVersion> textSource, int length)
            {
                FilePath = filePath;
                Options = options;
                TextSource = textSource;
                Length = length;
            }

            internal bool TryGetText(out SourceText text)
            {
                TextAndVersion textAndVersion;
                if (this.TextSource.TryGetValue(out textAndVersion))
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
                return new SyntaxTreeInfo(path, this.Options, this.TextSource, this.Length);
            }

            internal SyntaxTreeInfo WithOptionsAndLength(ParseOptions options, int length)
            {
                return new SyntaxTreeInfo(this.FilePath, options, this.TextSource, length);
            }
        }

        internal sealed class RecoverableSyntaxRoot<TRoot> : RecoverableCachedObjectSource<TRoot>
            where TRoot : SyntaxNode
        {
            private ITemporaryStreamStorage storage;

            private readonly IRecoverableSyntaxTree<TRoot> containingTree;
            private readonly AbstractSyntaxTreeFactoryService service;

            public RecoverableSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                TRoot root,
                IRecoverableSyntaxTree<TRoot> containingTree)
                : base(new ConstantValueSource<TRoot>(containingTree.CloneNodeAsRoot(root)))
            {
                this.service = service;
                this.containingTree = containingTree;
            }

            private RecoverableSyntaxRoot(
                RecoverableSyntaxRoot<TRoot> originalRoot,
                IRecoverableSyntaxTree<TRoot> containingTree)
                : base(originalRoot)
            {
                this.service = originalRoot.service;
                this.storage = originalRoot.storage;
                this.containingTree = containingTree;
            }

            public RecoverableSyntaxRoot<TRoot> WithSyntaxTree(IRecoverableSyntaxTree<TRoot> containingTree)
            {
                TRoot root;
                if (this.TryGetValue(out root))
                {
                    var result = new RecoverableSyntaxRoot<TRoot>(this.service, root, containingTree);
                    result.storage = this.storage;
                    return result;
                }
                else
                {
                    return new RecoverableSyntaxRoot<TRoot>(this, containingTree);
                }
            }

            protected override async Task SaveAsync(TRoot root, CancellationToken cancellationToken)
            {
                // tree will be always held alive in memory, but nodes come and go. serialize nodes to storage
                using (var stream = SerializableBytes.CreateWritableStream())
                {
                    root.SerializeTo(stream, cancellationToken);
                    stream.Position = 0;

                    storage = this.service.LanguageServices.WorkspaceServices.GetService<ITemporaryStorageService>().CreateTemporaryStreamStorage(cancellationToken);
                    await storage.WriteStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            protected override async Task<TRoot> RecoverAsync(CancellationToken cancellationToken)
            {
                using (var stream = await storage.ReadStreamAsync(cancellationToken).ConfigureAwait(false))
                {
                    return RecoverRoot(stream, cancellationToken);
                }
            }

            protected override TRoot Recover(CancellationToken cancellationToken)
            {
                using (var stream = storage.ReadStream(cancellationToken))
                {
                    return RecoverRoot(stream, cancellationToken);
                }
            }

            private TRoot RecoverRoot(Stream stream, CancellationToken cancellationToken)
            {
                return containingTree.CloneNodeAsRoot((TRoot)this.service.DeserializeNodeFrom(stream, cancellationToken));
            }
        }
    }

    internal interface IRecoverableSyntaxTree<TRoot> where TRoot : SyntaxNode
    {
        TRoot CloneNodeAsRoot(TRoot root);
    }
}
