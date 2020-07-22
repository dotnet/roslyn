// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractClass;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.PullMemberUp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractClass
{
    public class ExtractClassTests : AbstractCSharpCodeActionTest
    {
        private Task TestAsync(
            string input,
            string expected,
            IEnumerable<(string name, bool makeAbstract)> dialogSelection = null,
            bool sameFile = false,
            bool expectNullMember = false,
            TestParameters testParameters = default)
        {
            var service = new ExtractClassOptionsService(dialogSelection, sameFile, expectNullMember);

            return TestInRegularAndScript1Async(
                input,
                expected,
                parameters: testParameters.WithFixProviderData(service));
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
           => new CSharpExtractClassCodeRefactoringProvider((IExtractClassOptionsService)parameters.fixProviderData);

        [Fact]
        public async Task TestSingleMethod()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestEvent()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class Test
{
    private event EventHandler [||]Event1;    
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    private event EventHandler Event1;
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestProperty()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]MyProperty { get; set; }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int MyProperty { get; set; }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestField()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]MyField;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int MyField;
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestFileHeader()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">// this is my document header
// that should be copied over

class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">// this is my document header
// that should be copied over

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">// this is my document header
// that should be copied over

internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestWithInterface()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
interface ITest 
{
    int Method();
}

class Test : ITest
{
    int [||]Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
interface ITest 
{
    int Method();
}

class Test : MyBase, ITest
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45977")]
        public async Task TestRegion()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    #region MyRegion
    int [||]Method()
    {
        return 1 + 1;
    }

    void OtherMethiod() { }
    #endregion
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{

    #region MyRegion

    void OtherMethiod() { }
    #endregion
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    #region MyRegion
    int Method()
    {
        return 1 + 1;
    }
    #endregion
}</Document>
    </Project>
</Workspace>";

            await TestAsync(
                input,
                expected,
                dialogSelection: new[] { ("Method", false) });
        }

        [Fact]
        public async Task TestMakeAbstract_SingleMethod()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
    override int Method()
    {
        return 1 + 1;
    }
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal abstract class MyBase
{
    private abstract global::System.Int32 Method();
}</Document>
    </Project>
</Workspace>";

            await TestAsync(
                input,
                expected,
                dialogSelection: new[] { ("Method", true) });
        }

        [Fact]
        public async Task TestMakeAbstract_MultipleMethods()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }

    int Method2() => 2;
    int Method3() => 3;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
    override int Method()
    {
        return 1 + 1;
    }

    override int Method2() => 2;
    override int Method3() => 3;
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal abstract class MyBase
{
    private abstract global::System.Int32 Method();
    private abstract global::System.Int32 Method2();
    private abstract global::System.Int32 Method3();
}</Document>
    </Project>
</Workspace>";

            await TestAsync(
                input,
                expected,
                dialogSelection: new[] { ("Method", true), ("Method2", true), ("Method3", true) });
        }

        [Fact]
        public async Task TestMultipleMethods()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}</Document>
    </Project>
</Workspace>";

            await TestAsync(
                input,
                expected,
                dialogSelection: new[] { ("Method", false), ("Method2", false) });
        }

        [Fact]
        public async Task TestMultipleMethods_SomeSelected()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    int [||]Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
    int Method()
    {
        return Method2() + 1;
    }
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{

    int Method2() => 1;
}</Document>
    </Project>
</Workspace>";

            await TestAsync(
                input,
                expected,
                dialogSelection: new[] { ("Method2", false) });
        }

        [Fact]
        public async Task TestSelection_CompleteMethodAndComments()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    [|/// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }|]
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestSelection_PartialMethodAndComments()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    [|/// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {|]
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestSelection_PartialMethodAndComments2()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    /// <summary>
    /// [|this is a test method
    /// </summary>
    int Method()
    {|]
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestSelection_PartialMethodAndComments3()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    /// <summary>
    /// [|this is a test method
    /// </summary>
    int Method()|]
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestAttributes()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }

