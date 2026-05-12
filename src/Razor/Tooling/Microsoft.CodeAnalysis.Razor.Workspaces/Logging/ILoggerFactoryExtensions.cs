// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal static class ILoggerFactoryExtensions
{
    public static ILogger GetOrCreateLogger<T>(this ILoggerFactory factory)
    {
        return factory.GetOrCreateLogger(typeof(T));
    }

    public static ILogger GetOrCreateLogger(this ILoggerFactory factory, Type type)
    {
        return factory.GetOrCreateLogger(TrimTypeName(type.FullName.AssumeNotNull()));
    }

    private static string TrimTypeName(string name)
    {
        if (TryTrim(name, "Microsoft.VisualStudio.", out var trimmedName) ||
            TryTrim(name, "Microsoft.AspNetCore.Razor.", out trimmedName) ||
            TryTrim(name, "Microsoft.CodeAnalysis.Razor.", out trimmedName) ||
            TryTrim(name, "Microsoft.VisualStudioCode.RazorExtension.", out trimmedName))
        {
            return trimmedName;
        }

        return name;

        static bool TryTrim(string name, string prefix, out string trimmedName)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                trimmedName = name.Substring(prefix.Length);
                return true;
            }

            trimmedName = name;

            return false;
        }
    }
}
