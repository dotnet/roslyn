// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<Roslyn.Diagnostics.Analyzers.NumberCommentslRefactoring>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class NumberCommentsRefactoringTests
    {
        [Fact]
        public async Task TestAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]class D { } //
"";
}";
            const string fixedSource = @"
public class C
{
    string s = @""
class D { } // 1
"";
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_WithTriviaAsync()
        {
            const string source = @"
public class C
{
    string s =
[||]/*before*/ @""
class D { } //
"" /*after*/ ;
}";
            const string fixedSource = @"
public class C
{
    string s =
/*before*/ @""
class D { } // 1
"" /*after*/ ;
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_NonNumberCommentsLeftAloneAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]//
class D //
{//
} // test
"";
}";
            const string fixedSource = @"
public class C
{
    string s = @""
//
class D // 1
{//
} // test
"";
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_MultipleCommasAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]class D //1
{ //,
} //
"";
}";
            const string fixedSource = @"
public class C
{
    string s = @""
class D // 1
{ // 2, 3
} // 4
"";
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CSharp_VerifyFix_LastLineAsync()
        {
            const string source = @"
public class C
{
    string s = @""[||]class D { } //"";
}";
            const string fixedSource = @"
public class C
{
    string s = @""class D { } // 1"";
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task CountOverTenAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]class D { } // ,,,,,,,,,,,,
"";
}";
            const string fixedSource = @"
public class C
{
    string s = @""
class D { } // 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
"";
}";
            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyNumberIsImproperAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]class D // 1
{ // 2, 3
} //
"";
}";

            const string fixedSource = @"
public class C
{
    string s = @""
class D // 1
{ // 2, 3
} // 4
"";
}";

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyNumberBeforeCommaIsImproperAsync()
        {
            const string source = @"
public class C
{
    string s = @""
[||]class C // 1
{ // , 3
}
"";
}";

            const string fixedSource = @"
public class C
{
    string s = @""
class C // 1
{ // 2, 3
}
"";
}";

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Fact]
        public async Task EmptyCommentOnEmptyLineIsProperAsync()
        {
            const string source = @"
public class C
{
    string s = @""
// much stuff
//
// more stuff
[||]class C // 1
{ // 2, 3
} //
"";
}";

            const string fixedSource = @"
public class C
{
    string s = @""
// much stuff
//
// more stuff
class C // 1
{ // 2, 3
} // 4
"";
}";

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }
    }
}
