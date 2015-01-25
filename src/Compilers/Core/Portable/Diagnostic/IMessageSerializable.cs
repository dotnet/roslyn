// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Indicates that the implementing type can be serialized via <see cref="object.ToString"/> 
    /// for diagnostic message purposes.
    /// </summary>
    /// <remarks>
    /// Not appropriate on types that require localization, since localization should
    /// happen after serialization.
    /// </remarks>
    internal interface IMessageSerializable
    {
    }
}
