﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal static class ExceptionUtilities
    {
        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> with information about an unexpected value.
        /// </summary>
        /// <param name="o">The unexpected value.</param>
        /// <returns>The <see cref="InvalidOperationException"/>, which should be thrown by the caller.</returns>
        internal static Exception UnexpectedValue(object? o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            Debug.Assert(false, output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static Exception Unreachable
        {
            get { return new InvalidOperationException("This program location is thought to be unreachable."); }
        }
    }
}
