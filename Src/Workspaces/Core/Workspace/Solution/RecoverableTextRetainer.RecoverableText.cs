using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Services.Host;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    internal partial class RecoverableTextRetainer
    {
        /// <summary>
        /// A fake IText wrapper that manages the lifetime and recoverability of the actual IText.
        /// </summary>
        private class RecoverableText : IText, IRetainedObject
        {
            private readonly ITemporaryStorageService storageService;
            private readonly NonReentrantLock gate = new NonReentrantLock();
            private IRetainer<TextAndVersion> retainedText;
            private WeakReference<IText> weakText;
            private VersionStamp originalVersion;
            private ITemporaryStorage storage;
            private bool evicted;

            public RecoverableText(ITemporaryStorageService storageService, IRetainer<TextAndVersion> sourceText)
            {
                this.storageService = storageService;
                this.retainedText = sourceText;
            }

            public TextAndVersion GetValue(CancellationToken cancellationToken = default(CancellationToken))
            {
                var textAndVersion = this.retainedText.GetValue(cancellationToken);
                if (textAndVersion == null)
                {
                    using (this.gate.DisposableWait(cancellationToken))
                    {
                        // must have been previously evicted.
                        // attempt to recover either from weak reference or storage
                        var text = this.weakText.GetTarget();
                        if (text == null)
                        {
                            text = this.storage.ReadText(cancellationToken);
                        }

                        textAndVersion = new TextAndVersion(text, this.originalVersion);

                        // after recovering keep alive here until next eviction
                        Interlocked.Exchange(ref this.retainedText, new StrongRetainer<TextAndVersion>(textAndVersion));
                    }
                }

                return textAndVersion;
            }

            public bool TryGetValue(out TextAndVersion value)
            {
                this.retainedText.TryGetValue(out value);

                if (value == null)
                {
                    var text = this.weakText != null ? this.weakText.GetTarget() : null;
                    if (text != null)
                    {
                        value = new TextAndVersion(text, this.originalVersion);
                    }
                }

                return value != null;
            }

            void IRetainedObject.OnEvicted()
            {
                using (this.gate.DisposableWait())
                {
                    // only write to temporary storage if we have ever observed it
                    TextAndVersion text;
                    if (!evicted && this.retainedText.TryGetValue(out text) && text != null)
                    {
                        this.evicted = true;

                        // do actual save asynchronously
                        StartSaveText();
                    }
                }
            }

            private static Task latestTask;
            private static readonly NonReentrantLock taskGuard = new NonReentrantLock();

            private void StartSaveText()
            {
                using (taskGuard.DisposableWait())
                {
                    if (latestTask == null)
                    {
                        latestTask = Task.Factory.StartNew(() => SaveText(CancellationToken.None));
                    }
                    else
                    {
                        // force all save tasks to be in sequence
                        latestTask = latestTask.ContinueWith(t => SaveText(CancellationToken.None));
                    }
                }
            }

            private void SaveText(CancellationToken cancellationToken)
            {
                // only write to temporary storage if we have ever observed it
                TextAndVersion textAndVersion;
                if (this.retainedText.TryGetValue(out textAndVersion) && textAndVersion != null)
                {
                    using (this.gate.DisposableWait(cancellationToken))
                    {
                        // only need to write to temp file the first time it is evicted.
                        // if it gets re-added to cache and re-evicted, it will still have the same content.
                        if (this.storage == null)
                        {
                            this.storage = this.storageService.CreateTemporaryStorage(CancellationToken.None);
                            this.storage.WriteText(textAndVersion.Text);
                            this.originalVersion = textAndVersion.Version;
                        }

                        // keep text alive as long as GC will allow
                        this.weakText = new WeakReference<IText>(textAndVersion.Text);
                        Interlocked.Exchange(ref this.retainedText, new StrongRetainer<TextAndVersion>(null));
                    }
                }
            }

            public override string ToString()
            {
                return this.GetValue().ToString();
            }

            #region IText

            [ExcludeFromCodeCoverage]
            ITextContainer IText.Container
            {
                get { return null; }
            }

            // used by costing function
            int IText.Length
            {
                get { return this.GetValue().Text.Length; }
            }

            [ExcludeFromCodeCoverage]
            int IText.LineCount
            {
                get { throw new NotImplementedException(); }
            }

            [ExcludeFromCodeCoverage]
            IEnumerable<ITextLine> IText.Lines
            {
                get { throw new NotImplementedException(); }
            }

            [ExcludeFromCodeCoverage]
            char IText.this[int position]
            {
                get { throw new NotImplementedException(); }
            }

            [ExcludeFromCodeCoverage]
            ITextLine IText.GetLineFromLineNumber(int lineNumber)
            {
                throw new NotImplementedException();
            }

            [ExcludeFromCodeCoverage]
            ITextLine IText.GetLineFromPosition(int position)
            {
                throw new NotImplementedException();
            }

            [ExcludeFromCodeCoverage]
            int IText.GetLineNumberFromPosition(int position)
            {
                throw new NotImplementedException();
            }

            [ExcludeFromCodeCoverage]
            string IText.GetText()
            {
                throw new NotImplementedException();
            }

            [ExcludeFromCodeCoverage]
            string IText.GetText(TextSpan textSpan)
            {
                return this.GetValue().Text.GetText(textSpan);
            }

            [ExcludeFromCodeCoverage]
            void IText.CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                throw new NotImplementedException();
            }

            [ExcludeFromCodeCoverage]
            void IText.Write(System.IO.TextWriter textWriter)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}