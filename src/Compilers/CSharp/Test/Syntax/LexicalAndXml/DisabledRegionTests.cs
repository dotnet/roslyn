// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DisabledRegionTests : CSharpTestBase
    {
        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledError_DiagnosticsAndEffect()
        {
            var source = @"
#if false
#error ""error1""
#endif
#error ""error2""
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_ErrorDirective, @"""error2""").WithArguments(@"""error2"""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledWarning_DiagnosticsAndEffect()
        {
            var source = @"
#if false
#warning ""warning1""
#endif
#warning ""warning2""
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.WRN_WarningDirective, @"""warning2""").WithArguments(@"""warning2"""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledLine_Diagnostics()
        {
            var source = @"
#if false
#line
#line 0
#endif
#line
#line 0
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, ""),
                Diagnostic(ErrorCode.ERR_InvalidLineNumber, "0"));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledPragma_Diagnostics()
        {
            var source = @"
#if false
#pragma
#pragma warning
#pragma warning disable ""something""
#pragma warning disable 0
#pragma warning disable -1
#pragma checksum
#pragma checksum ""file""
#pragma checksum ""file"" ""guid""
#pragma checksum ""file"" ""guid"" ""bytes""
#endif
#pragma
#pragma warning
#pragma warning disable ""something2""
#pragma warning disable 1
#pragma warning disable -2
#pragma checksum
#pragma checksum ""file""
#pragma checksum ""file"" ""guid""
#pragma checksum ""file"" ""guid"" ""bytes""
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""),
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""),
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "\"something2\""),
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "-"),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, ""),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, ""),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, @"""guid"""),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, ""),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, @"""guid"""),
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, @"""bytes"""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledReference_Diagnostics()
        {
            var source = @"
#if false
#r
#endif
#r
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source, TestOptions.Script,
                Diagnostic(ErrorCode.ERR_ExpectedPPFile, ""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledDefine_Effect()
        {
            var source = @"
#if false
#define foo
#endif
#if foo
#warning ""warning""
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source);
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledDefine_Diagnostics()
        {
            var source = @"
#if false
#define
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledUndef_Effect()
        {
            var source = @"
#define foo
#if false
#undef foo
#endif
#if foo
#warning ""warning""
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.WRN_WarningDirective, @"""warning""").WithArguments(@"""warning"""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledUndef_Diagnostics()
        {
            var source = @"
#if false
#undef
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledRegion_Diagnostics()
        {
            var source = @"
#if false
#region
#endif
class C { }
";

            // CONSIDER: it would be nicer not to double-report this.
            // (It's happening because the #endif doesn't pop the region
            // off the directive stack, so it's still there when we clean
            // up.)
            // NOTE: we deliberately suppress the "missing endif" that
            // dev10 would have reported - we only report the first error
            // when unwinding the stack.
            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_EndRegionDirectiveExpected, "#endif"),
                Diagnostic(ErrorCode.ERR_EndRegionDirectiveExpected, ""));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledEndRegion_Diagnostics()
        {
            var source = @"
#if false
#endregion
#endif
class C { }
";

            // Deliberately refined from ERR_UnexpectedDirective in Dev10.
            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "#endregion"));
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledIf_Effect()
        {
            var source = @"
#if false
#if true
#error error
#endif
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source);
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledIf_Diagnostics()
        {
            var source = @"
#if false
#if true
#endif
class C { }
";

            ParserErrorMessageTests.ParseAndValidate(source,
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, ""));
        }
    }
}
