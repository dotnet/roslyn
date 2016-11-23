// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal interface IObjectReadable
    {
        Func<ObjectReader, object> GetReader();
    }
}
