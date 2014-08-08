#if false
// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.Providers;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Roslyn.UnitTestFramework;
using Xunit;

namespace MakeConstCS.UnitTests
{
    public class MakeConstTests : SyntaxNodeCodeIssueProviderTestFixture
    {
        [Fact]
        public void SimpleCase()
        {
            Test("int i = 0;", "const int i = 0;");
        }

        [Fact]
        public void NoIssueForExistingConst()
        {
            TestMissing("const int i = 0;");
        }

        [Fact]
        public void NoIssueForVariableThatIsWrittem()
        {
            var code = @"
int i = 0;
i = 1;";

            TestMissing(code);
        }

        [Fact]
        public void IssueForConstAssignedToVariable()
        {
            var code = @"
class C
{
    void M()
    {
        const int i = 0;
        int j = i;
    }
}";

            var expected = @"
class C
{
    void M()
    {
        const int i = 0;
        const int j = i;
    }
}";

            Test(code, expected,
                root => root.Members[0].Members[0].Body.Statements[1]);
        }

        [Fact]
        public void HandleVar()
        {
            var code = @"
class C
{
    void M()
    {
        const byte a = 0;
        const byte b = 1;
        var c = a + b;
    }
}";

            var expected = @"
class C
{
    void M()
    {
        const byte a = 0;
        const byte b = 1;
        const int c = a + b;
    }
}";

            Test(code, expected,
                root => root.Members[0].Members[0].Body.Statements[2]);
        }

        [Fact]
        public void NoIssueForStringLiteralAssignedToObject1()
        {
            TestMissing("object x = \"abc\";");
        }

        [Fact]
        public void NoIssueForStringLiteralAssignedToObject2()
        {
            TestMissing("object x = (object)\"abc\";");
        }

        [Fact]
        public void IssueForStringLiteralAssignedToString()
        {
            Test("string x = \"abc\";", "const string x = \"abc\";");
        }

        [Fact]
        public void NoIssueForNumericLiteralAssignedToObject()
        {
            TestMissing("object x = 1;");
        }

        [Fact]
        public void IssueForNumericLiteralAssignedToInt()
        {
            Test("int x = 1;", "const int x = 1;");
        }

        [Fact]
        public void IssueForVarAlias()
        {
            var code = @"
using var = System.String;
class C
{
    void M()
    {
        var s = ""abc"";
    }
}";

            var expected = @"
using var = System.String;
class C
{
    void M()
    {
        const var s = ""abc"";
    }
}";

            Test(code, expected,
                root => root.Members[0].Members[0].Body.Statements[0]);
        }

        [Fact]
        public void NoIssueForStringLiteralAssignedToInt32()
        {
            TestMissing("int x = \"abc\";");
        }

        [Fact]
        public void NoIssueForNumericLiteralAssignedToIComparable()
        {
            TestMissing("System.IComparable x = 3;");
        }

        [Fact]
        public void IssueForNullAssignedToIComparable()
        {
            Test("System.IComparable x = null;", "const System.IComparable x = null;");
        }

        [Fact]
        public void NoIssueForNumericLiteralWithUserDefinedConversion1()
        {
            var code = @"
class C
{
    void M()
    {
        C c = 3;
    }

    static implicit operator C(int n)
    {
        return null;
    }
}";

            TestMissing(code,
                root => root.Members[0].Members[0].Body.Statements[0]);
        }

        [Fact]
        public void NoIssueForNumericLiteralWithUserDefinedConversion2()
        {
            var code = @"
struct S
{
    void M()
    {
        S S = 3;
    }

    static implicit operator S(int n)
    {
        return null;
    }
}";

            TestMissing(code,
                root => root.Members[0].Members[0].Body.Statements[0]);
        }

        [Fact]
        public void NoIssueForStringLiteralCastToIComparable()
        {
            var code = @"
class C
{
    void M()
    {
        System.IComparable c = (IComparable)""Foo"";
    }
}";

            TestMissing(code,
                root => root.Members[0].Members[0].Body.Statements[0]);
        }

        [Fact]
        public void IssueForEnum()
        {
            var code = @"
enum E { A, B, C}
class C
{
    void M()
    {
        E e = E.B;
    }
}";

            var expected = @"
enum E { A, B, C}
class C
{
    void M()
    {
        const E e = E.B;
    }
}";

            Test(code, expected,
                root => root.Members[1].Members[0].Body.Statements[0]);
        }

        [Fact]
        public void IssueForNullWithExplicitCast()
        {
            var code = @"
class C
{
    void M()
    {
        C c = (C)null;
    }
}";

            var expected = @"
class C
{
    void M()
    {
        const C c = (C)null;
    }
}";

            Test(code, expected,
                root => root.Members[0].Members[0].Body.Statements[0]);
        }

        private void Test(string code, string expected, int issueIndex = 0, int actionIndex = 0, bool compareTokens = true)
        {
            Test(
                code: "class C { void M() { " + code + " } }",
                expected: "class C { void M() { " + expected + " } }",
                nodeFinder: root => root.Members[0].Members[0].Body.Statements[0],
                issueIndex: issueIndex,
                actionIndex: actionIndex,
                compareTokens: compareTokens);
        }

        private void TestMissing(string code)
        {
            TestMissing(
                code: "class C { void M() { " + code + " } }",
                nodeFinder: root => root.Members[0].Members[0].Body.Statements[0]);
        }

        protected override ICodeIssueProvider CreateCodeIssueProvider()
        {
            return new CodeIssueProvider();
        }

        protected override string LanguageName
        {
            get { return LanguageNames.CSharp; }
        }
    }
}
#endif