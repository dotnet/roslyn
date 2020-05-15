﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingFatalErrorAccessor
    {
        public static bool ReportWithoutCrash(this Exception e)
            => FatalError.ReportWithoutCrash(e);

        public static bool ReportWithoutCrashUnlessCanceled(this Exception e)
            => FatalError.ReportWithoutCrashUnlessCanceled(e);
    }
}