class Test
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [||][TestAttribute]
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestAttributes2()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }
class TestAttribute2 : Attribute { }

class Test
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [||][TestAttribute]
    [TestAttribute2]
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }
class TestAttribute2 : Attribute { }

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    [TestAttribute2]
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45987")]
        public async Task TestAttributes3()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }
class TestAttribute2 : Attribute { }

class Test
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    [||][TestAttribute2]
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    [TestAttribute2]
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45987")]
        public async Task TestAttributes4()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }
class TestAttribute2 : Attribute { }

class Test
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    [TestAttribute2][||]
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    [TestAttribute2]
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected);
        }

        [Fact]
        public async Task TestSameFile()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test
{
    void Method[||]()
    {
    }
}
        </Document>
    </Project>
</Workspace>";
            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
internal class MyBase
{
    void Method()
    {
    }
}

class Test : MyBase
{
}
        </Document>
    </Project>
</Workspace>";
            await TestAsync(input, expected, sameFile: true);
        }

        [Fact]
        public async Task TestClassDeclaration()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test[||]
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration2()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class [||]Test
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration3()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
[||]class Test
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration4()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class[||] Test
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_Comment()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// [|This is a test class
/// </summary>
class Test|]
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_Comment2()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// This is a test class
/// [|</summary>
class Test|]
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_Comment3()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// This is a [|test class
/// </summary>
class|] Test
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_Attribute()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

public class MyAttribute : Attribute { }

[||][MyAttribute]
class Test
{
    int Method()
    {
        return 1 + 1;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
using System;

public class MyAttribute : Attribute { }

[MyAttribute]
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">[MyAttribute]
internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_SelectWithMembers()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
[|class Test
{
    int Method()
    {
        return 1 + 1;
    }
}|]
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        [Fact]
        public async Task TestClassDeclaration_SelectWithMembers2()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
[|class Test
{
    int Method()
    {
        return 1 + 1;
    }|]
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"">
        <Document FilePath=""Test.cs"">
class Test : MyBase
{
}
        </Document>
        <Document FilePath=""MyBase.cs"">internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}</Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, expectNullMember: true);
        }

        private class ExtractClassOptionsService : IExtractClassOptionsService
        {
            private readonly IEnumerable<(string name, bool makeAbstract)> _dialogSelection;
            private readonly bool _sameFile;
            private readonly bool _expectNullMember;

            public ExtractClassOptionsService(IEnumerable<(string name, bool makeAbstract)> dialogSelection = null, bool sameFile = false, bool expectNullMember = false)
            {
                _dialogSelection = dialogSelection;
                _sameFile = sameFile;
                _expectNullMember = expectNullMember;
            }

            public string FileName { get; set; } = "MyBase.cs";
            public string BaseName { get; set; } = "MyBase";

            public Task<ExtractClassOptions> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalSymbol, ISymbol selectedMember)
            {
                var availableMembers = originalSymbol.GetMembers().Where(member => MemberAndDestinationValidator.IsMemberValid(member));

                IEnumerable<(ISymbol member, bool makeAbstract)> selections;

                if (_dialogSelection == null)
                {
                    if (selectedMember is null)
                    {
                        Assert.True(_expectNullMember);
                        selections = availableMembers.Select(member => (member, makeAbstract: false));
                    }
                    else
                    {
                        Assert.False(_expectNullMember);
                        selections = new[] { (selectedMember, false) };
                    }
                }
                else
                {
                    selections = _dialogSelection.Select(selection => (member: availableMembers.Single(symbol => symbol.Name == selection.name), selection.makeAbstract));
                }

                var memberAnalysis = selections.Select(s =>
                    new ExtractClassMemberAnalysisResult(
                        s.member,
                        s.makeAbstract))
                    .ToImmutableArray();

                return Task.FromResult(new ExtractClassOptions(FileName, BaseName, _sameFile, memberAnalysis));
            }
        }
    }
}
