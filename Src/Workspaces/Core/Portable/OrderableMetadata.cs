// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class OrderableMetadata : IOrderableMetadata
    {
        [DefaultValue(new string[] { })]
        public object After { get; private set; }

        [DefaultValue(new string[] { })]
        public object Before { get; private set; }

        internal IEnumerable<string> AfterTyped { get; set; }
        internal IEnumerable<string> BeforeTyped { get; set; }

        public string Name { get; private set; }

        IEnumerable<string> IOrderableMetadata.After
        {
            get
            {
                return AfterTyped;
            }
        }

        IEnumerable<string> IOrderableMetadata.Before
        {
            get
            {
                return BeforeTyped;
            }
        }

        public OrderableMetadata(IDictionary<string, object> data)
        {
            this.AfterTyped = GetEnumerableMetadata<string>(data, "After");
            this.BeforeTyped = GetEnumerableMetadata<string>(data, "Before");
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        protected static IEnumerable<T> GetEnumerableMetadata<T>(IDictionary<string, object> data, string name)
        {
            var metadata = data.GetValueOrDefault(name);

            return metadata.TypeSwitch((IEnumerable<T> enumerable) => enumerable,
                                (T s) => SpecializedCollections.SingletonEnumerable(s),
                                _ => SpecializedCollections.EmptyEnumerable<T>());
        }

        public OrderableMetadata(string name, IEnumerable<string> after = null, IEnumerable<string> before = null)
        {
            this.AfterTyped = after ?? SpecializedCollections.EmptyEnumerable<string>();
            this.BeforeTyped = before ?? SpecializedCollections.EmptyEnumerable<string>();
            this.Name = name;
        }
    }
}