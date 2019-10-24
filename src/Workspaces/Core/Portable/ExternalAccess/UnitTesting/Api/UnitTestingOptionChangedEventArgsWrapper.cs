// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingOptionChangedEventArgsWrapper
    {
        internal OptionChangedEventArgs UnderlyingObject { get; }

        public UnitTestingOptionChangedEventArgsWrapper(OptionChangedEventArgs underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));
    }
}
