// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.IO;

namespace Microsoft.DotNet.FileBasedPrograms;

/// <summary>
/// When targeting netstandard2.0, the user of the source package must "implement" certain methods by declaring members in this type.
/// </summary>
internal partial class ExternalHelpers
{
    public static partial int CombineHashCodes(int value1, int value2);
    public static partial string GetRelativePath(string relativeTo, string path);

    public static partial bool IsPathFullyQualified(string path);

#if NET
    public static partial int CombineHashCodes(int value1, int value2)
        => HashCode.Combine(value1, value2);

    public static partial string GetRelativePath(string relativeTo, string path)
        => Path.GetRelativePath(relativeTo, path);

    public static partial bool IsPathFullyQualified(string path)
        => Path.IsPathFullyQualified(path);

#elif FILE_BASED_PROGRAMS_SOURCE_PACKAGE_BUILD
    // This path should only be used when we are verifying that the source package itself builds under netstandard2.0.
    public static partial int CombineHashCodes(int value1, int value2)
        => throw new NotImplementedException();

    public static partial string GetRelativePath(string relativeTo, string path)
        => throw new NotImplementedException();

    public static partial bool IsPathFullyQualified(string path)
        => throw new NotImplementedException();

#endif
}

// https://github.com/dotnet/sdk/issues/51487: Remove usage of GracefulException from the source package
#if FILE_BASED_PROGRAMS_SOURCE_PACKAGE_GRACEFUL_EXCEPTION
internal class GracefulException : Exception
{
    public GracefulException()
    {
    }

    public GracefulException(string? message) : base(message)
    {
    }

    public GracefulException(string format, string arg) : this(string.Format(format, arg))
    {
    }

    public GracefulException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
#endif
