// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
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

        Fusion,
    }

    public sealed class ClrOnlyFactAttribute : FactAttribute
    {
        public readonly ClrOnlyReason Reason;

        public ClrOnlyFactAttribute(ClrOnlyReason reason = ClrOnlyReason.Unknown)
        {
            Reason = reason;

            if (MonoHelpers.IsRunningOnMono())
            {
                Skip = GetSkipReason(Reason);
            }
        }

        private static string GetSkipReason(ClrOnlyReason reason)
        {
            switch (reason)
            {
                case ClrOnlyReason.Ilasm:
                    return "Mono ilasm doesn't support all of the features we need";
                case ClrOnlyReason.MemberOrder:
                    return "Mono returns certain symbols in different order than we are expecting";
                case ClrOnlyReason.Pdb:
                    return "Can't emit a PDB in this scenario";
                case ClrOnlyReason.Signing:
                    return "Can't sign assemblies in this scenario";
                case ClrOnlyReason.DocumentationComment:
                    return "Documentation comment compiler can't run this test on Mono";
                case ClrOnlyReason.Fusion:
                    return "Fusion not available on Mono";
                default:
                    return "Test supported only on CLR";
            }
        }
    }

    public sealed class MonoOnlyFactAttribute : FactAttribute
    {
        public MonoOnlyFactAttribute(string reason)
        {
            if (!MonoHelpers.IsRunningOnMono())
            {
                Skip = reason;
            }
        }
    }
}
