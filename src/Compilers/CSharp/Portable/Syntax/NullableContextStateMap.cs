// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// Contains the nullable warnings and annotations context state at a given position in source.
    /// </summary>
    internal readonly struct NullableContextState
    {
        internal int Position { get; }
        internal State WarningsState { get; }
        internal State AnnotationsState { get; }

        internal NullableContextState(int position, State warningsState, State annotationsState)
        {
            Position = position;
            WarningsState = warningsState;
            AnnotationsState = annotationsState;
        }

        internal enum State : byte
        {
            Unknown,
            Disabled,
            Enabled,
            ExplicitlyRestored
        }
    }

    internal readonly struct NullableContextStateMap
    {
        private readonly ImmutableArray<NullableContextState> _contexts;

        internal static NullableContextStateMap Create(SyntaxTree tree)
        {
            var contexts = GetContexts(tree);
            return new NullableContextStateMap(contexts);
        }

        private NullableContextStateMap(ImmutableArray<NullableContextState> contexts)
        {
#if DEBUG
            for (int i = 1; i < contexts.Length; i++)
            {
                Debug.Assert(contexts[i - 1].Position < contexts[i].Position);
            }
#endif
            _contexts = contexts;
        }

        private static NullableContextState GetContextForFileStart()
            => new NullableContextState(
                position: 0,
                warningsState: NullableContextState.State.Unknown,
                annotationsState: NullableContextState.State.Unknown);

        private int GetContextStateIndex(int position)
        {
            // PositionComparer only checks the position, not the states
            var searchContext = new NullableContextState(position, warningsState: NullableContextState.State.Unknown, annotationsState: NullableContextState.State.Unknown);
            int index = _contexts.BinarySearch(searchContext, PositionComparer.Instance);
            if (index < 0)
            {
                // If no exact match, BinarySearch returns the complement
                // of the index of the next higher value.
                index = ~index - 1;
            }

            Debug.Assert(index >= -1);
            Debug.Assert(index < _contexts.Length);
#if DEBUG
            if (index >= 0)
            {
                Debug.Assert(_contexts[index].Position <= position);
                Debug.Assert(index == _contexts.Length - 1 || position < _contexts[index + 1].Position);
            }
#endif
            return index;
        }

        internal NullableContextState GetContextState(int position)
        {
            var index = GetContextStateIndex(position);
            return index < 0 ? GetContextForFileStart() : _contexts[index];
        }

        /// <summary>
        /// Returns whether nullable warnings are enabled within the span.
        /// Returns true if nullable warnings are enabled anywhere in the span;
        /// false if nullable warnings are disabled throughout the span; and
        /// null otherwise.
        /// </summary>
        internal bool? IsNullableAnalysisEnabled(TextSpan span)
        {
            bool hasUnknownOrExplicitlyRestored = false;
            int index = GetContextStateIndex(span.Start);
            var context = index < 0 ? GetContextForFileStart() : _contexts[index];
            Debug.Assert(context.Position <= span.Start);

            while (true)
            {
                switch (context.WarningsState)
                {
                    case NullableContextState.State.Enabled:
                        return true;
                    case NullableContextState.State.Unknown:
                    case NullableContextState.State.ExplicitlyRestored:
                        hasUnknownOrExplicitlyRestored = true;
                        break;
                }
                index++;
                if (index >= _contexts.Length)
                {
                    break;
                }
                context = _contexts[index];
                if (context.Position >= span.End)
                {
                    break;
                }
            }

            return hasUnknownOrExplicitlyRestored ? null : false;
        }

        private static ImmutableArray<NullableContextState> GetContexts(SyntaxTree tree)
        {
            var previousContext = GetContextForFileStart();

            var builder = ArrayBuilder<NullableContextState>.GetInstance();
            foreach (var d in tree.GetRoot().GetDirectives())
            {
                if (d.Kind() != SyntaxKind.NullableDirectiveTrivia)
                {
                    continue;
                }
                var nn = (NullableDirectiveTriviaSyntax)d;
                if (nn.SettingToken.IsMissing || !nn.IsActive)
                {
                    continue;
                }

                var position = nn.EndPosition;
                var setting = (nn.SettingToken.Kind()) switch
                {
                    SyntaxKind.EnableKeyword => NullableContextState.State.Enabled,
                    SyntaxKind.DisableKeyword => NullableContextState.State.Disabled,
                    SyntaxKind.RestoreKeyword => NullableContextState.State.ExplicitlyRestored,
                    var kind => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                var context = nn.TargetToken.Kind() switch
                {
                    SyntaxKind.None => new NullableContextState(position, setting, setting),
                    SyntaxKind.WarningsKeyword => new NullableContextState(position, warningsState: setting, annotationsState: previousContext.AnnotationsState),
                    SyntaxKind.AnnotationsKeyword => new NullableContextState(position, warningsState: previousContext.WarningsState, annotationsState: setting),
                    var kind => throw ExceptionUtilities.UnexpectedValue(kind)
                };

                builder.Add(context);
                previousContext = context;
            }

            return builder.ToImmutableAndFree();
        }

        private sealed class PositionComparer : IComparer<NullableContextState>
        {
            internal static readonly PositionComparer Instance = new PositionComparer();

            public int Compare(NullableContextState x, NullableContextState y)
            {
                return x.Position.CompareTo(y.Position);
            }
        }
    }
}
