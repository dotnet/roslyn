// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal abstract partial class TextDiffer
{
    protected readonly struct DiffEdit
    {
        public DiffEditKind Kind { get; }
        public int Position { get; }
        public int? NewTextPosition { get; }
        public int Length { get; }

        private DiffEdit(DiffEditKind kind, int position, int? newTextPosition, int length)
        {
            Kind = kind;
            Position = position;
            NewTextPosition = newTextPosition;
            Length = length;
        }

        public override string ToString()
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            builder.Append($"{Kind}: Position = {Position}");

            if (NewTextPosition is int newTextPosition)
            {
                builder.Append($", NewTextPosition = {newTextPosition}");
            }

            builder.Append($", Length = {Length}");

            return builder.ToString();
        }

        public void Deconstruct(out DiffEditKind kind, out int position, out int? newTextPosition, out int length)
            => (kind, position, newTextPosition, length) = (Kind, Position, NewTextPosition, Length);

        public static DiffEdit Insert(int position, int newTextPosition, int length = 1)
            => new(DiffEditKind.Insert, position, newTextPosition, length);

        public static DiffEdit Delete(int position, int length = 1)
            => new(DiffEditKind.Delete, position, newTextPosition: null, length);

        public DiffEdit Offset(int positionOffset, int newTextPositionOffset)
            => new(Kind, positionOffset + Position, newTextPositionOffset + NewTextPosition, Length);
    }
}
