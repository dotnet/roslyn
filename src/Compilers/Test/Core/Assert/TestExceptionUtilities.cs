// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class TestExceptionUtilities
    {
        public static InvalidOperationException UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            System.Diagnostics.Debug.Fail(output);
            return new InvalidOperationException(output);
        }
    }
}
