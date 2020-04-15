﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQMethod : RQMethodBase
    {
        public RQMethod(
            RQUnconstructedType containingType,
            RQMethodPropertyOrEventName memberName,
            int typeParameterCount,
            IList<RQParameter> parameters)
            : base(containingType, memberName, typeParameterCount, parameters)
        {
        }
    }
}
