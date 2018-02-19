// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal class RQIndexer : RQPropertyBase
    {
        public RQIndexer(
            RQUnconstructedType containingType,
            RQMethodPropertyOrEventName memberName,
            int typeParameterCount,
            IList<RQParameter> parameters)
            : base(containingType, memberName, typeParameterCount, parameters)
        {
        }
    }
}
