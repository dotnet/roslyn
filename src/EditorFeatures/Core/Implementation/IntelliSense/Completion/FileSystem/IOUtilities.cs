﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
{
    internal static class IOUtilities
    {
        public static T PerformIO<T>(Func<T> function, T defaultValue = default(T))
        {
            try
            {
                return function();
            }
            catch (IOException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            return defaultValue;
        }
    }
}
