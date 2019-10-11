// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingPinnedSolutionInfoWrapper
    {
        internal UnitTestingPinnedSolutionInfoWrapper(PinnedSolutionInfo underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        public UnitTestingPinnedSolutionInfoWrapper(object underlyingObject)
            => UnderlyingObject = (PinnedSolutionInfo)underlyingObject;

        internal PinnedSolutionInfo UnderlyingObject { get; }
    }
}
