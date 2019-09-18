// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingRemoteHostOptionsWrapper
    {
        public static Option<bool> OOP64Bit = RemoteHostOptions.OOP64Bit;
    }
}
