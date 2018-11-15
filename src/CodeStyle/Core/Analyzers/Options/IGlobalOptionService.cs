// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IGlobalOptionService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(Option<T> option);
    }
}
