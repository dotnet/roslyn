// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal struct NullableDirective
    {
        internal int Position { get; }
        internal bool? WarningsState { get; }
        internal bool? AnnotationsState { get; }

        internal NullableDirective(int position, bool? warningsState, bool? annotationsState)
        {
            Position = position;
            WarningsState = warningsState;
            AnnotationsState = annotationsState;
        }
    }
    internal sealed class NullableDirectiveMap
    {
        private static readonly NullableDirectiveMap EmptyGenerated = new NullableDirectiveMap(ImmutableArray<NullableDirective>.Empty, isGeneratedCode: true);

        private static readonly NullableDirectiveMap EmptyNonGenerated = new NullableDirectiveMap(ImmutableArray<NullableDirective>.Empty, isGeneratedCode: false);

        private readonly ImmutableArray<NullableDirective> _directives;

        private readonly bool _isGeneratedCode;

        internal static NullableDirectiveMap Create(SyntaxTree tree, bool isGeneratedCode)
        {
            var directives = GetDirectives(tree);

            var empty = isGeneratedCode ? EmptyGenerated : EmptyNonGenerated;
            return directives.IsEmpty ? empty : new NullableDirectiveMap(directives, isGeneratedCode);
        }

        private NullableDirectiveMap(ImmutableArray<NullableDirective> directives, bool isGeneratedCode)
        {
#if DEBUG
            for (int i = 1; i < directives.Length; i++)
            {
                Debug.Assert(directives[i - 1].Position < directives[i].Position);
            }
#endif
            _directives = directives;
            _isGeneratedCode = isGeneratedCode;
        }

        /// <summary>
        /// Returns true if the `#nullable` directive preceding the position is
        /// `enable` or `safeonly`, false if `disable`, and null if no preceding directive,
        /// or directive preceding the position is `restore`.
        /// </summary>
        internal NullableDirective GetDirectiveState(int position)
        {
            // PositionComparer only checks the position, not the states
            var searchDirective = new NullableDirective(position, default, default);
            int index = _directives.BinarySearch(searchDirective, PositionComparer.Instance);
            if (index < 0)
            {
                // If no exact match, BinarySearch returns the complement
                // of the index of the next higher value.
                index = ~index - 1;
            }

            // Generated files have an initial nullable context that is "disabled"
            var directive = _isGeneratedCode
                ? new NullableDirective(position, false, false)
                : new NullableDirective(position, null, null);

            if (index >= 0)
            {
                Debug.Assert(_directives[index].Position <= position);
                Debug.Assert(index == _directives.Length - 1 || position < _directives[index + 1].Position);
                directive = _directives[index];
            }

            return directive;
        }

        private static ImmutableArray<NullableDirective> GetDirectives(SyntaxTree tree)
        {
            var builder = ArrayBuilder<NullableDirective>.GetInstance();
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
                var setting = nn.SettingToken.Kind() switch
                {
                    SyntaxKind.EnableKeyword => true,
                    SyntaxKind.DisableKeyword => false,
                    SyntaxKind.RestoreKeyword => (bool?)null,
                    var kind => throw ExceptionUtilities.UnexpectedValue(kind)
                };

                var previousDirective = builder.LastOrNullable();

                var directive = nn.TargetToken.Kind() switch
                {
                    SyntaxKind.None => new NullableDirective(position, setting, setting),
                    SyntaxKind.WarningsKeyword => new NullableDirective(position, warningsState: setting, annotationsState: previousDirective?.AnnotationsState),
                    SyntaxKind.AnnotationsKeyword => new NullableDirective(position, warningsState: previousDirective?.WarningsState, annotationsState: setting),
                    var kind => throw ExceptionUtilities.UnexpectedValue(kind)
                };

                builder.Add(directive);
            }
            return builder.ToImmutableAndFree();
        }

        private sealed class PositionComparer : IComparer<NullableDirective>
        {
            internal static readonly PositionComparer Instance = new PositionComparer();

            public int Compare(NullableDirective x, NullableDirective y)
            {
                return x.Position.CompareTo(y.Position);
            }
        }
    }
}
