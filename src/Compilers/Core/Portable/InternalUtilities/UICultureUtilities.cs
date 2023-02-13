// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Roslyn.Utilities
{
    internal static class UICultureUtilities
    {

        public static Action WithCurrentUICulture(Action action)
        {
            var savedCulture = CultureInfo.CurrentUICulture;
            return () =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    CultureInfo.CurrentUICulture = savedCulture;
                    try
                    {
                        action();
                    }
                    finally
                    {
                        CultureInfo.CurrentUICulture = currentCulture;
                    }
                }
                else
                {
                    action();
                }
            };
        }

        public static Action<T> WithCurrentUICulture<T>(Action<T> action)
        {
            var savedCulture = CultureInfo.CurrentUICulture;
            return param =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    CultureInfo.CurrentUICulture = savedCulture;
                    try
                    {
                        action(param);
                    }
                    finally
                    {
                        CultureInfo.CurrentUICulture = currentCulture;
                    }
                }
                else
                {
                    action(param);
                }
            };
        }

        public static Func<T> WithCurrentUICulture<T>(Func<T> func)
        {
            var savedCulture = CultureInfo.CurrentUICulture;
            return () =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    CultureInfo.CurrentUICulture = savedCulture;
                    try
                    {
                        return func();
                    }
                    finally
                    {
                        CultureInfo.CurrentUICulture = currentCulture;
                    }
                }
                else
                {
                    return func();
                }
            };
        }
    }
}
