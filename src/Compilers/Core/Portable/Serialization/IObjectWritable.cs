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
    }
}
