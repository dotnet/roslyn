// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Xunit;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public class WindowsOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return Path.DirectorySeparatorChar != '\\'; } }

        public override string SkipReason { get { return "Test not supported on Mono"; } }
    }

    public class UnixLikeOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return !PathUtilities.IsUnixLikePlatform; } }

        public override string SkipReason { get { return "Test not supported on Windows"; } }
    }

    public class NotMonoOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return MonoHelpers.IsRunningOnMono(); } }

        public override string SkipReason { get { return "Test not supported on Mono"; } }
    }

    public class Framework35Installed : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                try
                {
                    const string RegistryPath = @"Software\Microsoft\NET Framework Setup\NDP\v3.5";
                    var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                    if (key == null)
                    {
                        return true;
                    }

                    var value = Convert.ToInt32(key.GetValue("Install", 0) ?? 0);
                    return value == 0;
                }
                catch
                {
                    return true;
                }
            }
        }

        public override string SkipReason
        {
            get
            {
                return ".NET Framework 3.5 is not installed";
            }
        }
    }

    public class NotFramework45 : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                // On Framework 4.5, ExtensionAttribute lives in mscorlib...
                return typeof(System.Runtime.CompilerServices.ExtensionAttribute).Assembly ==
                    typeof(object).Assembly;
            }
        }

        public override string SkipReason { get { return "Test currently not supported on Framework 4.5"; } }
    }

    public class OSVersionWin8 : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                return !OSVersion.IsWin8;
            }
        }

        public override string SkipReason
        {
            get
            {
                return "Window Version is not Win8 (build:9200)";
            }
        }
    }

    public sealed class OSVersion
    {
        public static readonly bool IsWin8 = System.Environment.OSVersion.Version.Build >= 9200;
    }

    public class HasValidFonts : ExecutionCondition
    {
        public override bool ShouldSkip
        {
            get
            {
                try
                {
                    var unused = SystemFonts.DefaultFont;
                    return unused == null;
                }
                catch
                {
                    // Motivating issue: https://github.com/dotnet/roslyn/issues/11278
                    //
                    // On a small percentage of Jenkins runs we see that fonts are corrupted on the machine.  That causes any
                    // font based test to fail with no reasonable way to recover (other than manually re-installing fonts). 
                    //
                    // This at least gives us a mechanism for skipping those tests when fonts are in a bad state.
                    return true;
                }
            }
        }

        public override string SkipReason => "Fonts are unavailable";
    }
}
