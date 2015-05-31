// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public class ConditionalFactAttribute : FactAttribute
    {
        public ConditionalFactAttribute(Type skipCondition)
        {
            ExecutionCondition condition = (ExecutionCondition)Activator.CreateInstance(skipCondition);
            if (condition.ShouldSkip)
            {
                Skip = condition.SkipReason;
            }
        }
    }

    public abstract class ExecutionCondition
    {
        public abstract bool ShouldSkip { get; }
        public abstract string SkipReason { get; }
    }

    public class x86 : ExecutionCondition
    {
        public override bool ShouldSkip { get { return IntPtr.Size != 4; } }

        public override string SkipReason { get { return "Target platform is not x86"; } }
    }

    public enum ClrOnlyReason
    {
        Unknown,

        // The Mono version of ilasm doesn't have all of the features we need to run 
        // our tests.  In particular it doesn't appear to support the full range of 
        // modopt operators that our tests invoke.
        Ilasm,

        // Mono lists certain methods in a different order than the CLR.  For example
        // Equals, GetHashCode, ToString, etc ... which breaks our tests which hard
        // code the order. 
        MemberOrder, 

        // Can't emit a PDB.
        Pdb,

        // The documentation comment compiler has a dependency on a resource in the 
        // System.Xml assembly.  This is a non-portable / implementation detail 
        // that Mono doesn't mirror.  We need to make this test more robust so it can
        // run on all runtimes. 
        //
        // See DocumentationCommentCompiler.GetDescription 
        DocumentationComment,

        // Can't sign. 
        Signing,
    }

    public class ClrOnlyFact : FactAttribute
    {
        public readonly ClrOnlyReason Reason;

        public ClrOnlyFact(ClrOnlyReason reason = ClrOnlyReason.Unknown)
        {
            Reason = reason;

            if (CLRHelpers.IsRunningOnMono())
            {
                Skip = GetSkipReason(Reason);
            }
        }

        private static string GetSkipReason(ClrOnlyReason reason)
        { 
            switch (reason)
            {
                case ClrOnlyReason.Ilasm:
                    return "Mono ilasm doesn't suupport all of the features we need";
                case ClrOnlyReason.MemberOrder:
                    return "Mono returns certain symbols in different order than we are expecting";
                case ClrOnlyReason.Pdb:
                    return "Can't emit a PDB in this scenario";
                case ClrOnlyReason.Signing:
                    return "Can't sign assemblies in this scenario";
                case ClrOnlyReason.DocumentationComment:
                    return "Documentation comment compiler can't run this test on Mono";
                default:
                    return "Test supported only on CLR";
            }
        }
    }

    public class WindowsOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return Path.DirectorySeparatorChar != '\\'; } }

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
}
