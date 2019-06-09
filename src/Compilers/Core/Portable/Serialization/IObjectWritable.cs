// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    /// <summary>
    /// Objects that implement this interface know how to write their contents to an <see cref="ObjectWriter"/>,
    /// so they can be reconstructed later by an <see cref="ObjectReader"/>.
    /// </summary>
    internal interface IObjectWritable
    {
        void WriteTo(ObjectWriter writer);

        /// <summary>
        /// Returns 'true' when the same instance could be used more than once.
        /// Instances that return 'false' should not be tracked for the purpose 
        /// of de-duplication while serializing/deserializing.
        /// </summary>
        bool ShouldReuseInSerialization { get; }
    }
}
