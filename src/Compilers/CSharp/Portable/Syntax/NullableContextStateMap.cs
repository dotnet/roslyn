// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// Contains the nullable warnings and annotations context state at a given position in source,
    /// where <see langword="true"/> means the context is 'enable', <see langword="false"/> means the context is 'disable',
    /// and <see langword="null"/> means the context is 'restore' or not specified.
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
        private static readonly NullableContextStateMap Empty = new NullableContextStateMap(ImmutableArray<NullableContextState>.Empty);

        private readonly ImmutableArray<NullableContextState> _contexts;

        internal static NullableContextStateMap Create(SyntaxTree tree)
        {
            var contexts = GetContexts(tree);

            return contexts.IsEmpty ? Empty : new NullableContextStateMap(contexts);
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

        private static NullableContextState GetContextForFileStart(int position)
            => new NullableContextState(
                position,
                warningsState: NullableContextState.State.Unknown,
                annotationsState: NullableContextState.State.Unknown);

        internal NullableContextState GetContextState(int position)
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

            var context = GetContextForFileStart(position);

            if (index >= 0)
            {
                Debug.Assert(_contexts[index].Position <= position);
                Debug.Assert(index == _contexts.Length - 1 || position < _contexts[index + 1].Position);
                context = _contexts[index];
            }

            return context;
        }

        /// <summary>
        /// Returns true if any of the NullableContexts in this map enable annotations, warnings, or both.
        /// This does not include any restore directives.
        /// </summary>
        internal bool HasNullableEnables()
        {
            foreach (var context in _contexts)
            {
                if (context.AnnotationsState == NullableContextState.State.Enabled || context.WarningsState == NullableContextState.State.Enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private static ImmutableArray<NullableContextState> GetContexts(SyntaxTree tree)
        {
            var previousContext = GetContextForFileStart(position: 0);

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
