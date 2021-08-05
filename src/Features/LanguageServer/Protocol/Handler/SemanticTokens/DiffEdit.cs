// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal readonly struct DiffEdit
    {
        public DiffEdit(Type operation, int pos, int? newTextPosition)
        {
            Operation = operation;
            Position = pos;
            NewTextPosition = newTextPosition;
        }

        public Type Operation { get; }

        public int Position { get; }

        public int? NewTextPosition { get; }

        public static DiffEdit Insert(int pos, int newTextPos)
        {
            return new DiffEdit(Type.Insert, pos, newTextPos);
        }

        public static DiffEdit Delete(int pos)
        {
            return new DiffEdit(Type.Delete, pos, newTextPosition: null);
        }

        internal enum Type
        {
            Insert,
            Delete,
        }
    }
}
