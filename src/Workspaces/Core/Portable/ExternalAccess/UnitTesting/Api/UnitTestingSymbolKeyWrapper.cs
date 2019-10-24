// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal struct UnitTestingSymbolKeyWrapper
    {
        internal SymbolKey UnderlyingObject { get; }

        public UnitTestingSymbolKeyWrapper(SymbolKey underlyingObject)
            => UnderlyingObject = underlyingObject;

        public override string ToString()
            => UnderlyingObject.ToString();
    }
}
