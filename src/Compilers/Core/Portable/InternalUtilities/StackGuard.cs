// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    internal static class StackGuard
    {
        public const int MaxUncheckedRecursionDepth = 20;

        public static void EnsureSufficientExecutionStack(int recursionDepth)
        {
            if (recursionDepth > MaxUncheckedRecursionDepth)
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
        }

        // TODO (DevDiv workitem 966425): Replace exception name test with a type test once the type 
        // is available in the PCL
        public static bool IsInsufficientExecutionStackException(Exception ex)
        {
            return ex.GetType().Name == "InsufficientExecutionStackException";
        }
    }
}