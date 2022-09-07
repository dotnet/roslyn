﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Serialization type used to pass information to/from OOP and VS.
    /// </summary>
    [DataContract]
    internal readonly struct TodoCommentData : IEquatable<TodoCommentData>
    {
        [DataMember(Order = 0)]
        public readonly int Priority;

        [DataMember(Order = 1)]
        public readonly string Message;

        [DataMember(Order = 2)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 3)]
        public readonly FileLinePositionSpan Span;

        [DataMember(Order = 4)]
        public readonly FileLinePositionSpan MappedSpan;

        public TodoCommentData(
            int priority,
            string message,
            DocumentId documentId,
            FileLinePositionSpan span,
            FileLinePositionSpan mappedSpan)
        {
            Priority = priority;
            Message = message;
            DocumentId = documentId;
            Span = span;
            MappedSpan = mappedSpan;
        }

        public override bool Equals(object? obj)
            => obj is TodoCommentData other && Equals(other);

        public override int GetHashCode()
            => GetHashCode(this);

        public override string ToString()
            => $"{Priority} {Message} {MappedSpan.Path ?? ""} ({MappedSpan.StartLinePosition.Line}, {MappedSpan.StartLinePosition.Character}) [original: {Span.Path ?? ""} ({Span.StartLinePosition.Line}, {Span.StartLinePosition.Character})";

        public bool Equals(TodoCommentData obj)
            => DocumentId == obj.DocumentId &&
               Priority == obj.Priority &&
               Message == obj.Message &&
               Span == obj.Span &&
               MappedSpan == obj.MappedSpan;

        public static int GetHashCode(TodoCommentData item)
            => Hash.Combine(item.DocumentId,
               Hash.Combine(item.Priority,
               Hash.Combine(item.Message,
               Hash.Combine(item.Span.GetHashCode(),
               Hash.Combine(item.MappedSpan.GetHashCode(), 0)))));
    }
}
