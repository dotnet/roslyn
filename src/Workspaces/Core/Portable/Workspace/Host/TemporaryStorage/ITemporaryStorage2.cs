// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// TemporaryStorage can be used to read and write text to a temporary storage location.
    /// </summary>
    internal interface ITemporaryTextStorage2 : ITemporaryTextStorage
    {
        string Name { get; }
        long Size { get; }
    }

    internal interface ITemporaryStreamStorage2 : ITemporaryStreamStorage
    {
        string Name { get; }
        long Size { get; }
    }
}
