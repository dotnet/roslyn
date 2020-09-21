// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal static class UnitTestingWellKnownExperimentNamesAccessor
    {
        [Obsolete("This experiment is no longer used", error: true)]
        public const string RoslynOOP64bit = nameof(RoslynOOP64bit);
    }
}
