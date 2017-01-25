// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Objects that implement this interface know how to provide a way to read and construct instances of the same type 
    /// from an <see cref="ObjectReader"/>.
    /// </summary>
    /// <remarks>
    /// This is typically used by a <see cref="ObjectBinder"/> that records how to 
    /// read back objects that were previously written using the same <see cref="ObjectBinder"/> instance, as a way to avoid needing 
    /// to describe all deserialization readers up front, and/or avoid using reflection to discover them.
    /// </remarks>
    internal interface IObjectReadable
    {
        Func<ObjectReader, object> GetReader();
    }
}
