using System;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Host;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp
{
    internal partial class CSharpSyntaxTreeFactoryService
    {
        /// <summary>
        /// Represents a syntax tree that only has a weak reference to its 
        /// underlying data.  This way it can be passed around without forcing
        /// the underlying full tree to stay alive.  Think of it more as a 
        /// key that can be used to identify a tree rather than the tree itself.
        /// </summary>
        private class WeakSyntaxTree : SyntaxTree
        {
            private readonly CSharpSyntaxTreeFactoryService syntaxTreeFactory;
            private readonly Func<CancellationToken, IText> createText;
            private readonly string fileName;
            private readonly ParseOptions options;
            private readonly IRetainerFactory<CommonSyntaxTree> syntaxTreeRetainerFactory;
            private readonly NullSyntaxReference nullSyntaxReference;

            // protects mutable state of this class
            private readonly NonReentrantLock gate = new NonReentrantLock();

            // We hold onto the underlying tree weakly.  That way if we're asked for it again and
            // it's still alive, we can just return it.
            private IRetainer<CommonSyntaxTree> weakTreeRetainer;

            // NOTE(cyrusn): Extremely subtle.  We need to guarantee that there cannot be two
            // different 'root' nodes around for this tree at any time.  If that happened then we'd
            // violate lots of invariants in our system.  However, if we only have the
            // weakTreeReference then it's possible to get multiple roots.  One caller comes in and
            // gets the first root, and then the actual syntax tree is reclaimed .  The next caller
            // comes in and then gets a different underlying tree and a different underlying root.
            // In order to prevent that, we must ensure that we always give back the same root
            // (without forcing the root to stay alive).  To do that, we keep a weak reference around
            // to the root.  This means that as long as the root is held by someone, anyone else will
            // always see the same root if they ask for it.
            private WeakReference<SyntaxNode> weakRootReference;

            public WeakSyntaxTree(
                CSharpSyntaxTreeFactoryService syntaxTreeFactory,
                Func<CancellationToken, IText> createText,
                string fileName,
                ParseOptions options,
                IRetainerFactory<CommonSyntaxTree> syntaxTreeRetainerFactory)
            {
                this.syntaxTreeFactory = syntaxTreeFactory;
                this.createText = createText;
                this.fileName = fileName;
                this.options = options;
                this.syntaxTreeRetainerFactory = syntaxTreeRetainerFactory;

                this.nullSyntaxReference = new NullSyntaxReference(this);
            }

            private SyntaxTree GetUnderlyingTreeNoLock(CancellationToken cancellationToken = default(CancellationToken))
            {
                // See if we're still holding onto a tree we previously created
                var syntaxTree = (SyntaxTree)((weakTreeRetainer != null) ? weakTreeRetainer.GetValue() : null);
                if (syntaxTree != null)
                {
                    return syntaxTree;
                }

                // Ok.  We need to actually parse out a tree.
                cancellationToken.ThrowIfCancellationRequested();
                var text = createText(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                syntaxTree = syntaxTreeFactory.CreateSyntaxTree(this.fileName, text, options, cancellationToken);

                // Keep a weak reference to it so that we can retrieve it if 
                // anyone else is holding onto it.
                weakTreeRetainer = this.syntaxTreeRetainerFactory.CreateRetainer(syntaxTree);

                return syntaxTree;
            }

            public override string FilePath
            {
                get
                {
                    return this.fileName;
                }
            }

            public override ParseOptions Options
            {
                get
                {
                    return this.options;
                }
            }

            // TODO: we should lock here!
            public override IText GetText(CancellationToken cancellationToken)
            {
                var syntaxTree = (SyntaxTree)((weakTreeRetainer != null) ? weakTreeRetainer.GetValue() : null);
                if (syntaxTree != null)
                {
                    return syntaxTree.GetText(cancellationToken);
                }

                // Ok.  We need to actually parse out a tree.
                cancellationToken.ThrowIfCancellationRequested();
                return createText(cancellationToken);
            }

            public override SyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken))
            {
                // NOTE(cyrusn): We can only have one thread executing here at a time.  Otherwise
                // we could have a race condition where both created the new root and then one
                // stomped on the other.  By locking here we ensure that only one will succeed in
                // creating the new root. The other thread will either get that same root, or it
                // might get a new root (but only if the first root isn't being held onto by
                // anything).
                //
                // We lock on the weakRootReference here as it is safe to do so.  It is private
                // to us and allows us to save on all the extra space necessary to hold a lock.
                using (gate.DisposableWait(cancellationToken))
                {
                    SyntaxNode root = null;
                    if (weakRootReference == null || !weakRootReference.TryGetTarget(out root))
                    {
                        root = this.CloneNodeAsRoot(GetUnderlyingTreeNoLock(cancellationToken).GetRoot(cancellationToken));
                        weakRootReference = new WeakReference<SyntaxNode>(root);
                    }

                    return root;
                }
            }

            public override bool TryGetRoot(out SyntaxNode root)
            {
                root = null;
                var wr = this.weakRootReference;
                return wr != null && wr.TryGetTarget(out root);
            }

            // TODO: we should lock here!
            public override SyntaxTree WithChange(IText newText, params TextChangeRange[] changes)
            {
                return GetUnderlyingTreeNoLock().WithChange(newText, changes);
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                if (node != null)
                {
                    // many people will take references to nodes in this tree.  
                    // We don't actually want those references to keep the tree alive.
                    if (node.Span.Length == 0)
                    {
                        return new PathSyntaxReference(this, node);
                    }
                    else
                    {
                        return new PositionalSyntaxReference(this, node);
                    }
                }
                else
                {
                    return nullSyntaxReference;
                }
            }
        }
    }
}