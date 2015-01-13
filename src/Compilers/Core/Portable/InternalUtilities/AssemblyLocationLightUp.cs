// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    internal static class AssemblyLocationLightUp
    {
        private static Lazy<Func<Assembly, string>> lazyLocationGetter = new Lazy<Func<Assembly, string>>(() =>
        {
            try
            {
                var getter = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("get_Location");
                return (Func<Assembly, string>)getter?.CreateDelegate(typeof(Func<Assembly, string>));
            }
            catch
            {
                return null;
            }
        });

        internal static string GetAssemblyLocation(Assembly assembly)
        {
            var getter = lazyLocationGetter.Value;
            if (getter == null)
            {
                throw new PlatformNotSupportedException();
            }

            return getter(assembly);
        }
    }
}
