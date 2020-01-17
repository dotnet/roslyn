// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal bool? WarningsState { get; }
        internal bool? AnnotationsState { get; }
        internal bool AnnotationsExplicitlyRestored { get; }
        internal bool WarningsExplicitlyRestored { get; }

        internal NullableContextState(int position, bool? warningsState, bool? annotationsState, bool warningsExplicitlyRestored, bool annotationsExplicitlyRestored)
        {
            Position = position;
            WarningsState = warningsState;
            AnnotationsState = annotationsState;
            WarningsExplicitlyRestored = warningsExplicitlyRestored;
            AnnotationsExplicitlyRestored = annotationsExplicitlyRestored;
        }
    }
    internal sealed class NullableContextStateMap
    {
        private static readonly NullableContextStateMap EmptyGenerated = new NullableContextStateMap(ImmutableArray<NullableContextState>.Empty, isGeneratedCode: true);

        private static readonly NullableContextStateMap EmptyNonGenerated = new NullableContextStateMap(ImmutableArray<NullableContextState>.Empty, isGeneratedCode: false);

        private readonly ImmutableArray<NullableContextState> _contexts;

        private readonly bool _isGeneratedCode;

        internal static NullableContextStateMap Create(SyntaxTree tree, bool isGeneratedCode)
        {
            var contexts = GetContexts(tree, isGeneratedCode);

            var empty = isGeneratedCode ? EmptyGenerated : EmptyNonGenerated;
            return contexts.IsEmpty ? empty : new NullableContextStateMap(contexts, isGeneratedCode);
        }

        private NullableContextStateMap(ImmutableArray<NullableContextState> contexts, bool isGeneratedCode)
        {
#if DEBUG
            for (int i = 1; i < contexts.Length; i++)
            {
                Debug.Assert(contexts[i - 1].Position < contexts[i].Position);
            }
#endif
            _contexts = contexts;
            _isGeneratedCode = isGeneratedCode;
        }

        private static NullableContextState GetContextForFileStart(int position, bool isGeneratedCode)
        {
            // Generated files have an initial nullable context that is "disabled"
            return isGeneratedCode
                ? new NullableContextState(position, warningsState: false, annotationsState: false, warningsExplicitlyRestored: false, annotationsExplicitlyRestored: false)
                : new NullableContextState(position, warningsState: null, annotationsState: null, warningsExplicitlyRestored: false, annotationsExplicitlyRestored: false);
        }

        internal NullableContextState GetContextState(int position)
        {
            // PositionComparer only checks the position, not the states
            var searchContext = new NullableContextState(position, default, default, warningsExplicitlyRestored: false, annotationsExplicitlyRestored: false);
            int index = _contexts.BinarySearch(searchContext, PositionComparer.Instance);
            if (index < 0)
            {
                // If no exact match, BinarySearch returns the complement
                // of the index of the next higher value.
                index = ~index - 1;
            }

            var context = GetContextForFileStart(position, _isGeneratedCode);

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
                if (context.AnnotationsState == true || context.WarningsState == true)
                {
                    return true;
                }
            }

            return false;
        }

        private static ImmutableArray<NullableContextState> GetContexts(SyntaxTree tree, bool isGeneratedCode)
        {
            var previousContext = GetContextForFileStart(position: 0, isGeneratedCode: isGeneratedCode);

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

                var position = nn.Location.SourceSpan.End;

                bool? setting;
                bool wasExplicitlyRestored = false;
                switch (nn.SettingToken.Kind())
                {
                    case SyntaxKind.EnableKeyword:
                        setting = true;
                        break;
                    case SyntaxKind.DisableKeyword:
                        setting = false;
                        break;
                    case SyntaxKind.RestoreKeyword:
                        setting = null;
                        wasExplicitlyRestored = true;
                        break;
                    case var kind:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }

                var context = nn.TargetToken.Kind() switch
                {
                    SyntaxKind.None => new NullableContextState(position, setting, setting, wasExplicitlyRestored, wasExplicitlyRestored),
                    SyntaxKind.WarningsKeyword => new NullableContextState(position,
                                                                           warningsState: setting,
                                                                           annotationsState: previousContext.AnnotationsState,
                                                                           warningsExplicitlyRestored: wasExplicitlyRestored,
                                                                           annotationsExplicitlyRestored: previousContext.AnnotationsExplicitlyRestored),
                    SyntaxKind.AnnotationsKeyword => new NullableContextState(position,
                                                                              warningsState: previousContext.WarningsState,
                                                                              annotationsState: setting,
                                                                              warningsExplicitlyRestored: previousContext.WarningsExplicitlyRestored,
                                                                              annotationsExplicitlyRestored: wasExplicitlyRestored),
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
