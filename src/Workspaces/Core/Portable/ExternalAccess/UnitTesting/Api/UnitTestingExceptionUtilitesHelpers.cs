// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingExceptionUtilitesHelpers
    {
        public static Exception Unreachable => ExceptionUtilities.Unreachable;
    }
}
