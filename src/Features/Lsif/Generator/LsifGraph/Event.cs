// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents an event. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#events for further details.
    /// </summary>
    internal sealed class Event : Vertex
    {
        public string Kind { get; }
        public string Scope { get; }
        public Id<Element> Data { get; }

        private Event(EventKind kind, string scope, Id<Element> data)
            : base(label: "$event")
        {
            this.Kind = kind switch { EventKind.Begin => "begin", EventKind.End => "end", _ => throw new ArgumentException(nameof(kind)) };
            this.Scope = scope;
            this.Data = data;
        }

        public Event(EventKind kind, Id<Project> data)
            : this(kind, "project", data.As<Project, Element>())
        {
        }

        public Event(EventKind kind, Id<Document> data)
            : this(kind, "document", data.As<Document, Element>())
        {
        }

        public enum EventKind
        {
            Begin,
            End
        }
    }
}
