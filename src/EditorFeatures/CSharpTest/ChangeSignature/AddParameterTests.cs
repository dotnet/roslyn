// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact]
        public async Task AddParameters()
        {
            var markup = """
                static class Ext
                {
                    /// <summary>
                    /// This is a summary of <see cref="M(object, int, string, bool, int, string, int[])"/>
                    /// </summary>
                    /// <param name="o"></param>
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    /// <param name="x"></param>
                    /// <param name="y"></param>
                    /// <param name="p"></param>
                    static void $$M(this object o, int a, string b, bool c, int x = 0, string y = "Zero", params int[] p)
                    {
                        object t = new object();

                        M(t, 1, "two", true, 3, "four", new[] { 5, 6 });
                        M(t, 1, "two", true, 3, "four", 5, 6);
                        t.M(1, "two", true, 3, "four", new[] { 5, 6 });
                        t.M(1, "two", true, 3, "four", 5, 6);

                        M(t, 1, "two", true, 3, "four");
                        M(t, 1, "two", true, 3);
                        M(t, 1, "two", true);

                        M(t, 1, "two", c: true);
                        M(t, 1, "two", true, 3, y: "four");

                        M(t, 1, "two", true, 3, p: new[] { 5 });
                        M(t, 1, "two", true, p: new[] { 5 });
                        M(t, 1, "two", true, y: "four");
                        M(t, 1, "two", true, x: 3);

                        M(t, 1, "two", true, y: "four", x: 3);
                        M(t, 1, y: "four", x: 3, b: "two", c: true);
                        M(t, y: "four", x: 3, c: true, b: "two", a: 1);
                        M(t, p: new[] { 5 }, y: "four", x: 3, c: true, b: "two", a: 1);
                        M(p: new[] { 5 }, y: "four", x: 3, c: true, b: "two", a: 1, o: t);
                    }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "System.Int32"),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "string", "newString", CallSiteKind.Value, ""), "System.String"),
                new AddedParameterOrExistingIndex(5)};
            var updatedCode = """
                static class Ext
                {
                    /// <summary>
                    /// This is a summary of <see cref="M(object, string, int, string, string)"/>
                    /// </summary>
                    /// <param name="o"></param>
                    /// <param name="b"></param>
                    /// <param name="newIntegerParameter"></param>
                    /// <param name="newString"></param>
                    /// <param name="y"></param>
                    /// 
                    /// 
                    static void M(this object o, string b, int newIntegerParameter, string newString, string y = "Zero")
                    {
                        object t = new object();

                        M(t, "two", 12345, , "four");
                        M(t, "two", 12345, , "four");
                        t.M("two", 12345, , "four");
                        t.M("two", 12345, , "four");

                        M(t, "two", 12345, , "four");
                        M(t, "two", 12345, );
                        M(t, "two", 12345, );

                        M(t, "two", 12345, );
                        M(t, "two", 12345, , y: "four");

                        M(t, "two", 12345, );
                        M(t, "two", 12345, );
                        M(t, "two", 12345, , y: "four");
                        M(t, "two", 12345, );

                        M(t, "two", 12345, , y: "four");
                        M(t, y: "four", newIntegerParameter: 12345, newString:, b: "two");
                        M(t, y: "four", newIntegerParameter: 12345, newString:, b: "two");
                        M(t, y: "four", newIntegerParameter: 12345, newString:, b: "two");
                        M(y: "four", b: "two", newIntegerParameter: 12345, newString:, o: t);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddParameterToParameterlessMethod()
        {
            var markup = """
                static class Ext
                {
                    static void $$M()
                    {
                        M();
                    }
                }
                """;
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "System.Int32")};
            var updatedCode = """
                static class Ext
                {
                    static void M(int newIntegerParameter)
                    {
                        M(12345);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderLocalFunctionParametersAndArguments_OnDeclaration()
        {
            var markup = """
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
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class MyClass
                {
                    public void M()
                    {
                        Goo(2, 34, 1);
                        void Goo(string y, byte b, int x)
                        {
                        }
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderLocalFunctionParametersAndArguments_OnInvocation()
        {
            var markup = """
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
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class MyClass
                {
                    public void M()
                    {
                        Goo(null, 34, 1);
                        void Goo(string y, byte b, int x)
                        {
                        }
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderMethodParameters()
        {
            var markup = """
                using System;
                class MyClass
                {
                    public void $$Goo(int x, string y)
                    {
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class MyClass
                {
                    public void Goo(string y, byte b, int x)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderMethodParametersAndArguments()
        {
            var markup = """
                using System;
                class MyClass
                {
                    public void $$Goo(int x, string y)
                    {
                        Goo(3, "hello");
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class MyClass
                {
                    public void Goo(string y, byte b, int x)
                    {
                        Goo("hello", 34, 3);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderMethodParametersAndArgumentsOfNestedCalls()
        {
            var markup = """
                using System;
                class MyClass
                {
                    public int $$Goo(int x, string y)
                    {
                        return Goo(Goo(4, "inner"), "outer");
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class MyClass
                {
                    public int Goo(string y, byte b, int x)
                    {
                        return Goo("outer", 34, Goo("inner", 34, 4));
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderConstructorParametersAndArguments()
        {
            var markup = """
                using System;

                class MyClass2 : MyClass
                {
                    public MyClass2() : base(5, "test2")
                    {
                    }
                }

                class MyClass
                {
                    public MyClass() : this(2, "test")
                    {
                    }

                    public $$MyClass(int x, string y)
                    {
                        var t = new MyClass(x, y);
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                AddedParameterOrExistingIndex.CreateAdded("byte", "b", CallSiteKind.Value, "34"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;

                class MyClass2 : MyClass
                {
                    public MyClass2() : base("test2", 34, 5)
                    {
                    }
                }

                class MyClass
                {
                    public MyClass() : this("test", 34, 2)
                    {
                    }

                    public MyClass(string y, byte b, int x)
                    {
                        var t = new MyClass(y, 34, x);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderAttributeConstructorParametersAndArguments()
        {
            var markup = """
                [My("test", 8)]
                class MyClass
                {
                }

                class MyAttribute : System.Attribute
                {
                    public MyAttribute(string x, int y)$$
                    {
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                [My(8, 34, "test")]
                class MyClass
                {
                }

                class MyAttribute : System.Attribute
                {
                    public MyAttribute(int y, byte b, string x)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderExtensionMethodParametersAndArguments_StaticCall()
        {
            var markup = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        CExt.M(new C(), 1, 2, "three", "four", "five");
                    }
                }

                public static class CExt
                {
                    public static void M(this $$C goo, int x, int y, string a = "test_a", string b = "test_b", string c = "test_c")
                    { }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(5),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3)};

            var updatedCode = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        CExt.M(new C(), 2, 1, 34, "five", "four", "three");
                    }
                }

                public static class CExt
                {
                    public static void M(this C goo, int y, int x, byte b, string c = "test_c", string b = "test_b", string a = "test_a")
                    { }
                }
                """;

            // Although the `ParameterConfig` has 0 for the `SelectedIndex`, the UI dialog will make an adjustment
            // and select parameter `y` instead because the `this` parameter cannot be moved or removed.
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [Fact]
        public async Task AddAndReorderExtensionMethodParametersAndArguments_ExtensionCall()
        {
            var markup = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        new C().M(1, 2, "three", "four", "five");
                    }
                }

                public static class CExt
                {
                    public static void M(this C goo, int x$$, int y, string a = "test_a", string b = "test_b", string c = "test_c")
                    { }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(5),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3)};
            var updatedCode = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        new C().M(2, 1, 34, "five", "four", "three");
                    }
                }

                public static class CExt
                {
                    public static void M(this C goo, int y, int x, byte b, string c = "test_c", string b = "test_b", string a = "test_a")
                    { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 1);
        }

        [Fact]
        public async Task AddParameterWithOmittedArgument_ParamsAsArray()
        {
            var markup = """
                public class C
                {
                    void $$M(int x, int y, params int[] p)
                    {
                        M(x, y, p: p);
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(1),
                AddedParameterOrExistingIndex.CreateAdded("int", "z", CallSiteKind.Omitted, isRequired: false, defaultValue: "3"),
                new AddedParameterOrExistingIndex(2)};
            var updatedCode = """
                public class C
                {
                    void M(int x, int y, int z = 3, params int[] p)
                    {
                        M(x, y, p: p);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamsMethodParametersAndArguments_ParamsAsArray()
        {
            var markup = """
                public class C
                {
                    void $$M(int x, int y, params int[] p)
                    {
                        M(x, y, new[] { 1, 2, 3 });
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(2)};
            var updatedCode = """
                public class C
                {
                    void M(int y, int x, byte b, params int[] p)
                    {
                        M(y, x, 34, new[] { 1, 2, 3 });
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamsMethodParametersAndArguments_ParamsExpanded()
        {
            var markup = """
                public class C
                {
                    void $$M(int x, int y, params int[] p)
                    {
                        M(x, y, 1, 2, 3);
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(2)};

            var updatedCode = """
                public class C
                {
                    void M(int y, int x, byte b, params int[] p)
                    {
                        M(y, x, 34, 1, 2, 3);
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderExtensionAndParamsMethodParametersAndArguments_VariedCallsites()
        {
            var markup = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        CExt.M(new C(), 1, 2, "three", "four", "five", new[] { 6, 7, 8 });
                        CExt.M(new C(), 1, 2, "three", "four", "five", 6, 7, 8);
                        new C().M(1, 2, "three", "four", "five", new[] { 6, 7, 8 });
                        new C().M(1, 2, "three", "four", "five", 6, 7, 8);
                    }
                }

                public static class CExt
                {
                    public static void $$M(this C goo, int x, int y, string a = "test_a", string b = "test_b", string c = "test_c", params int[] p)
                    { }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(5),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(6)};
            var updatedCode = """
                public class C
                {
                    static void Main(string[] args)
                    {
                        CExt.M(new C(), 2, 1, 34, "five", "four", "three", new[] { 6, 7, 8 });
                        CExt.M(new C(), 2, 1, 34, "five", "four", "three", 6, 7, 8);
                        new C().M(2, 1, 34, "five", "four", "three", new[] { 6, 7, 8 });
                        new C().M(2, 1, 34, "five", "four", "three", 6, 7, 8);
                    }
                }

                public static class CExt
                {
                    public static void M(this C goo, int y, int x, byte b, string c = "test_c", string b = "test_b", string a = "test_a", params int[] p)
                    { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [Fact]
        public async Task AddAndReorderIndexerParametersAndArguments()
        {
            var markup = """
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
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                class Program
                {
                    void M()
                    {
                        var x = new Program()[2, 34, 1];
                        new Program()[2, 34, 1] = x;
                    }

                    public int this[int y, byte b, int x]
                    {
                        get { return 5; }
                        set { }
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_SingleLineDocComments_OnIndividualLines()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="c"></param>
                    /// <param name="b"></param>
                    /// <param name="bb"></param>
                    /// <param name="a"></param>
                    void Goo(int c, int b, byte bb, int a)
                    {

                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_SingleLineDocComments_OnSameLine()
        {
            var markup = """
                public class C
                {
                    /// <param name="a">a is fun</param><param name="b">b is fun</param><param name="c">c is fun</param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="c">c is fun</param><param name="b">b is fun</param><param name="bb"></param>
                    /// <param name="a">a is fun</param>
                    void Goo(int c, int b, byte bb, int a)
                    {

                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_SingleLineDocComments_MixedLineDistribution()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param><param name="b"></param>
                    /// <param name="c"></param>
                    /// <param name="d"></param>
                    /// <param name="e">Comments spread
                    /// over several
                    /// lines</param><param name="f"></param>
                    void $$Goo(int a, int b, int c, int d, int e, int f)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(5),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="f"></param><param name="e">Comments spread
                    /// over several
                    /// lines</param>
                    /// <param name="d"></param>
                    /// <param name="c"></param>
                    /// <param name="bb"></param><param name="b"></param>
                    /// <param name="a"></param>
                    void Goo(int f, int e, int d, int c, byte bb, int b, int a)
                    {

                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_SingleLineDocComments_MixedWithRegularComments()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param><param name="b"></param>
                    // Why is there a regular comment here?
                    /// <param name="c"></param><param name="d"></param><param name="e"></param>
                    void $$Goo(int a, int b, int c, int d, int e)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="e"></param><param name="d"></param>
                    // Why is there a regular comment here?
                    /// <param name="c"></param><param name="b"></param><param name="b"></param>
                    /// <param name="a"></param>
                    void Goo(int e, int d, int c, byte b, int b, int a)
                    {

                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_MultiLineDocComments_OnSeparateLines1()
        {
            var markup = """
                class Program
                {
                    /**
                     * <param name="x">x!</param>
                     * <param name="y">y!</param>
                     * <param name="z">z!</param>
                     */
                    static void $$M(int x, int y, int z)
                    {
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                class Program
                {
                    /**
                     * <param name="z">z!</param>
                     * <param name="b"></param>
                     * <param name="y">y!</param>
                     */
                    /// <param name="x">x!</param>
                    static void M(int z, byte b, int y, int x)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_MultiLineDocComments_OnSingleLine()
        {
            var markup = """
                class Program
                {
                    /** <param name="x">x!</param><param name="y">y!</param><param name="z">z!</param> */
                    static void $$M(int x, int y, int z)
                    {
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                class Program
                {
                    /** <param name="z">z!</param><param name="b"></param><param name="y">y!</param> */
                    /// <param name="x">x!</param>
                    static void M(int z, byte b, int y, int x)
                    {
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_IncorrectOrder_MaintainsOrder()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="c"></param>
                    /// <param name="b"></param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="c"></param>
                    /// <param name="b"></param>
                    void Goo(int c, byte bb, int b, int a)
                    {

                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_WrongNames_MaintainsOrder()
        {
            var markup = """
                public class C
                {
                    /// <param name="a2"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="a2"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    void Goo(int c, byte b, int b, int a)
                    {

                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_InsufficientTags_MaintainsOrder()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="c"></param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="c"></param>
                    void Goo(int c, byte b, int b, int a)
                    {

                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_ExcessiveTags_MaintainsOrder()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    /// <param name="d"></param>
                    void $$Goo(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    /// <param name="d"></param>
                    void Goo(int c, byte bb, int b, int a)
                    {

                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_OnConstructors()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    public $$C(int a, int b, int c)
                    {

                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="c"></param>
                    /// <param name="bb"></param>
                    /// <param name="b"></param>
                    /// <param name="a"></param>
                    public C(int c, byte bb, int b, int a)
                    {

                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParamTagsInDocComments_OnIndexers()
        {
            var markup = """
                public class C
                {
                    /// <param name="a"></param>
                    /// <param name="b"></param>
                    /// <param name="c"></param>
                    public int $$this[int a, int b, int c]
                    {
                        get { return 5; }
                        set { }
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                public class C
                {
                    /// <param name="c"></param>
                    /// <param name="bb"></param>
                    /// <param name="b"></param>
                    /// <param name="a"></param>
                    public int this[int c, byte bb, int b, int a]
                    {
                        get { return 5; }
                        set { }
                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParametersInCrefs()
        {
            var markup = """
                class C
                {
                    /// <summary>
                    /// See <see cref="M(int, string)"/> and <see cref="M"/>
                    /// </summary>
                    $$void M(int x, string y)
                    { }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                class C
                {
                    /// <summary>
                    /// See <see cref="M(string, byte, int)"/> and <see cref="M"/>
                    /// </summary>
                    void M(string y, byte b, int x)
                    { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType1()
        {
            var markup = """
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
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                interface I
                {
                    void M(string y, byte b, int x);
                }

                class C
                {
                    public void M(string y, byte b, int x)
                    {
                    }
                }

                class D : C, I
                {
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddAndReorderParametersInMethodThatImplementsInterfaceMethodOnlyThroughADerivedType2()
        {
            var markup = """
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
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                interface I
                {
                    void M(string y, byte b, int x);
                }

                class C
                {
                    public void M(string y, byte b, int x)
                    {
                    }
                }

                class D : C, I
                {
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43664")]
        public async Task AddParameterOnUnparenthesizedLambda()
        {
            var markup = """
                using System.Linq;

                namespace ConsoleApp426
                {
                    class Program
                    {
                        static void M(string[] args)
                        {
                            if (args.All(b$$ => Test()))
                            {

                            }
                        }

                        static bool Test() { return true; }
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(0),
                AddedParameterOrExistingIndex.CreateAdded("byte", "bb", CallSiteKind.Value, callSiteValue: "34") };

            var updatedCode = """
                using System.Linq;

                namespace ConsoleApp426
                {
                    class Program
                    {
                        static void M(string[] args)
                        {
                            if (args.All((b, byte bb) => Test()))
                            {

                            }
                        }

                        static bool Test() { return true; }
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44126")]
        public async Task AddAndReorderImplicitObjectCreationParameter()
        {
            var markup = """
                using System;
                class C
                {
                    $$C(int x, string y)
                    {
                    }

                    public void M()
                    {
                        C _ = new(1, "y");
                    }
                }
                """;
            var permutation = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "b", CallSiteKind.Value, callSiteValue: "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var updatedCode = """
                using System;
                class C
                {
                    C(string y, byte b, int x)
                    {
                    }

                    public void M()
                    {
                        C _ = new("y", 34, 1);
                    }
                }
                """;
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44558")]
        public async Task AddParameters_Record()
        {
            var markup = """
                /// <param name="First"></param>
                /// <param name="Second"></param>
                /// <param name="Third"></param>
                record $$R(int First, int Second, int Third)
                {
                    static R M() => new R(1, 2, 3);
                }
                """;
            var updatedSignature = new AddedParameterOrExistingIndex[]
            {
                new(0),
                new(2),
                new(1),
                new(new AddedParameter(null, "int", "Forth", CallSiteKind.Value, "12345"), "System.Int32")
            };
            var updatedCode = """
                /// <param name="First"></param>
                /// <param name="Third"></param>
                /// <param name="Second"></param>
                /// <param name="Forth"></param>
                record R(int First, int Third, int Second, int Forth)
                {
                    static R M() => new R(1, 3, 2, 12345);
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddParameters_PrimaryConstructor_Class()
        {
            var markup = """
                /// <param name="First"></param>
                /// <param name="Second"></param>
                /// <param name="Third"></param>
                class $$R(int First, int Second, int Third)
                {
                    static R M() => new R(1, 2, 3);
                }
                """;
            var updatedSignature = new AddedParameterOrExistingIndex[]
            {
                new(0),
                new(2),
                new(1),
                new(new AddedParameter(null, "int", "Forth", CallSiteKind.Value, "12345"), "System.Int32")
            };
            var updatedCode = """
                /// <param name="First"></param>
                /// <param name="Third"></param>
                /// <param name="Second"></param>
                /// <param name="Forth"></param>
                class R(int First, int Third, int Second, int Forth)
                {
                    static R M() => new R(1, 3, 2, 12345);
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddParameters_PrimaryConstructor_Struct()
        {
            var markup = """
                /// <param name="First"></param>
                /// <param name="Second"></param>
                /// <param name="Third"></param>
                struct $$R(int First, int Second, int Third)
                {
                    static R M() => new R(1, 2, 3);
                }
                """;
            var updatedSignature = new AddedParameterOrExistingIndex[]
            {
                new(0),
                new(2),
                new(1),
                new(new AddedParameter(null, "int", "Forth", CallSiteKind.Value, "12345"), "System.Int32")
            };
            var updatedCode = """
                /// <param name="First"></param>
                /// <param name="Third"></param>
                /// <param name="Second"></param>
                /// <param name="Forth"></param>
                struct R(int First, int Third, int Second, int Forth)
                {
                    static R M() => new R(1, 3, 2, 12345);
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
