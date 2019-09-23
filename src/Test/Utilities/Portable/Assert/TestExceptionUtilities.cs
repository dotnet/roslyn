// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
