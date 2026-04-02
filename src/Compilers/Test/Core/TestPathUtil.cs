// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities;

/// <summary>
/// This is a utility class to make it easier to write tests that use paths across 
/// the different operating systems that we support.
/// </summary>
public static class TestPathUtil
{
    public const string WindowsRoot = @"Q:\";
    public const string UnixRoot = @"/q/";
    public static string Root => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsRoot : UnixRoot;

    public static string GetRootedPath(params string[] relativePath) => Path.Combine([Root, .. relativePath]);
}