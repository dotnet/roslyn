﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/TestExtensionsMethods.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    internal static partial class TestExtensionsMethods
    {
        internal static void ValidateDefaultThisBehavior(Action a)
        {
            Assert.Throws<NullReferenceException>(a);
        }
    }
}
