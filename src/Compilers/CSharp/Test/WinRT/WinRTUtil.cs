// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
