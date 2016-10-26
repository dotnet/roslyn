// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal class ObjectData
    {
        public ImmutableArray<object> Objects { get; }

        public ObjectData(ImmutableArray<object> objects)
        {
            this.Objects = objects.NullToEmpty();
        }
    }
}