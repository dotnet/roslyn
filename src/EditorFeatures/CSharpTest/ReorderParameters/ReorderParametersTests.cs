﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.ReorderParameters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReorderParameters
{
    public partial class ReorderParametersTests : AbstractReorderParametersTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters()
        {
            var markup = @"
using System;
class MyClass
{
    public void $$Goo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParametersAndArguments()
        {
            var markup = @"
using System;
class MyClass
{
    public void $$Goo(int x, string y)
    {
        Goo(3, ""hello"");
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
        Goo(""hello"", 3);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParametersAndArgumentsOfNestedCalls()
        {
            var markup = @"
using System;
class MyClass
{
    public int $$Goo(int x, string y)
    {
        return Goo(Goo(4, ""inner""), ""outer"");
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public int Goo(string y, int x)
    {
        return Goo(""outer"", Goo(""inner"", 4));
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderConstructorParametersAndArguments()
        {
            var markup = @"
using System;

class MyClass2 : MyClass
{
    public MyClass2() : base(5, ""test2"")
    {
    }
}

class MyClass
{
    public MyClass() : this(2, ""test"")
    {
    }

    public $$MyClass(int x, string y)
    {
        var t = new MyClass(x, y);
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;

class MyClass2 : MyClass
{
    public MyClass2() : base(""test2"", 5)
    {
    }
}

class MyClass
{
    public MyClass() : this(""test"", 2)
    {
    }

    public MyClass(string y, int x)
    {
        var t = new MyClass(y, x);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderAttributeConstructorParametersAndArguments()
        {
            var markup = @"
[My(""test"", 8)]
class MyClass
{
}

class MyAttribute : System.Attribute
{
    public MyAttribute(string x, int y)$$
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
[My(8, ""test"")]
class MyClass
{
}

class MyAttribute : System.Attribute
{
    public MyAttribute(int y, string x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderExtensionMethodParametersAndArguments_StaticCall()
        {
            var markup = @"
public class C
{
    static void Main(string[] args)
    {
        CExt.M(new C(), 1, 2, ""three"", ""four"", ""five"");
    }
}

public static class CExt
{
    public static void $$M(this C goo, int x, int y, string a = ""test_a"", string b = ""test_b"", string c = ""test_c"")
    { }
}";
            var permutation = new[] { 0, 2, 1, 5, 4, 3 };
            var updatedCode = @"
public class C
{
    static void Main(string[] args)
    {
        CExt.M(new C(), 2, 1, ""five"", ""four"", ""three"");
    }
}

public static class CExt
{
    public static void M(this C goo, int y, int x, string c = ""test_c"", string b = ""test_b"", string a = ""test_a"")
    { }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderExtensionMethodParametersAndArguments_ExtensionCall()
        {
            var markup = @"
public class C
{
    static void Main(string[] args)
    {
        new C().M(1, 2, ""three"", ""four"", ""five"");
    }
}

public static class CExt
{
    public static void $$M(this C goo, int x, int y, string a = ""test_a"", string b = ""test_b"", string c = ""test_c"")
    { }
}";
            var permutation = new[] { 0, 2, 1, 5, 4, 3 };
            var updatedCode = @"
public class C
{
    static void Main(string[] args)
    {
        new C().M(2, 1, ""five"", ""four"", ""three"");
    }
}

public static class CExt
{
    public static void M(this C goo, int y, int x, string c = ""test_c"", string b = ""test_b"", string a = ""test_a"")
    { }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamsMethodParametersAndArguments_ParamsAsArray()
        {
            var markup = @"
public class C
{
    void $$M(int x, int y, params int[] p)
    {
        M(x, y, new[] { 1, 2, 3 });
    }
}";
            var permutation = new[] { 1, 0, 2 };
            var updatedCode = @"
public class C
{
    void M(int y, int x, params int[] p)
    {
        M(y, x, new[] { 1, 2, 3 });
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamsMethodParametersAndArguments_ParamsExpanded()
        {
            var markup = @"
public class C
{
    void $$M(int x, int y, params int[] p)
    {
        M(x, y, 1, 2, 3);
    }
}";
            var permutation = new[] { 1, 0, 2 };
            var updatedCode = @"
public class C
{
    void M(int y, int x, params int[] p)
    {
        M(y, x, 1, 2, 3);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderExtensionAndParamsMethodParametersAndArguments_VariedCallsites()
        {
            var markup = @"
public class C
{
    static void Main(string[] args)
    {
        CExt.M(new C(), 1, 2, ""three"", ""four"", ""five"", new[] { 6, 7, 8 });
        CExt.M(new C(), 1, 2, ""three"", ""four"", ""five"", 6, 7, 8 );
        new C().M(1, 2, ""three"", ""four"", ""five"", new[] { 6, 7, 8 });
        new C().M(1, 2, ""three"", ""four"", ""five"", 6, 7, 8);
    }
}

public static class CExt
{
    public static void $$M(this C goo, int x, int y, string a = ""test_a"", string b = ""test_b"", string c = ""test_c"", params int[] p)
    { }
}";
            var permutation = new[] { 0, 2, 1, 5, 4, 3, 6 };
            var updatedCode = @"
public class C
{
    static void Main(string[] args)
    {
        CExt.M(new C(), 2, 1, ""five"", ""four"", ""three"", new[] { 6, 7, 8 });
        CExt.M(new C(), 2, 1, ""five"", ""four"", ""three"", 6, 7, 8 );
        new C().M(2, 1, ""five"", ""four"", ""three"", new[] { 6, 7, 8 });
        new C().M(2, 1, ""five"", ""four"", ""three"", 6, 7, 8);
    }
}

public static class CExt
{
    public static void M(this C goo, int y, int x, string c = ""test_c"", string b = ""test_b"", string a = ""test_a"", params int[] p)
    { }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParametersAndArguments()
        {
            var markup = @"
class Program
{
    void M()
    {
        var x = new Program()[1, 2];
        new Program()[1, 2] = x;
    }

    public int this[int x, int y]$$
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    void M()
    {
        var x = new Program()[2, 1];
        new Program()[2, 1] = x;
    }

    public int this[int y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact(Skip = "Not Yet Implemented"), Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderCollectionInitializerAddMethodParametersAndArguments()
        {
            var markup = @"
using System;
using System.Collections;

class Program : IEnumerable
{
    static void Main(string[] args)
    {
        new Program { { 1, 2 }, { ""three"", ""four"" }, { 5, 6 } };
    }

    public void Add(int x, int y)$$
    {
    }

    public void Add(string x, string y)
    {
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
using System.Collections;

class Program : IEnumerable
{
    static void Main(string[] args)
    {
        new Program { { 2, 1 }, { ""three"", ""four"" }, { 6, 5 } };
    }

    public void Add(int y, int x)
    {
    }

    public void Add(string x, string y)
    {
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_SingleLineDocComments_OnIndividualLines()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    void Goo(int c, int b, int a)
    {

    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_SingleLineDocComments_OnSameLine()
        {
            var markup = @"
public class C
{
    /// <param name=""a"">a is fun</param><param name=""b"">b is fun</param><param name=""c"">c is fun</param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""c"">c is fun</param><param name=""b"">b is fun</param><param name=""a"">a is fun</param>
    void Goo(int c, int b, int a)
    {

    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_SingleLineDocComments_MixedLineDistribution()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param><param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""d""></param>
    /// <param name=""e"">Comments spread
    /// over several
    /// lines</param><param name=""f""></param>
    void $$Goo(int a, int b, int c, int d, int e, int f)
    {

    }
}";
            var permutation = new[] { 5, 4, 3, 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""f""></param><param name=""e"">Comments spread
    /// over several
    /// lines</param>
    /// <param name=""d""></param>
    /// <param name=""c""></param>
    /// <param name=""b""></param><param name=""a""></param>
    void Goo(int f, int e, int d, int c, int b, int a)
    {

    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_SingleLineDocComments_MixedWithRegularComments()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param><param name=""b""></param>
    // Why is there a regular comment here?
    /// <param name=""c""></param><param name=""d""></param><param name=""e""></param>
    void $$Goo(int a, int b, int c, int d, int e)
    {

    }
}";
            var permutation = new[] { 4, 3, 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""e""></param><param name=""d""></param>
    // Why is there a regular comment here?
    /// <param name=""c""></param><param name=""b""></param><param name=""a""></param>
    void Goo(int e, int d, int c, int b, int a)
    {

    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_MultiLineDocComments_OnSeparateLines1()
        {
            var markup = @"
class Program
{
    /**
     * <param name=""x"">x!</param>
     * <param name=""y"">y!</param>
     * <param name=""z"">z!</param>
     */
    static void $$M(int x, int y, int z)
    {
    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
class Program
{
    /**
     * <param name=""z"">z!</param>
     * <param name=""y"">y!</param>
     * <param name=""x"">x!</param>
     */
    static void M(int z, int y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_MultiLineDocComments_OnSingleLine()
        {
            var markup = @"
class Program
{
    /** <param name=""x"">x!</param><param name=""y"">y!</param><param name=""z"">z!</param> */
    static void $$M(int x, int y, int z)
    {
    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
class Program
{
    /** <param name=""z"">z!</param><param name=""y"">y!</param><param name=""x"">x!</param> */
    static void M(int z, int y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_IncorrectOrder_MaintainsOrder()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    void Goo(int c, int b, int a)
    {

    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_WrongNames_MaintainsOrder()
        {
            var markup = @"
public class C
{
    /// <param name=""a2""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""a2""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Goo(int c, int b, int a)
    {

    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_InsufficientTags_MaintainsOrder()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""c""></param>
    void Goo(int c, int b, int a)
    {

    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_ExcessiveTags_MaintainsOrder()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""d""></param>
    void $$Goo(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""d""></param>
    void Goo(int c, int b, int a)
    {

    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_OnConstructors()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    public $$C(int a, int b, int c)
    {

    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    public C(int c, int b, int a)
    {

    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParamTagsInDocComments_OnIndexers()
        {
            var markup = @"
public class C
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    public int $$this[int a, int b, int c]
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
public class C
{
    /// <param name=""c""></param>
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    public int this[int c, int b, int a]
    {
        get { return 5; }
        set { }
    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParametersInCrefs()
        {
            var markup = @"
class C
{
    /// <summary>
    /// See <see cref=""M(int, string)""/> and <see cref=""M""/>
    /// </summary>
    $$void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class C
{
    /// <summary>
    /// See <see cref=""M( string,int)""/> and <see cref=""M""/>
    /// </summary>
    void M(string y, int x)
    { }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType1()
        {
            var markup = @"
interface I
{
    $$void M(int x, string y);
}

class C
{
    public void M(int x, string y)
    {
    }
}

class D : C, I
{
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
interface I
{
    void M(string y, int x);
}

class C
{
    public void M(string y, int x)
    {
    }
}

class D : C, I
{
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType2()
        {
            var markup = @"
interface I
{
    void M(int x, string y);
}

class C
{
    $$public void M(int x, string y)
    {
    }
}

class D : C, I
{
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
interface I
{
    void M(string y, int x);
}

class C
{
    public void M(string y, int x)
    {
    }
}

class D : C, I
{
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
