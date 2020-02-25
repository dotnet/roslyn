// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ExceptionUtilities
    {
        public static Exception Unreachable => new InvalidOperationException("This program location is thought to be unreachable.");
    }
}
