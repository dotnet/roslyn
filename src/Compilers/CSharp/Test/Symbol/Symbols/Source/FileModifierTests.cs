// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FileModifierTests : CSharpTestBase
    {
        [Fact]
        public void LangVersion()
        {
            var source = """
                file class C { }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,12): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // file class C { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("file types").WithLocation(1, 12));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Nested()
        {
            var source = """
                class Outer
                {
                    file class C { }
                }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,16): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     file class C { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("file types").WithLocation(3, 16));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }
    }
}
