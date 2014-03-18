// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class OrderableMetadata : IOrderableMetadata
    {
        [DefaultValue(new string[] { })]
        public IEnumerable<string> After { get; private set; }

        [DefaultValue(new string[] { })]
        public IEnumerable<string> Before { get; private set; }

        public string Name { get; private set; }

        public OrderableMetadata(IDictionary<string, object> data)
        {
            this.After = (IEnumerable<string>)data.GetValueOrDefault("After") ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Before = (IEnumerable<string>)data.GetValueOrDefault("Before") ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        public OrderableMetadata(string name, IEnumerable<string> after = null, IEnumerable<string> before = null)
        {
            this.After = after ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Before = before ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Name = name;
        }
    }
}