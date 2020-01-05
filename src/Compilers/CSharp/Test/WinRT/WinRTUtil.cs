// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal static class WinRTUtil
    {
        internal static CompilationVerifier CompileAndVerifyOnWin8Only(
            this CSharpTestBase testBase,
            string source,
            MetadataReference[] additionalRefs = null,
            string expectedOutput = null)
        {
            var isWin8 = OSVersion.IsWin8;
            return testBase.CompileAndVerifyWithWinRt(
                source,
                references: additionalRefs,
                expectedOutput: isWin8 ? expectedOutput : null,
                verify: isWin8 ? Verification.Passes : Verification.Fails);
        }

    }
}
