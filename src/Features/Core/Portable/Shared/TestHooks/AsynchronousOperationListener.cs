// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener : IAsynchronousOperationListener, IAsynchronousOperationWaiter
    {
        /// <summary>
        /// Stores the source information for an <see cref="IAsyncToken"/> value.  Helpful when 
        /// tracking down tokens which aren't properly disposed.
        /// </summary>
        private struct TokenSourceInfo
        {
            internal IAsyncToken Token { get; set; }
            internal string FilePath { get; set; }
            internal int LineNumber { get; set; }

            public override string ToString() => $"{Path.GetFileName(FilePath)} {LineNumber}";
        }

        private readonly object _gate = new object();
        private readonly List<TokenSourceInfo> _tokenList = new List<TokenSourceInfo>();
        private readonly HashSet<TaskCompletionSource<bool>> _pendingTasks = new HashSet<TaskCompletionSource<bool>>();

        private int _counter;
        private bool _trackActiveTokens;
        private HashSet<DiagnosticAsyncToken> _activeDiagnosticTokens = new HashSet<DiagnosticAsyncToken>();

        public AsynchronousOperationListener()
        {
            _trackActiveTokens = Debugger.IsAttached;
        }

        public IAsyncToken BeginAsyncOperation(string name, object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            lock (_gate)
            {
                IAsyncToken asyncToken;
                if (_trackActiveTokens)
                {
                    var token = new DiagnosticAsyncToken(this, name, tag);
                    _activeDiagnosticTokens.Add(token);
                    asyncToken = token;
                }
                else
                {
                    asyncToken = new AsyncToken(this);
                }

                _tokenList.Add(new TokenSourceInfo()
                {
                    Token = asyncToken,
                    FilePath = filePath,
                    LineNumber = lineNumber
                });

                return asyncToken;
            }
        }

        private void Increment()
        {
            lock (_gate)
            {
                _counter++;
            }
        }

        private void Decrement(AsyncToken token)
        {
            lock (_gate)
            {
                _counter--;
                if (_counter == 0)
                {
                    foreach (var task in _pendingTasks)
                    {
                        task.SetResult(true);
                    }

                    _pendingTasks.Clear();
                }

                int i = 0;
                bool removed = false;
                while (i < _tokenList.Count)
                {
                    if (_tokenList[i].Token == token)
                    {
                        _tokenList.RemoveAt(i);
                        removed = true;
                        break;
                    }

                    i++;
                }

                if (!removed)
                {
                    Debug.Assert(false, "Hit the error");
                    throw new InvalidOperationException();
                }

                if (_trackActiveTokens)
                {
                    var diagnosticAsyncToken = token as DiagnosticAsyncToken;

                    if (diagnosticAsyncToken != null)
                    {
                        _activeDiagnosticTokens.Remove(diagnosticAsyncToken);
                    }
                }
            }
        }

        public virtual Task CreateWaitTask()
        {
            lock (_gate)
            {
                var source = new TaskCompletionSource<bool>();
                if (_counter == 0)
                {
                    // There is nothing to wait for, so we are immediately done
                    source.SetResult(true);
                }
                else
                {
                    _pendingTasks.Add(source);
                }

                return source.Task;
            }
        }

        public bool TrackActiveTokens
        {
            get
            {
                return _trackActiveTokens;
            }

            set
            {
                lock (_gate)
                {
                    if (_trackActiveTokens == value)
                    {
                        return;
                    }

                    _trackActiveTokens = value;

                    if (_trackActiveTokens)
                    {
                        _activeDiagnosticTokens = new HashSet<DiagnosticAsyncToken>();
                    }
                    else
                    {
                        _activeDiagnosticTokens = null;
                    }
                }
            }
        }

        public bool HasPendingWork
        {
            get
            {
                return _counter != 0;
            }
        }

        public ImmutableArray<DiagnosticAsyncToken> ActiveDiagnosticTokens
        {
            get
            {
                lock (_gate)
                {
                    if (_activeDiagnosticTokens == null)
                    {
                        return ImmutableArray<DiagnosticAsyncToken>.Empty;
                    }

                    return _activeDiagnosticTokens.ToImmutableArray();
                }
            }
        }

        internal static IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> CreateListeners(
            string featureName, IAsynchronousOperationListener listener)
        {
            return CreateListeners(ValueTuple.Create(featureName, listener));
        }

        internal static IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> CreateListeners<T>(
            params ValueTuple<string, T>[] pairs) where T : IAsynchronousOperationListener
        {
            return pairs.Select(CreateLazy).ToList();
        }

        private static Lazy<IAsynchronousOperationListener, FeatureMetadata> CreateLazy<T>(
            ValueTuple<string, T> tuple) where T : IAsynchronousOperationListener
        {
            return new Lazy<IAsynchronousOperationListener, FeatureMetadata>(
                () => tuple.Item2, new FeatureMetadata(new Dictionary<string, object>() { { "FeatureName", tuple.Item1 } }));
        }
    }
}