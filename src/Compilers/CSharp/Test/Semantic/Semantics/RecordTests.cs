// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RecordTests : CompilingTestBase
    {
        [Fact]
        public void RecordLanguageVersion()
        {
            var src1 = @"
class Point(int x, int y);
";
            var src2 = @"
data class Point;
";
            var src3 = @"
data class Point(int x, int y);
";
            var comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (2,12): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("records").WithLocation(2, 12)
            );
            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("records").WithLocation(2, 1)
            );
            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("records").WithLocation(2, 1),
                // (2,17): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("records").WithLocation(2, 17)
            );
        }
    }
}