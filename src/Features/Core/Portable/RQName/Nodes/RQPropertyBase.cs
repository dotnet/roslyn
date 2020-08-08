// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQPropertyBase : RQMethodOrProperty
    {
        public RQPropertyBase(
            RQUnconstructedType containingType,
            RQMethodPropertyOrEventName memberName,
            int typeParameterCount,
            IList<RQParameter> parameters)
            : base(containingType, memberName, typeParameterCount, parameters)
        { }

        protected override string RQKeyword
        {
            get { return RQNameStrings.Prop; }
        }
    }
}
