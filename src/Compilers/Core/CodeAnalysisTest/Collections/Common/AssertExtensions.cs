// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public static class AssertExtensions
    {
        private static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        public static T Throws<T>(string netCoreParamName, string netFxParamName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            if (netFxParamName == null && IsNetFramework)
            {
                // Param name varies between .NET Framework versions -- skip checking it
                return exception;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string expectedParamName =
                IsNetFramework ?
                netFxParamName : netCoreParamName;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        public static T Throws<T>(string expectedParamName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            Assert.Equal(expectedParamName, exception.ParamName);

            return exception;
        }
    }
}
