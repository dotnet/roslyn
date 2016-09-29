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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Roslyn.UnitTestFramework;
using Xunit;

namespace ConvertToConditionalCS.UnitTests
{
    public class ConvertToConditionalTests : CodeRefactoringProviderTestFixture
    {
        [Fact]
        public void ReturnSimpleCase()
        {
            string initialCode =
@"class C
{
    int M(bool p)
    {
        [||]if (p)
            return 0;
        else
            return 1;
    }
}";

            string expectedCode =
@"class C
{
    int M(bool p)
    {
        return p ? 0 : 1;
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnCastToReturnType()
        {
            string initialCode =
@"class C
{
    byte M(bool p)
    {
        [||]if (p)
            return 0;
        else
            return 1;
    }
}";

            string expectedCode =
@"class C
{
    byte M(bool p)
    {
        return (byte)(p ? 0 : 1);
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnReferenceTypes()
        {
            string initialCode =
@"class A
{
}

class B : A
{
}

class C : B
{
}

class D
{
    A M(bool p)
    {
        [||]if (p)
            return new C();
        else
            return new B();
    }
}";

            string expectedCode =
@"class A
{
}

class B : A
{
}

class C : B
{
}

class D
{
    A M(bool p)
    {
        return p ? new C() : new B();
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnReferenceTypesWithCast()
        {
            string initialCode =
@"class A
{
}

class B : A
{
}

class C : A
{
}

class D
{
    A M(bool p)
    {
        [||]if (p)
            return new C();
        else
            return new B();
    }
}";

            string expectedCode =
@"class A
{
}

class B : A
{
}

class C : A
{
}

class D
{
    A M(bool p)
    {
        return p ? (A)new C() : new B();
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ParenthesizeConditionThatIsBooleanAssignment_Bug8236()
        {
            string initialCode =
@"using System;

public class C
{
    int Foo(bool x, bool y)
    {
        [||]if (x = y)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}
";

            string expectedCode =
@"using System;

public class C
{
    int Foo(bool x, bool y)
    {
        return (x = y) ? 1 : 2;
    }
}
";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ParenthesizeLambdaIfNeeded_Bug8238()
        {
            string initialCode =
@"using System;

public class C
{
    Func<int> Foo(bool x)
    {
        [||]if (x)
        {
            return () => 1;
        }
        else
        {
            return () => 2;
        }
    }
}
";

            string expectedCode =
@"using System;

public class C
{
    Func<int> Foo(bool x)
    {
        return x ? (Func<int>)(() => 1) : () => 2;
    }
}
";

            Test(initialCode, expectedCode);
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider()
        {
            return new ConvertToConditionalCodeRefactoringProvider();
        }

        protected override string LanguageName
        {
            get { return LanguageNames.CSharp; }
        }
    }
}