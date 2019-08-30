// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal struct UnitTestingInvovationReasonsWrapper
    {
        internal UnitTestingInvovationReasonsWrapper(InvocationReasons underLyingObject)
            => UnderlyingObject = underLyingObject;

        internal InvocationReasons UnderlyingObject { get; }

        public bool Contains(string reason)
            => UnderlyingObject.Contains(reason);
    }
}
