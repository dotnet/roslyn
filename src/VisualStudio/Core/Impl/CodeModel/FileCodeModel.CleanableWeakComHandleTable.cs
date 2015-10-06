// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    using CodeElementWeakComAggregateHandle = WeakComHandle<EnvDTE.CodeElement, EnvDTE.CodeElement>;

    public sealed partial class FileCodeModel
    {
        /// <summary>
        /// A wrapper around a collection containing weak references that can clean
        /// itself out of dead weak references in a timesliced way.
        /// The class holds an enumerator over inner collection and a queue of dead elements.
        /// On every 25 element added the cleanup is initiated, which is performed when
        /// CleanupWeakComHandles() method is called (FCM calls it on idle).
        /// The cleanup works in timesliced way.
        /// The cleanup first scans the inner collection adding each element with dead weak
        /// ref to the dead queue. When scan is finished (or new element is added/removed,
        /// which invalidates the enumerator), cleanup processes the dead queue removing
        /// elements from the inner collection.
        /// We keep dead queue alive even when the enumerator got invalidated, removing elements
        /// from the dead queue when they are removed from the collection via external call.
        /// </summary>
        private class CleanableWeakComHandleTable
        {
            // TODO: Move these to options
            private static readonly TimeSpan s_timeSliceForFileCodeModelCleanup = TimeSpan.FromMilliseconds(15);
            private const int ElementsAddedBeforeFileCodeModelCleanup = 25;

            private readonly Dictionary<SyntaxNodeKey, CodeElementWeakComAggregateHandle> _elements;
            private bool _needCleanup;
            private int _elementsAddedSinceLastCleanup;

            // Dead queue is a hash set so we could effectively remove element from
            // the dead queue when it's being removed externally
            private readonly HashSet<SyntaxNodeKey> _deadQueue;
            private IEnumerator<KeyValuePair<SyntaxNodeKey, CodeElementWeakComAggregateHandle>> _cleanupEnumerator;

            private enum State
            {
                Initial,
                Checking,
                ProcessingDeadQueue
            }

            private State _state;

            public CleanableWeakComHandleTable()
            {
                _elements = new Dictionary<SyntaxNodeKey, CodeElementWeakComAggregateHandle>();
                _deadQueue = new HashSet<SyntaxNodeKey>();
                _state = State.Initial;
            }

            private void InvalidateEnumerator()
            {
                if (_cleanupEnumerator != null)
                {
                    _cleanupEnumerator.Dispose();
                    _cleanupEnumerator = null;
                }
            }

            public bool NeedCleanup
            {
                get { return _needCleanup; }
            }

            public void Add(SyntaxNodeKey key, CodeElementWeakComAggregateHandle value)
            {
                TriggerCleanup();
                InvalidateEnumerator();
                _elements.Add(key, value);
            }

            public void Remove(SyntaxNodeKey key)
            {
                InvalidateEnumerator();

                if (_deadQueue.Contains(key))
                {
                    _deadQueue.Remove(key);
                }

                _elements.Remove(key);
            }

            public bool TryGetValue(SyntaxNodeKey key, out CodeElementWeakComAggregateHandle value)
            {
                return _elements.TryGetValue(key, out value);
            }

            public bool ContainsKey(SyntaxNodeKey key)
            {
                return _elements.ContainsKey(key);
            }

            public IEnumerable<CodeElementWeakComAggregateHandle> Values
            {
                get { return _elements.Values; }
            }

            private void TriggerCleanup()
            {
                _elementsAddedSinceLastCleanup++;
                if (_elementsAddedSinceLastCleanup >= ElementsAddedBeforeFileCodeModelCleanup)
                {
                    _needCleanup = true;
                    _elementsAddedSinceLastCleanup = 0;
                }
            }

            private bool CheckWeakComHandles(TimeSlice timeSlice)
            {
                Debug.Assert(_cleanupEnumerator != null);
                Debug.Assert(_state == State.Checking);

                while (_cleanupEnumerator.MoveNext())
                {
                    if (!_cleanupEnumerator.Current.Value.IsAlive())
                    {
                        _deadQueue.Add(_cleanupEnumerator.Current.Key);
                    }

                    if (timeSlice.IsOver)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool ProcessDeadQueue(TimeSlice timeSlice)
            {
                Debug.Assert(_cleanupEnumerator == null, "We should never process dead queue when enumerator is alive.");

                while (_deadQueue.Count > 0)
                {
                    var key = _deadQueue.First();

                    _deadQueue.Remove(key);
                    Debug.Assert(_elements.ContainsKey(key), "How come the key is in the dead queue, but not in the dictionary?");

                    _elements.Remove(key);

                    if (timeSlice.IsOver)
                    {
                        return false;
                    }
                }

                return true;
            }

            public void CleanupWeakComHandles()
            {
                if (!_needCleanup)
                {
                    return;
                }

                var timeSlice = new TimeSlice(s_timeSliceForFileCodeModelCleanup);

                if (_state == State.Initial)
                {
                    _cleanupEnumerator = _elements.GetEnumerator();
                    _state = State.Checking;
                }

                if (_state == State.Checking)
                {
                    if (_cleanupEnumerator == null)
                    {
                        // The enumerator got reset while we were checking, need to process dead queue
                        // before starting checking over again
                        if (!ProcessDeadQueue(timeSlice))
                        {
                            // Need more time to finish processing dead queue, continue next time
                            return;
                        }

                        _cleanupEnumerator = _elements.GetEnumerator();
                    }

                    if (!CheckWeakComHandles(timeSlice))
                    {
                        // Need more time to check for dead elements, continue next time
                        return;
                    }

                    // Done with checking, now process dead queue
                    InvalidateEnumerator();
                    _state = State.ProcessingDeadQueue;
                }

                if (_state == State.ProcessingDeadQueue)
                {
                    if (!ProcessDeadQueue(timeSlice))
                    {
                        // Need more time to finish processing dead queue, continue next time
                        return;
                    }

                    // Done with cleanup
                    _state = State.Initial;
                    _needCleanup = false;
                }
            }
        }
    }
}
