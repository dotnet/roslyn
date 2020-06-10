// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal static class UnitTestingExceptionUtilitesHelpers
    {
        public static Exception Unreachable => ExceptionUtilities.Unreachable;
    }
}
