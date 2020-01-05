// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingFatalErrorAccessor
    {
        public static bool ReportWithoutCrash(Exception e)
            => FatalError.ReportWithoutCrash(e);

        public static bool ReportWithoutCrashUnlessCanceled(Exception e)
            => FatalError.ReportWithoutCrashUnlessCanceled(e);
    }
}
