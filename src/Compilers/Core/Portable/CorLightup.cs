// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This type contains the light up scenarios for various platform and runtimes.  Any function
    /// in this type can, and is expected to, fail on various platforms.  These are light up scenarios
    /// only.
    /// </summary>
    internal static class CorLightup
    {
        internal static class Desktop
        {
            private static class CultureInfo
            {
                internal static readonly Type Type = typeof(System.Globalization.CultureInfo);

                internal static readonly PropertyInfo CultureTypes = Type
                    .GetTypeInfo()
                    .GetDeclaredProperty(nameof(CultureTypes));
            }

            private static class CultureTypes
            {
                internal const int UserCustomCulture = 8;
            }

            internal static bool? IsUserCustomCulture(System.Globalization.CultureInfo cultureInfo)
            {
                if (CultureInfo.CultureTypes == null)
                {
                    return null;
                }

                try
                {
                    var value = (int)CultureInfo.CultureTypes.GetValue(cultureInfo);
                    return (value & CultureTypes.UserCustomCulture) != 0;
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    return null;
                }
            }
        }
    }
}
