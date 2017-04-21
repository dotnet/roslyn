// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal interface IMefHostExportProvider
    {
        IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>();
        IEnumerable<Lazy<TExtension>> GetExports<TExtension>();
    }
}
