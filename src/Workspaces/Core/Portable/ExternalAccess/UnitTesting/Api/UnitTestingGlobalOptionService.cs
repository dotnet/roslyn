// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingGlobalOptionService
    {
        public UnitTestingGlobalOptionService(
            IEnumerable<Lazy<IOptionProvider>> optionProviders,
            IEnumerable<Lazy<IOptionPersister>> optionSerializers)
            => UnderlyingObject = new GlobalOptionService(optionProviders, optionSerializers);

        internal GlobalOptionService UnderlyingObject { get; }
    }
}
