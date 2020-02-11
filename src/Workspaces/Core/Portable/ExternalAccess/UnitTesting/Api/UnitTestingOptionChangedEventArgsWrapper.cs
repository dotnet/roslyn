﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
