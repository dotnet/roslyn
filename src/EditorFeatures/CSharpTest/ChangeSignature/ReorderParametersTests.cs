// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderLocalFunctionParametersAndArguments_OnDeclaration()
        {
            var markup = @"
using System;
class MyClass
{
    public void M()
    {
        Goo(1, 2);
        void $$Goo(int x, string y)
        {
        }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void M()
    {
        Goo(2, 1);
        void Goo(string y, int x)
        {
        }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderLocalFunctionParametersAndArguments_OnInvocation()
        {
            var markup = @"
using System;
class MyClass
{
    public void M()
    {
        $$Goo(1, null);
        void Goo(int x, string y)
        {
        }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void M()
    {
        Goo(null, 1);
        void Goo(string y, int x)
        {
        }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParametersAndArguments()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParametersAndArgumentsOfNestedCalls()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderConstructorParametersAndArguments()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderAttributeConstructorParametersAndArguments()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderExtensionMethodParametersAndArguments_StaticCall()
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
    public static void M(this $$C goo, int x, int y, string a = ""test_a"", string b = ""test_b"", string c = ""test_c"")
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

            // Although the `ParameterConfig` has 0 for the `SelectedIndex`, the UI dialog will make an adjustment
            // and select parameter `y` instead because the `this` parameter cannot be moved or removed.
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderExtensionMethodParametersAndArguments_ExtensionCall()
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
    public static void M(this C goo, int x$$, int y, string a = ""test_a"", string b = ""test_b"", string c = ""test_c"")
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamsMethodParametersAndArguments_ParamsAsArray()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamsMethodParametersAndArguments_ParamsExpanded()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderExtensionAndParamsMethodParametersAndArguments_VariedCallsites()
        {
            var markup = @"
public class C
{
    static void Main(string[] args)
    {
        CExt.M(new C(), 1, 2, ""three"", ""four"", ""five"", new[] { 6, 7, 8 });
        CExt.M(new C(), 1, 2, ""three"", ""four"", ""five"", 6, 7, 8);
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
        CExt.M(new C(), 2, 1, ""five"", ""four"", ""three"", 6, 7, 8);
        new C().M(2, 1, ""five"", ""four"", ""three"", new[] { 6, 7, 8 });
        new C().M(2, 1, ""five"", ""four"", ""three"", 6, 7, 8);
    }
}

public static class CExt
{
    public static void M(this C goo, int y, int x, string c = ""test_c"", string b = ""test_b"", string a = ""test_a"", params int[] p)
    { }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParametersAndArguments()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_SingleLineDocComments_OnIndividualLines()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_SingleLineDocComments_OnSameLine()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_SingleLineDocComments_MixedLineDistribution()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_SingleLineDocComments_MixedWithRegularComments()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_MultiLineDocComments_OnSeparateLines1()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_MultiLineDocComments_OnSingleLine()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_IncorrectOrder_MaintainsOrder()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_WrongNames_MaintainsOrder()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_InsufficientTags_MaintainsOrder()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_ExcessiveTags_MaintainsOrder()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_OnConstructors()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParamTagsInDocComments_OnIndexers()
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
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParametersInCrefs()
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
    /// See <see cref=""M(string, int)""/> and <see cref=""M""/>
    /// </summary>
    void M(string y, int x)
    { }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType1()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType2()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
