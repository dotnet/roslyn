// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class RecoverableTextAndVersion
{
    /// <summary>
    /// This class holds onto a <see cref="SourceText"/> value weakly, but can save its value and recover it on demand
    /// if needed.  The value is initially strongly held, until the first time that <see cref="GetValue"/> or <see
    /// cref="GetValueAsync"/> is called.  At that point, it will be dumped to secondary storage, and retrieved and
    /// weakly held from that point on in the future.
    /// </summary>
    private sealed partial class RecoverableText
    {
        // enforce saving in a queue so save's don't overload the thread pool.
        private static readonly AsyncBatchingWorkQueue<(RecoverableText recoverableText, SourceText sourceText)> s_saveQueue =
            new(TimeSpan.Zero,
                SaveAllAsync,
                AsynchronousOperationListenerProvider.NullListener,
                CancellationToken.None);

        /// <summary>
        /// Whether or not we've saved our value to secondary storage.  Used so we only do that once.
        /// </summary>
        private bool _saved;

        /// <summary>
        /// Initial strong reference to the SourceText this is initialized with.  Will be used to respond to the first
        /// request to get the value, at which point it will be dumped into secondary storage.
        /// </summary>
        private SourceText? _initialValue;

        /// <summary>
        /// Weak reference to the value last returned from this value source.  Will thus return the same value as long
        /// as something external is holding onto it.
        /// </summary>
        private WeakReference<SourceText>? _weakReference;

        private SemaphoreSlim Gate { get => InterlockedOperations.Initialize(ref field, SemaphoreSlimFactory.Instance); set; }

        /// <summary>
        /// Attempts to get the value, but only through the weak reference.  This will only succeed *after* the value
        /// has been retrieved at least once, and has thus then been save to secondary storage.
        /// </summary>
        private bool TryGetWeakValue([NotNullWhen(true)] out SourceText? value)
        {
            value = null;
            var weakReference = _weakReference;
            return weakReference != null && weakReference.TryGetTarget(out value) && value != null;
        }

        /// <summary>
        /// Attempts to get the value, either through our strong or weak reference.
        /// </summary>
        private bool TryGetStrongOrWeakValue([NotNullWhen(true)] out SourceText? value)
        {
            // See if we still have the constant value stored.  If so, we can trivially return that.
            value = _initialValue;
            if (value != null)
                return true;

            // If not, see if it's something someone else is holding into, and is available through the weak-ref.
            return TryGetWeakValue(out value);
        }

        public bool TryGetValue([MaybeNullWhen(false)] out SourceText value)
            => TryGetStrongOrWeakValue(out value);

        public SourceText GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // if the value is currently being held weakly, then we can return that immediately as we know we will have
            // kicked off the work to save the value to secondary storage.
            if (TryGetWeakValue(out var instance))
                return instance;

            // Otherwise, we're either holding the value strongly, or we need to recover it from secondary storage.
            using (Gate.DisposableWait(cancellationToken))
            {
                if (!TryGetStrongOrWeakValue(out instance))
                    instance = Recover(cancellationToken);

                // If the value was strongly held, kick off the work to write it to secondary storage and release the
                // strong reference to it.
                UpdateWeakReferenceAndEnqueueSaveTask_NoLock(instance);
                return instance;
            }
        }

        public async Task<SourceText> GetValueAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // if the value is currently being held weakly, then we can return that immediately as we know we will have
            // kicked off the work to save the value to secondary storage.
            if (TryGetWeakValue(out var instance))
                return instance;

            using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!TryGetStrongOrWeakValue(out instance))
                    instance = await RecoverAsync(cancellationToken).ConfigureAwait(false);

                // If the value was strongly held, kick off the work to write it to secondary storage and release the
                // strong reference to it.
                UpdateWeakReferenceAndEnqueueSaveTask_NoLock(instance);
                return instance;
            }
        }

        /// <summary>
        /// Kicks off the work to save this instance to secondary storage at some point in the future.  Once that save
        /// occurs successfully, we will drop our cached data and return values from that storage instead.
        /// </summary>
        private void UpdateWeakReferenceAndEnqueueSaveTask_NoLock(SourceText instance)
        {
            Contract.ThrowIfTrue(Gate.CurrentCount != 0);

            _weakReference ??= new WeakReference<SourceText>(instance);
            _weakReference.SetTarget(instance);

            // Ensure we only save once.
            if (!_saved)
            {
                _saved = true;

                s_saveQueue.AddWork((this, instance));
            }
        }

        private static async ValueTask SaveAllAsync(
            ImmutableSegmentedList<(RecoverableText recoverableText, SourceText sourceText)> list, CancellationToken cancellationToken)
        {
            foreach (var (recoverableText, sourceText) in list)
                await recoverableText.SaveAsync(sourceText, cancellationToken).ConfigureAwait(false);
        }
    }
}
