// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Represents a fixed array of objects that do not need to be serialized because they are well known on both ends.
    /// </summary>
    /// <remarks>
    /// This class is just a sneaky way to get a reference to an ImmutableArray, 
    /// due to <see cref="StreamObjectWriter"/> needing to be able to use it as a key for a ConditionalWeakTable.
    /// </remarks>
    internal class ObjectData
    {
        public ImmutableArray<object> Objects { get; }

        public ObjectData(ImmutableArray<object> objects)
        {
            this.Objects = objects.NullToEmpty();
        }
    }
}