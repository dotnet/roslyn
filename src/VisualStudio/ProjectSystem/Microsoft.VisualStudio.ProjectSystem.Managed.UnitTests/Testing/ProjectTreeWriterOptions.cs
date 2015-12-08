// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Testing
{
    [Flags]
    internal enum ProjectTreeWriterOptions 
    {
        None = 0,
        Tags = 1,
        FilePath = 2,
        Capabilities = 4,
        Visibility = 8,
        AllProperties = FilePath | Visibility | Capabilities
    }
}
