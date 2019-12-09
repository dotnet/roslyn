// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class OrderableMetadata
    {
        [DefaultValue(new string[] { })]
        public object? After { get; }

        [DefaultValue(new string[] { })]
        public object? Before { get; }

        internal IEnumerable<string> AfterTyped { get; set; }
        internal IEnumerable<string> BeforeTyped { get; set; }

        public string? Name { get; }

        public OrderableMetadata(IDictionary<string, object> data)
        {
            var readOnlyData = (IReadOnlyDictionary<string, object>)data;
            this.AfterTyped = readOnlyData.GetEnumerableMetadata<string>("After").WhereNotNull();
            this.BeforeTyped = readOnlyData.GetEnumerableMetadata<string>("Before").WhereNotNull();
            this.Name = (string?)data.GetValueOrDefault("Name");
        }

        public OrderableMetadata(string? name, IEnumerable<string>? after = null, IEnumerable<string>? before = null)
        {
            this.AfterTyped = after ?? SpecializedCollections.EmptyEnumerable<string>();
            this.BeforeTyped = before ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Name = name;
        }
    }
}
