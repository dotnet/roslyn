﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.ImplementInterface.CSharpImplementInterfaceCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface
{
    public class ImplementInterfaceTests
    {
        private readonly NamingStylesTestOptionSets _options = new NamingStylesTestOptionSets(LanguageNames.CSharp);

        private static OptionsCollection AllOptionsOff
            => new OptionsCollection(LanguageNames.CSharp)
            {
                 { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private static OptionsCollection AllOptionsOn
            => new OptionsCollection(LanguageNames.CSharp)
            {
                 { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            };

        private static OptionsCollection AccessorOptionsOn
            => new OptionsCollection(LanguageNames.CSharp)
            {
                 { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                 { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        internal static async Task TestWithAllCodeStyleOptionsOffAsync(
            string initialMarkup, string expectedMarkup,
            (string equivalenceKey, int index)? codeAction = null)
        {
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = codeAction?.equivalenceKey,
                CodeActionIndex = codeAction?.index,
            }.RunAsync();
        }

        internal static async Task TestWithAllCodeStyleOptionsOnAsync(string initialMarkup, string expectedMarkup)
        {
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                Options = { AllOptionsOn },
            }.RunAsync();
        }

        internal static async Task TestWithAccessorCodeStyleOptionsOnAsync(string initialMarkup, string expectedMarkup)
        {
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                Options = { AccessorOptionsOn },
            }.RunAsync();
        }

        private static async Task TestInRegularAndScriptAsync(
            string initialMarkup,
            string expectedMarkup,
            (string equivalenceKey, int index)? codeAction = null)
        {
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionEquivalenceKey = codeAction?.equivalenceKey,
                CodeActionIndex = codeAction?.index,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethod()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    void Method1();
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    void Method1();
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethodInRecord()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"interface IInterface
{
    void Method1();
}

record Record : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();
}

record Record : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        [WorkItem(42986, "https://github.com/dotnet/roslyn/issues/42986")]
        public async Task TestMethodWithNativeIntegers()
        {
            var nativeIntegerAttributeDefinition = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.All)]
    public sealed class NativeIntegerAttribute : System.Attribute
    {
        public NativeIntegerAttribute()
        {
        }
        public NativeIntegerAttribute(bool[] flags)
        {
        }
    }
}";

            // Note: we're putting the attribute by hand to simulate metadata
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"interface IInterface
{
    [return: {|CS8335:System.Runtime.CompilerServices.NativeInteger(new[] { true, true })|}]
    (nint, nuint) Method(nint x, nuint x2);
}

class Class : {|CS0535:IInterface|}
{
}" + nativeIntegerAttributeDefinition,
                FixedCode = @"interface IInterface
{
    [return: {|CS8335:System.Runtime.CompilerServices.NativeInteger(new[] { true, true })|}]
    (nint, nuint) Method(nint x, nuint x2);
}

class Class : IInterface
{
    public (nint, nuint) Method(nint x, nuint x2)
    {
        throw new System.NotImplementedException();
    }
}" + nativeIntegerAttributeDefinition,
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethodWithTuple()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    (int, int) Method((string, string) x);
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    (int, int) Method((string, string) x);
}

class Class : IInterface
{
    public (int, int) Method((string, string) x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(16793, "https://github.com/dotnet/roslyn/issues/16793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethodWithValueTupleArity1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"
using System;
interface I
{
    ValueTuple<object> F();
}
class C : {|CS0535:I|}
{
}",
@"
using System;
interface I
{
    ValueTuple<object> F();
}
class C : I
{
    public ValueTuple<object> F()
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExpressionBodiedMethod1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"interface IInterface
{
    void Method1();
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    void Method1();
}

class Class : IInterface
{
    public void Method1() => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TupleWithNamesInMethod()
        {
            // Note: we're putting the attribute by hand to simulate metadata
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Method1((int c, string) x);
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Method1((int c, string) x);
}

class Class : IInterface
{
    public (int a, int b)[] Method1((int c, string) x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TupleWithNamesInMethod_Explicitly()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Method1((int c, string) x);
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Method1((int c, string) x);
}

class Class : IInterface
{
    (int a, int b)[] IInterface.Method1((int c, string) x)
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TupleWithNamesInProperty()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Property1 { [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}] get; [param: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}] set; }
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    (int a, int b)[] Property1 { [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}] get; [param: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}] set; }
}

class Class : IInterface
{
    public (int a, int b)[] Property1
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TupleWithNamesInEvent()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface IInterface
{
    [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    event System.Func<(int a, int b)> Event1;
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"using System;

interface IInterface
{
    [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { ""a"", ""b"" })|}]
    event System.Func<(int a, int b)> Event1;
}

class Class : IInterface
{
    public event Func<(int a, int b)> Event1;
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task NoDynamicAttributeInMethod()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    [return: {|CS1970:System.Runtime.CompilerServices.DynamicAttribute()|}]
    object Method1();
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    [return: {|CS1970:System.Runtime.CompilerServices.DynamicAttribute()|}]
    object Method1();
}

class Class : IInterface
{
    public object Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task NoNullableAttributesInMethodFromMetadata()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
#nullable enable

using System;

class C : {|CS0535:{|CS0535:IInterface|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"
#nullable enable

public interface IInterface
{
    void M(string? s1, string s2);
    string this[string? s1, string s2] { get; set; }
}"
                            },
                        },
                    },
                    AdditionalProjectReferences =
                    {
                        "Assembly1",
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
#nullable enable

using System;

class C : IInterface
{
    public string this[string? s1, string s2]
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public void M(string? s1, string s2)
    {
        throw new NotImplementedException();
    }
}",
                    },
                },
                CodeActionEquivalenceKey = "False;False;True:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethodWhenClassBracesAreMissing()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    void Method1();
}

class Class : {|CS0535:IInterface|}{|CS1513:|}{|CS1514:|}",
@"interface IInterface
{
    void Method1();
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInheritance1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
}

class Class : {|CS0535:IInterface2|}
{
}",
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
}

class Class : IInterface2
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInheritance2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
}

interface IInterface2 : IInterface1
{
    void Method1();
}

class Class : {|CS0535:IInterface2|}
{
}",
@"interface IInterface1
{
}

interface IInterface2 : IInterface1
{
    void Method1();
}

class Class : IInterface2
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInheritance3()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
    void Method2();
}

class Class : {|CS0535:{|CS0535:IInterface2|}|}
{
}",
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
    void Method2();
}

class Class : IInterface2
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }

    public void Method2()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInheritanceMatchingMethod()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
    void Method1();
}

class Class : {|CS0535:{|CS0535:IInterface2|}|}
{
}",
@"interface IInterface1
{
    void Method1();
}

interface IInterface2 : IInterface1
{
    void Method1();
}

class Class : IInterface2
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExistingConflictingMethodReturnType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
    void Method1();
}

class Class : {|CS0738:IInterface1|}
{
    public int Method1()
    {
        return 0;
    }
}",
@"interface IInterface1
{
    void Method1();
}

class Class : IInterface1
{
    public int Method1()
    {
        return 0;
    }

    void IInterface1.Method1()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExistingConflictingMethodParameters()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1
{
    void Method1(int i);
}

class Class : {|CS0535:IInterface1|}
{
    public void Method1(string i)
    {
    }
}",
@"interface IInterface1
{
    void Method1(int i);
}

class Class : IInterface1
{
    public void Method1(string i)
    {
    }

    public void Method1(int i)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementGenericType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1<T>
{
    void Method1(T t);
}

class Class : {|CS0535:IInterface1<int>|}
{
}",
@"interface IInterface1<T>
{
    void Method1(T t);
}

class Class : IInterface1<int>
{
    public void Method1(int t)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementGenericTypeWithGenericMethod()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1<T>
{
    void Method1<U>(T t, U u);
}

class Class : {|CS0535:IInterface1<int>|}
{
}",
@"interface IInterface1<T>
{
    void Method1<U>(T t, U u);
}

class Class : IInterface1<int>
{
    public void Method1<U>(int t, U u)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementGenericTypeWithGenericMethodWithNaturalConstraint()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections.Generic;
interface IInterface1<T>
{
    void Method1<U>(T t, U u) where U : IList<T>;
}

class Class : {|CS0535:IInterface1<int>|}
{
}",
@"using System.Collections.Generic;
interface IInterface1<T>
{
    void Method1<U>(T t, U u) where U : IList<T>;
}

class Class : IInterface1<int>
{
    public void Method1<U>(int t, U u) where U : IList<int>
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementGenericTypeWithGenericMethodWithUnexpressibleConstraint()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface1<T>
{
    void Method1<U>(T t, U u) where U : T;
}

class Class : {|CS0535:IInterface1<int>|}
{
}",
@"interface IInterface1<T>
{
    void Method1<U>(T t, U u) where U : T;
}

class Class : IInterface1<int>
{
    void IInterface1<int>.Method1<U>(int t, U u)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestArrayType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    string[] M();
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    string[] M();
}

class C : I
{
    public string[] M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMember()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Method1();
}

class C : {|CS0535:I|}
{
    I i;
}",
@"interface I
{
    void Method1();
}

class C : I
{
    I i;

    public void Method1()
    {
        i.Method1();
    }
}",
codeAction: ("False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;i", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMember_FixAll_SameMemberInDifferentType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Method1();
}

class C : {|CS0535:I|}
{
    I i;
}

class D : {|CS0535:I|}
{
    I i;
}",
@"interface I
{
    void Method1();
}

class C : I
{
    I i;

    public void Method1()
    {
        i.Method1();
    }
}

class D : I
{
    I i;

    public void Method1()
    {
        i.Method1();
    }
}",
codeAction: ("False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;i", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMember_FixAll_FieldInOnePropInAnother()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Method1();
}

class C : {|CS0535:I|}
{
    I i;
}

class D : {|CS0535:I|}
{
    I i { get; }
}",
@"interface I
{
    void Method1();
}

class C : I
{
    I i;

    public void Method1()
    {
        i.Method1();
    }
}

class D : I
{
    I i { get; }

    public void Method1()
    {
        i.Method1();
    }
}",
codeAction: ("False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;i", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMember_FixAll_FieldInOneNonViableInAnother()
        {
            var test = new VerifyCS.Test
            {
                TestCode = @"interface I
{
    void Method1();
}

class C : {|CS0535:I|}
{
    I i;
}

class D : {|CS0535:I|}
{
    int i;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    void Method1();
}

class C : I
{
    I i;

    public void Method1()
    {
        i.Method1();
    }
}

class D : {|CS0535:I|}
{
    int i;
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;i",
                CodeActionIndex = 1,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMemberInterfaceWithIndexer()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    int this[int x] { get; set; }
}

class Goo : {|CS0535:IGoo|}
{
    IGoo f;
}",
@"interface IGoo
{
    int this[int x] { get; set; }
}

class Goo : IGoo
{
    IGoo f;

    public int this[int x]
    {
        get
        {
            return f[x];
        }

        set
        {
            f[x] = value;
        }
    }
}",
codeAction: ("False;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;f", 1));
        }

        [WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMemberRemoveUnnecessaryCast()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections;

sealed class X : {|CS0535:IComparer|}
{
    X x;
}",
@"using System.Collections;

sealed class X : IComparer
{
    X x;

    public int Compare(object x, object y)
    {
        return ((IComparer)this.x).Compare(x, y);
    }
}",
codeAction: ("False;False;False:global::System.Collections.IComparer;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;x", 1));
        }

        [WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementThroughFieldMemberRemoveUnnecessaryCastAndThis()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections;

sealed class X : {|CS0535:IComparer|}
{
    X a;
}",
@"using System.Collections;

sealed class X : IComparer
{
    X a;

    public int Compare(object x, object y)
    {
        return ((IComparer)a).Compare(x, y);
    }
}",
codeAction: ("False;False;False:global::System.Collections.IComparer;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementAbstract()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Method1();
}

abstract class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Method1();
}

abstract class C : I
{
    public abstract void Method1();
}",
codeAction: ("False;True;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceWithRefOutParameters()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class C : {|CS0535:{|CS0535:I|}|}
{
    I goo;
}

interface I
{
    void Method1(ref int x, out int y, int z);
    int Method2();
}",
@"class C : I
{
    I goo;

    public void Method1(ref int x, out int y, int z)
    {
        goo.Method1(ref x, out y, z);
    }

    public int Method2()
    {
        return goo.Method2();
    }
}

interface I
{
    void Method1(ref int x, out int y, int z);
    int Method2();
}",
codeAction: ("False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;goo", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConflictingMethods1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class B
{
    public int Method1()
    {
        return 0;
    }
}

class C : B, {|CS0738:I|}
{
}

interface I
{
    void Method1();
}",
@"class B
{
    public int Method1()
    {
        return 0;
    }
}

class C : B, I
{
    void I.Method1()
    {
        throw new System.NotImplementedException();
    }
}

interface I
{
    void Method1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConflictingProperties()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class Test : {|CS0737:I1|}
{
    int Prop { get; set; }
}

interface I1
{
    int Prop { get; set; }
}",
@"class Test : I1
{
    int Prop { get; set; }

    int I1.Prop
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}

interface I1
{
    int Prop { get; set; }
}");
        }

        [WorkItem(539043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539043")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExplicitProperties()
        {
            var code =
@"interface I2
{
    decimal Calc { get; }
}

class C : I2
{
    protected decimal pay;

    decimal I2.Calc
    {
        get
        {
            return pay;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedMethodName()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    void @M();
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    void @M();
}

class Class : IInterface
{
    public void M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedMethodKeyword()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    void @int();
}

class Class : {|CS0535:IInterface|}
{
}",
@"interface IInterface
{
    void @int();
}

class Class : IInterface
{
    public void @int()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedInterfaceName1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface @IInterface
{
    void M();
}

class Class : {|CS0737:@IInterface|}
{
    string M() => """";
}",
@"interface @IInterface
{
    void M();
}

class Class : @IInterface
{
    string M() => """";

    void IInterface.M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedInterfaceName2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface @IInterface
{
    void @M();
}

class Class : {|CS0737:@IInterface|}
{
    string M() => """";
}",
@"interface @IInterface
{
    void @M();
}

class Class : @IInterface
{
    string M() => """";

    void IInterface.M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedInterfaceKeyword1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface @int
{
    void M();
}

class Class : {|CS0737:@int|}
{
    string M() => """";
}",
@"interface @int
{
    void M();
}

class Class : @int
{
    string M() => """";

    void @int.M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEscapedInterfaceKeyword2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface @int
{
    void @bool();
}

class Class : {|CS0737:@int|}
{
    string @bool() => """";
}",
@"interface @int
{
    void @bool();
}

class Class : @int
{
    string @bool() => """";

    void @int.@bool()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestPropertyFormatting()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface DD
{
    int Prop { get; set; }
}
public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; set; }
}
public class A : DD
{
    public int Prop
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestProperty_PropertyCodeStyleOn1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int Prop { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; }
}

public class A : DD
{
    public int Prop => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestProperty_AccessorCodeStyleOn1()
        {
            await TestWithAccessorCodeStyleOptionsOnAsync(
@"public interface DD
{
    int Prop { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; }
}

public class A : DD
{
    public int Prop { get => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexer_IndexerCodeStyleOn1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; }
}

public class A : DD
{
    public int this[int i] => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexer_AccessorCodeStyleOn1()
        {
            await TestWithAccessorCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; }
}

public class A : DD
{
    public int this[int i] { get => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMethod_AllCodeStyleOn1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int M();
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int M();
}

public class A : DD
{
    public int M() => throw new System.NotImplementedException();
}");
        }

        [WorkItem(539522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestReadonlyPropertyExpressionBodyYes1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int Prop { get; }
}
public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; }
}
public class A : DD
{
    public int Prop => throw new System.NotImplementedException();
}");
        }

        [WorkItem(539522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestReadonlyPropertyAccessorBodyYes1()
        {
            await TestWithAccessorCodeStyleOptionsOnAsync(
@"public interface DD
{
    int Prop { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; }
}

public class A : DD
{
    public int Prop { get => throw new System.NotImplementedException(); }
}");
        }

        [WorkItem(539522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestReadonlyPropertyAccessorBodyYes2()
        {
            await TestWithAccessorCodeStyleOptionsOnAsync(
@"public interface DD
{
    int Prop { get; set; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; set; }
}

public class A : DD
{
    public int Prop { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}");
        }

        [WorkItem(539522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestReadonlyPropertyExpressionBodyNo1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface DD
{
    int Prop { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int Prop { get; }
}

public class A : DD
{
    public int Prop
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexerExpressionBodyYes1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; }
}

public class A : DD
{
    public int this[int i] => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexerExpressionBodyNo1()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; set; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; set; }
}

public class A : DD
{
    public int this[int i] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexerAccessorExpressionBodyYes1()
        {
            await TestWithAccessorCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; }
}

public class A : DD
{
    public int this[int i] { get => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexerAccessorExpressionBodyYes2()
        {
            await TestWithAllCodeStyleOptionsOnAsync(
@"public interface DD
{
    int this[int i] { get; set; }
}

public class A : {|CS0535:DD|}
{
}",
@"public interface DD
{
    int this[int i] { get; set; }
}

public class A : DD
{
    public int this[int i] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCommentPlacement()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface DD
{
    void Goo();
}
public class A : {|CS0535:DD|}
{
    //comments
}",
@"public interface DD
{
    void Goo();
}
public class A : DD
{
    //comments
    public void Goo()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(539991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539991")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestBracePlacement()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|}",
@"using System;
class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(540318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540318")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMissingWithIncompleteMember()
        {
            var code =
@"interface ITest
{
    void Method();
}

class Test : ITest
{
    p {|CS1585:public|} void Method()
    {
        throw new System.NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(541380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541380")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExplicitProperty()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface i1
{
    int p { get; set; }
}

class c1 : {|CS0535:i1|}
{
}",
@"interface i1
{
    int p { get; set; }
}

class c1 : i1
{
    int i1.p
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}",
codeAction: ("True;False;False:global::i1;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(541981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoDelegateThroughField1()
        {
            var code =
@"interface I
{
    void Method1();
}

class C : {|CS0535:I|}
{
    I i { get; set; }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = @"interface I
{
    void Method1();
}

class C : I
{
    I i { get; set; }

    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                CodeActionEquivalenceKey = "False;False;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = @"interface I
{
    void Method1();
}

class C : I
{
    I i { get; set; }

    public void Method1()
    {
        i.Method1();
    }
}",
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;i",
                CodeActionIndex = 1,
            }.RunAsync();

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = @"interface I
{
    void Method1();
}

class C : I
{
    I i { get; set; }

    void I.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                CodeActionEquivalenceKey = "True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 2,
            }.RunAsync();
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIReadOnlyListThroughField()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections.Generic;

class A : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IReadOnlyList<int>|}|}|}|}
{
    int[] field;
}",
@"using System.Collections;
using System.Collections.Generic;

class A : IReadOnlyList<int>
{
    int[] field;

    public int this[int index]
    {
        get
        {
            return ((IReadOnlyList<int>)field)[index];
        }
    }

    public int Count
    {
        get
        {
            return ((IReadOnlyCollection<int>)field).Count;
        }
    }

    public IEnumerator<int> GetEnumerator()
    {
        return ((IEnumerable<int>)field).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return field.GetEnumerator();
    }
}",
codeAction: ("False;False;False:global::System.Collections.Generic.IReadOnlyList<int>;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;field", 1));
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIReadOnlyListThroughProperty()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections.Generic;

class A : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IReadOnlyList<int>|}|}|}|}
{
    int[] field { get; set; }
}",
@"using System.Collections;
using System.Collections.Generic;

class A : IReadOnlyList<int>
{
    public int this[int index]
    {
        get
        {
            return ((IReadOnlyList<int>)field)[index];
        }
    }

    public int Count
    {
        get
        {
            return ((IReadOnlyCollection<int>)field).Count;
        }
    }

    int[] field { get; set; }

    public IEnumerator<int> GetEnumerator()
    {
        return ((IEnumerable<int>)field).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return field.GetEnumerator();
    }
}",
codeAction: ("False;False;False:global::System.Collections.Generic.IReadOnlyList<int>;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;field", 1));
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughField()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A a;
}",
@"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I
{
    A a;

    public int M()
    {
        return ((I)a).M();
    }
}",
codeAction: ("False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a", 1));
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughField_FieldImplementsMultipleInterfaces()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I, I2
{
    int I.M()
    {
        return 0;
    }

    int I2.M2()
    {
        return 0;
    }
}

class B : {|CS0535:I|}, {|CS0535:I2|}
{
    A a;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I, I2
{
    int I.M()
    {
        return 0;
    }

    int I2.M2()
    {
        return 0;
    }
}

class B : I, {|CS0535:I2|}
{
    A a;

    public int M()
    {
        return ((I)a).M();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                DiagnosticSelector = diagnostics => diagnostics[0],
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a",
                CodeActionIndex = 1,
            }.RunAsync();

            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I, I2
{
    int I.M()
    {
        return 0;
    }

    int I2.M2()
    {
        return 0;
    }
}

class B : {|CS0535:I|}, {|CS0535:I2|}
{
    A a;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I, I2
{
    int I.M()
    {
        return 0;
    }

    int I2.M2()
    {
        return 0;
    }
}

class B : {|CS0535:I|}, I2
{
    A a;

    public int M2()
    {
        return ((I2)a).M2();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                DiagnosticSelector = diagnostics => diagnostics[1],
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "False;False;False:global::I2;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughField_MultipleFieldsCanImplementInterface()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A a;
    A aa;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I
{
    A a;
    A aa;

    public int M()
    {
        return ((I)a).M();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(4, codeActions.Length),
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a",
                CodeActionIndex = 1,
            }.RunAsync();

            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A a;
    A aa;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I
{
    A a;
    A aa;

    public int M()
    {
        return ((I)aa).M();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(4, codeActions.Length),
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;aa",
                CodeActionIndex = 2,
            }.RunAsync();
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughField_MultipleFieldsForMultipleInterfaces()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I2
{
    int I2.M2()
    {
        return 0;
    }
}

class C : {|CS0535:I|}, {|CS0535:I2|}
{
    A a;
    B b;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I2
{
    int I2.M2()
    {
        return 0;
    }
}

class C : I, {|CS0535:I2|}
{
    A a;
    B b;

    public int M()
    {
        return ((I)a).M();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                DiagnosticSelector = diagnostics => diagnostics[0],
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "False;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;a",
                CodeActionIndex = 1,
            }.RunAsync();

            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I2
{
    int I2.M2()
    {
        return 0;
    }
}

class C : {|CS0535:I|}, {|CS0535:I2|}
{
    A a;
    B b;
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface I
{
    int M();
}

interface I2
{
    int M2();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I2
{
    int I2.M2()
    {
        return 0;
    }
}

class C : {|CS0535:I|}, I2
{
    A a;
    B b;

    public int M2()
    {
        return ((I2)b).M2();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                DiagnosticSelector = diagnostics => diagnostics[1],
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "False;False;False:global::I2;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;b",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(18556, "https://github.com/dotnet/roslyn/issues/18556")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughExplicitProperty()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface IA
{
    IB B { get; }
}
interface IB
{
    int M();
}
class AB : IA, {|CS0535:IB|}
{
    IB IA.B => null;
}",
                FixedCode = @"interface IA
{
    IB B { get; }
}
interface IB
{
    int M();
}
class AB : IA, IB
{
    IB IA.B => null;

    public int M()
    {
        return ((IA)this).B.M();
    }
}",
                Options = { AllOptionsOff },
                CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
                CodeActionEquivalenceKey = "False;False;False:global::IB;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;IA.B",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoImplementThroughIndexer()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A this[int index]
    {
        get
        {
            return null;
        }
    }
}",
                FixedCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : I
{
    A this[int index]
    {
        get
        {
            return null;
        }
    }

    public int M()
    {
        throw new System.NotImplementedException();
    }
}",
                CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
            }.RunAsync();
        }

        [WorkItem(768799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoImplementThroughWriteOnlyProperty()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A a
    {
        set
        {
        }
    }
}",
                FixedCode = @"interface I
{
    int M();
}

class A : I
{
    int I.M()
    {
        return 0;
    }
}

class B : {|CS0535:I|}
{
    A a
    {
        set
        {
        }
    }

    public int M()
    {
        throw new System.NotImplementedException();
    }
}",
                CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementEventThroughMember()
        {
            await TestInRegularAndScriptAsync(@"
interface IGoo
{
    event System.EventHandler E;
}

class CanGoo : IGoo
{
    public event System.EventHandler E;
}

class HasCanGoo : {|CS0535:IGoo|}
{
    CanGoo canGoo;
}",
@"
using System;

interface IGoo
{
    event System.EventHandler E;
}

class CanGoo : IGoo
{
    public event System.EventHandler E;
}

class HasCanGoo : IGoo
{
    CanGoo canGoo;

    public event EventHandler E
    {
        add
        {
            ((IGoo)canGoo).E += value;
        }

        remove
        {
            ((IGoo)canGoo).E -= value;
        }
    }
}", codeAction: ("False;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;canGoo", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementEventThroughExplicitMember()
        {
            await TestInRegularAndScriptAsync(
@"interface IGoo { event System . EventHandler E ; } class CanGoo : IGoo { event System.EventHandler IGoo.E { add { } remove { } } } class HasCanGoo : {|CS0535:IGoo|} { CanGoo canGoo; } ",
@"using System;

interface IGoo { event System . EventHandler E ; } class CanGoo : IGoo { event System.EventHandler IGoo.E { add { } remove { } } } class HasCanGoo : IGoo { CanGoo canGoo;

    public event EventHandler E
    {
        add
        {
            ((IGoo)canGoo).E += value;
        }

        remove
        {
            ((IGoo)canGoo).E -= value;
        }
    }
} ",
codeAction: ("False;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;canGoo", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementEvent()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : {|CS0535:IGoo|}
{
}",
@"using System;

interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : IGoo
{
    public event EventHandler E;
}",
codeAction: ("False;False;True:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementEventAbstractly()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : {|CS0535:IGoo|}
{
}",
@"using System;

interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : IGoo
{
    public abstract event EventHandler E;
}",
codeAction: ("False;True;True:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementEventExplicitly()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : {|CS0535:IGoo|}
{
}",
@"using System;

interface IGoo
{
    event System.EventHandler E;
}

abstract class Goo : IGoo
{
    event EventHandler IGoo.E
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }
}",
codeAction: ("True;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 2));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestFaultToleranceInStaticMembers_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IFoo
{
    static string Name { set; get; }

    static int {|CS0501:Foo|}(string s);
}

class Program : IFoo
{
}",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestFaultToleranceInStaticMembers_02()
        {
            var test = new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IFoo
{
    string Name { set; get; }

    static int {|CS0501:Foo|}(string s);
}

class Program : {|CS0535:IFoo|}
{
}",
                FixedCode = @"interface IFoo
{
    string Name { set; get; }

    static int {|CS0501:Foo|}(string s);
}

class Program : IFoo
{
    public string Name
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}",
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestFaultToleranceInStaticMembers_03()
        {
            var test = new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IGoo
{
    static string Name { set; get; }

    int Goo(string s);
}

class Program : {|CS0535:IGoo|}
{
}",
                FixedCode = @"interface IGoo
{
    static string Name { set; get; }

    int Goo(string s);
}

class Program : IGoo
{
    public int Goo(string s)
    {
        throw new System.NotImplementedException();
    }
}",
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexers()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface ISomeInterface
{
    int this[int index] { get; set; }
}

class IndexerClass : {|CS0535:ISomeInterface|}
{
}",
@"public interface ISomeInterface
{
    int this[int index] { get; set; }
}

class IndexerClass : ISomeInterface
{
    public int this[int index]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexersExplicit()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface ISomeInterface
{
    int this[int index] { get; set; }
}

class IndexerClass : {|CS0535:ISomeInterface|}
{
}",
@"public interface ISomeInterface
{
    int this[int index] { get; set; }
}

class IndexerClass : ISomeInterface
{
    int ISomeInterface.this[int index]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}",
codeAction: ("True;False;False:global::ISomeInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexersWithASingleAccessor()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface ISomeInterface
{
    int this[int index] { get; }
}

class IndexerClass : {|CS0535:ISomeInterface|}
{
}",
@"public interface ISomeInterface
{
    int this[int index] { get; }
}

class IndexerClass : ISomeInterface
{
    public int this[int index]
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(542357, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConstraints1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo<T>() where T : class;
}

class A : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo<T>() where T : class;
}

class A : I
{
    public void Goo<T>() where T : class
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(542357, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConstraintsExplicit()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo<T>() where T : class;
}

class A : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo<T>() where T : class;
}

class A : I
{
    void I.Goo<T>()
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(542357, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUsingAddedForConstraint()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo<T>() where T : System.Attribute;
}

class A : {|CS0535:I|}
{
}",
@"using System;

interface I
{
    void Goo<T>() where T : System.Attribute;
}

class A : I
{
    public void Goo<T>() where T : Attribute
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542379")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIndexer()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    int this[int x] { get; set; }
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    int this[int x] { get; set; }
}

class C : I
{
    public int this[int x]
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(542588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542588")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRecursiveConstraint1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void Goo<T>() where T : IComparable<T>;
}

class C : {|CS0535:I|}
{
}",
@"using System;

interface I
{
    void Goo<T>() where T : IComparable<T>;
}

class C : I
{
    public void Goo<T>() where T : IComparable<T>
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542588")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRecursiveConstraint2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void Goo<T>() where T : IComparable<T>;
}

class C : {|CS0535:I|}
{
}",
@"using System;

interface I
{
    void Goo<T>() where T : IComparable<T>;
}

class C : I
{
    void I.Goo<T>()
    {
        throw new NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<string>|}
{
}",
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<string>
{
    void I<string>.Goo<T>()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<object>|}
{
}",
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<object>
{
    public void Goo<T>() where T : class
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint3()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<object>|}
{
}",
@"interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<object>
{
    void I<object>.Goo<T>()
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I<object>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint4()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<Delegate>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<Delegate>
{
    void I<Delegate>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint5()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<MulticastDelegate>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<MulticastDelegate>
{
    void I<MulticastDelegate>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint6()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

delegate void Bar();

class A : {|CS0535:I<Bar>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

delegate void Bar();

class A : I<Bar>
{
    void I<Bar>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint7()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<Enum>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<Enum>
{
    void I<Enum>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint8()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : {|CS0535:I<int[]>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class A : I<int[]>
{
    void I<int[]>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint9()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

enum E
{
}

class A : {|CS0535:I<E>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

enum E
{
}

class A : I<E>
{
    void I<E>.Goo<{|CS0455:T|}>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnexpressibleConstraint10()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : S;
}

class A : {|CS0535:I<ValueType>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : S;
}

class A : I<ValueType>
{
    void I<ValueType>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542669")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestArrayConstraint()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : S;
}

class C : {|CS0535:I<Array>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : S;
}

class C : I<Array>
{
    void I<Array>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542743")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMultipleClassConstraints()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : Exception, S;
}

class C : {|CS0535:I<Attribute>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : Exception, S;
}

class C : I<Attribute>
{
    void I<Attribute>.Goo<{|CS0455:T|}>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542751")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestClassConstraintAndRefConstraint()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class C : {|CS0535:I<Exception>|}
{
}",
@"using System;

interface I<S>
{
    void Goo<T>() where T : class, S;
}

class C : I<Exception>
{
    void I<Exception>.Goo<T>()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRenameConflictingTypeParameters1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Collections.Generic;

interface I<T>
{
    void Goo<S>(T x, IList<S> list) where S : T;
}

class A<S> : {|CS0535:I<S>|}
{
}",
@"using System;
using System.Collections.Generic;

interface I<T>
{
    void Goo<S>(T x, IList<S> list) where S : T;
}

class A<S> : I<S>
{
    public void Goo<S1>(S x, IList<S1> list) where S1 : S
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRenameConflictingTypeParameters2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Collections.Generic;

interface I<T>
{
    void Goo<S>(T x, IList<S> list) where S : T;
}

class A<S> : {|CS0535:I<S>|}
{
}",
@"using System;
using System.Collections.Generic;

interface I<T>
{
    void Goo<S>(T x, IList<S> list) where S : T;
}

class A<S> : I<S>
{
    void I<S>.Goo<S1>(S x, IList<S1> list)
    {
        throw new NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I<S>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(542505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRenameConflictingTypeParameters3()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Collections.Generic;

interface I<X, Y>
{
    void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
        where A : IList<B>
        where B : IList<A>;
}

class C<A, B> : {|CS0535:I<A, B>|}
{
}",
@"using System;
using System.Collections.Generic;

interface I<X, Y>
{
    void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
        where A : IList<B>
        where B : IList<A>;
}

class C<A, B> : I<A, B>
{
    public void Goo<A1, B1>(A x, B y, IList<A1> list1, IList<B1> list2)
        where A1 : IList<B1>
        where B1 : IList<A1>
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(542505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRenameConflictingTypeParameters4()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Collections.Generic;

interface I<X, Y>
{
    void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
        where A : IList<B>
        where B : IList<A>;
}

class C<A, B> : {|CS0535:I<A, B>|}
{
}",
@"using System;
using System.Collections.Generic;

interface I<X, Y>
{
    void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
        where A : IList<B>
        where B : IList<A>;
}

class C<A, B> : I<A, B>
{
    void I<A, B>.Goo<A1, B1>(A x, B y, IList<A1> list1, IList<B1> list2)
    {
        throw new NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I<A, B>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(542506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNameSimplification()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B x);
    }

    class C<U> : {|CS0535:I|}
    {
    }
}",
@"using System;

class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B x);
    }

    class C<U> : I
    {
        public void Goo(B x)
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WorkItem(542506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNameSimplification2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B[] x);
    }

    class C<U> : {|CS0535:I|}
    {
    }
}",
@"class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B[] x);
    }

    class C<U> : I
    {
        public void Goo(B[] x)
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(542506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNameSimplification3()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B[][,][,,][,,,] x);
    }

    class C<U> : {|CS0535:I|}
    {
    }
}",
@"class A<T>
{
    class B
    {
    }

    interface I
    {
        void Goo(B[][,][,,][,,,] x);
    }

    class C<U> : I
    {
        public void Goo(B[][,][,,][,,,] x)
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(544166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544166")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementAbstractProperty()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    int Gibberish { get; set; }
}

abstract class Goo : {|CS0535:IGoo|}
{
}",
@"interface IGoo
{
    int Gibberish { get; set; }
}

abstract class Goo : IGoo
{
    public abstract int Gibberish { get; set; }
}",
codeAction: ("False;True;True:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(544210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544210")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMissingOnWrongArity()
        {
            var code =
@"interface I1<T>
{
    int X { get; set; }
}

class C : {|CS0305:I1|}
{
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(544281, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544281")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplicitDefaultValue()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IOptional
{
    int Goo(int g = 0);
}

class Opt : {|CS0535:IOptional|}
{
}",
@"interface IOptional
{
    int Goo(int g = 0);
}

class Opt : IOptional
{
    public int Goo(int g = 0)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(544281, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544281")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExplicitDefaultValue()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IOptional
{
    int Goo(int g = 0);
}

class Opt : {|CS0535:IOptional|}
{
}",
@"interface IOptional
{
    int Goo(int g = 0);
}

class Opt : IOptional
{
    int IOptional.Goo(int g)
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::IOptional;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMissingInHiddenType()
        {
            var code =
@"using System;

class Program : {|CS0535:IComparable|}
{
#line hidden
}
#line default";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestGenerateIntoVisiblePart()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"#line default
using System;

partial class Program : {|CS0535:IComparable|}
{
    void Goo()
    {
#line hidden
    }
}
#line default",
@"#line default
using System;

partial class Program : IComparable
{
    public int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }

    void Goo()
    {
#line hidden
    }
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestGenerateIfAvailableRegionExists()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

partial class Program : {|CS0535:IComparable|}
{
#line hidden
}
#line default

partial class Program
{
}",
@"using System;

partial class Program : IComparable
{
#line hidden
}
#line default

partial class Program
{
    public int CompareTo(object obj)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545334")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoGenerateInVenusCase1()
        {
            var code =
@"using System;
#line 1 ""Bar""
class Goo : {|CS0535:IComparable|}{|CS1513:|}{|CS1514:|}


#line default
#line hidden
// stuff";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(545476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545476")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalDateTime1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo([Optional][DateTimeConstant(100)] DateTime x);
}

public class C : {|CS0535:IGoo|}
{
}",
                FixedCode = @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo([Optional][DateTimeConstant(100)] DateTime x);
}

public class C : IGoo
{
    public void Goo([DateTimeConstant(100), Optional] DateTime x)
    {
        throw new NotImplementedException();
    }
}",
                Options = { AllOptionsOff },

                // 🐛 one value is generated with 0L instead of 0
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [WorkItem(545476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545476")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalDateTime2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo([Optional][DateTimeConstant(100)] DateTime x);
}

public class C : {|CS0535:IGoo|}
{
}",
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo([Optional][DateTimeConstant(100)] DateTime x);
}

public class C : IGoo
{
    void IGoo.Goo(DateTime x)
    {
        throw new NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(545477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545477")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIUnknownIDispatchAttributes1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo1([Optional][IUnknownConstant] object x);
    void Goo2([Optional][IDispatchConstant] object x);
}

public class C : {|CS0535:{|CS0535:IGoo|}|}
{
}",
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo1([Optional][IUnknownConstant] object x);
    void Goo2([Optional][IDispatchConstant] object x);
}

public class C : IGoo
{
    public void Goo1([IUnknownConstant, Optional] object x)
    {
        throw new System.NotImplementedException();
    }

    public void Goo2([IDispatchConstant, Optional] object x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545477")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIUnknownIDispatchAttributes2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo1([Optional][IUnknownConstant] object x);
    void Goo2([Optional][IDispatchConstant] object x);
}

public class C : {|CS0535:{|CS0535:IGoo|}|}
{
}",
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface IGoo
{
    void Goo1([Optional][IUnknownConstant] object x);
    void Goo2([Optional][IDispatchConstant] object x);
}

public class C : IGoo
{
    void IGoo.Goo1(object x)
    {
        throw new System.NotImplementedException();
    }

    void IGoo.Goo2(object x)
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::IGoo;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(545464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545464")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestTypeNameConflict()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo
{
    void Goo();
}

public class Goo : {|CS0535:IGoo|}
{
}",
@"interface IGoo
{
    void Goo();
}

public class Goo : IGoo
{
    void IGoo.Goo()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStringLiteral()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo { void Goo ( string s = ""\"""" ) ; } class B : {|CS0535:IGoo|} { } ",
@"interface IGoo { void Goo ( string s = ""\"""" ) ; }
class B : IGoo
{
    public void Goo(string s = ""\"""")
    {
        throw new System.NotImplementedException();
    }
} ");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalNullableStructParameter1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"struct b
{
}

interface d
{
    void m(b? x = null, b? y = default(b?));
}

class c : {|CS0535:d|}
{
}",
@"struct b
{
}

interface d
{
    void m(b? x = null, b? y = default(b?));
}

class c : d
{
    public void m(b? x = null, b? y = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalNullableStructParameter2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"struct b
{
}

interface d
{
    void m(b? x = null, b? y = default(b?));
}

class c : {|CS0535:d|}
{
}",
@"struct b
{
}

interface d
{
    void m(b? x = null, b? y = default(b?));
}

class c : d
{
    void d.m(b? x, b? y)
    {
        throw new System.NotImplementedException();
    }
}", codeAction: ("True;False;False:global::d;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(916114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalNullableIntParameter()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface d
{
    void m(int? x = 5, int? y = null);
}

class c : {|CS0535:d|}
{
}",
@"interface d
{
    void m(int? x = 5, int? y = null);
}

class c : d
{
    public void m(int? x = 5, int? y = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalWithNoDefaultValue()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional] I o);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional] I o);
}

class C : I
{
    public void Goo([Optional] I o)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestIntegralAndFloatLiterals()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface I
{
    void M01(short s = short.MinValue);
    void M02(short s = -1);
    void M03(short s = short.MaxValue);
    void M04(ushort s = ushort.MinValue);
    void M05(ushort s = 1);
    void M06(ushort s = ushort.MaxValue);
    void M07(int s = int.MinValue);
    void M08(int s = -1);
    void M09(int s = int.MaxValue);
    void M10(uint s = uint.MinValue);
    void M11(uint s = 1);
    void M12(uint s = uint.MaxValue);
    void M13(long s = long.MinValue);
    void M14(long s = -1);
    void M15(long s = long.MaxValue);
    void M16(ulong s = ulong.MinValue);
    void M17(ulong s = 1);
    void M18(ulong s = ulong.MaxValue);
    void M19(float s = float.MinValue);
    void M20(float s = 1);
    void M21(float s = float.MaxValue);
    void M22(double s = double.MinValue);
    void M23(double s = 1);
    void M24(double s = double.MaxValue);
}

class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}
{
}",
                FixedCode = @"interface I
{
    void M01(short s = short.MinValue);
    void M02(short s = -1);
    void M03(short s = short.MaxValue);
    void M04(ushort s = ushort.MinValue);
    void M05(ushort s = 1);
    void M06(ushort s = ushort.MaxValue);
    void M07(int s = int.MinValue);
    void M08(int s = -1);
    void M09(int s = int.MaxValue);
    void M10(uint s = uint.MinValue);
    void M11(uint s = 1);
    void M12(uint s = uint.MaxValue);
    void M13(long s = long.MinValue);
    void M14(long s = -1);
    void M15(long s = long.MaxValue);
    void M16(ulong s = ulong.MinValue);
    void M17(ulong s = 1);
    void M18(ulong s = ulong.MaxValue);
    void M19(float s = float.MinValue);
    void M20(float s = 1);
    void M21(float s = float.MaxValue);
    void M22(double s = double.MinValue);
    void M23(double s = 1);
    void M24(double s = double.MaxValue);
}

class C : I
{
    public void M01(short s = short.MinValue)
    {
        throw new System.NotImplementedException();
    }

    public void M02(short s = -1)
    {
        throw new System.NotImplementedException();
    }

    public void M03(short s = short.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M04(ushort s = 0)
    {
        throw new System.NotImplementedException();
    }

    public void M05(ushort s = 1)
    {
        throw new System.NotImplementedException();
    }

    public void M06(ushort s = ushort.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M07(int s = int.MinValue)
    {
        throw new System.NotImplementedException();
    }

    public void M08(int s = -1)
    {
        throw new System.NotImplementedException();
    }

    public void M09(int s = int.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M10(uint s = 0)
    {
        throw new System.NotImplementedException();
    }

    public void M11(uint s = 1)
    {
        throw new System.NotImplementedException();
    }

    public void M12(uint s = uint.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M13(long s = long.MinValue)
    {
        throw new System.NotImplementedException();
    }

    public void M14(long s = -1)
    {
        throw new System.NotImplementedException();
    }

    public void M15(long s = long.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M16(ulong s = 0)
    {
        throw new System.NotImplementedException();
    }

    public void M17(ulong s = 1)
    {
        throw new System.NotImplementedException();
    }

    public void M18(ulong s = ulong.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M19(float s = float.MinValue)
    {
        throw new System.NotImplementedException();
    }

    public void M20(float s = 1)
    {
        throw new System.NotImplementedException();
    }

    public void M21(float s = float.MaxValue)
    {
        throw new System.NotImplementedException();
    }

    public void M22(double s = double.MinValue)
    {
        throw new System.NotImplementedException();
    }

    public void M23(double s = 1)
    {
        throw new System.NotImplementedException();
    }

    public void M24(double s = double.MaxValue)
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },

                // 🐛 one value is generated with 0U instead of 0
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEnumLiterals()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

enum E
{
   A = 1,
   B = 2  
}

[FlagsAttribute]
enum FlagE
{
   A = 1,
   B = 2
}

interface I
{
    void M1(E e = E.A | E.B);
    void M2(FlagE e = FlagE.A | FlagE.B);
}

class C : {|CS0535:{|CS0535:I|}|}
{
}",
@"using System;

enum E
{
   A = 1,
   B = 2  
}

[FlagsAttribute]
enum FlagE
{
   A = 1,
   B = 2
}

interface I
{
    void M1(E e = E.A | E.B);
    void M2(FlagE e = FlagE.A | FlagE.B);
}

class C : I
{
    public void M1(E e = (E)3)
    {
        throw new NotImplementedException();
    }

    public void M2(FlagE e = FlagE.A | FlagE.B)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCharLiterals()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void M01(char c = '\0');
    void M02(char c = '\r');
    void M03(char c = '\n');
    void M04(char c = '\t');
    void M05(char c = '\b');
    void M06(char c = '\v');
    void M07(char c = '\'');
    void M08(char c = '“');
    void M09(char c = 'a');
    void M10(char c = '""');
    void M11(char c = '\u2029');
}

class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}|}|}|}|}|}|}|}
{
}",
@"using System;

interface I
{
    void M01(char c = '\0');
    void M02(char c = '\r');
    void M03(char c = '\n');
    void M04(char c = '\t');
    void M05(char c = '\b');
    void M06(char c = '\v');
    void M07(char c = '\'');
    void M08(char c = '“');
    void M09(char c = 'a');
    void M10(char c = '""');
    void M11(char c = '\u2029');
}

class C : I
{
    public void M01(char c = '\0')
    {
        throw new NotImplementedException();
    }

    public void M02(char c = '\r')
    {
        throw new NotImplementedException();
    }

    public void M03(char c = '\n')
    {
        throw new NotImplementedException();
    }

    public void M04(char c = '\t')
    {
        throw new NotImplementedException();
    }

    public void M05(char c = '\b')
    {
        throw new NotImplementedException();
    }

    public void M06(char c = '\v')
    {
        throw new NotImplementedException();
    }

    public void M07(char c = '\'')
    {
        throw new NotImplementedException();
    }

    public void M08(char c = '“')
    {
        throw new NotImplementedException();
    }

    public void M09(char c = 'a')
    {
        throw new NotImplementedException();
    }

    public void M10(char c = '""')
    {
        throw new NotImplementedException();
    }

    public void M11(char c = '\u2029')
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545695")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRemoveParenthesesAroundTypeReference1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void Goo(DayOfWeek x = DayOfWeek.Friday);
}

class C : {|CS0535:I|}
{
    DayOfWeek DayOfWeek { get; set; }
}",
@"using System;

interface I
{
    void Goo(DayOfWeek x = DayOfWeek.Friday);
}

class C : I
{
    DayOfWeek DayOfWeek { get; set; }

    public void Goo(DayOfWeek x = DayOfWeek.Friday)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545696, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545696")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDecimalConstants1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo(decimal x = decimal.MaxValue);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo(decimal x = decimal.MaxValue);
}

class C : I
{
    public void Goo(decimal x = decimal.MaxValue)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545711")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNullablePrimitiveLiteral()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo(decimal? x = decimal.MaxValue);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo(decimal? x = decimal.MaxValue);
}

class C : I
{
    public void Goo(decimal? x = decimal.MaxValue)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545715")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNullableEnumType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void Goo(DayOfWeek? x = DayOfWeek.Friday);
}

class C : {|CS0535:I|}
{
}",
@"using System;

interface I
{
    void Goo(DayOfWeek? x = DayOfWeek.Friday);
}

class C : I
{
    public void Goo(DayOfWeek? x = DayOfWeek.Friday)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestByteLiterals()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo(byte x = 1);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo(byte x = 1);
}

class C : I
{
    public void Goo(byte x = 1)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545736, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545736")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCastedOptionalParameter1()
        {
            const string code = @"
using System;
interface I
{
    void Goo(ConsoleColor x = (ConsoleColor)(-1));
}

class C : {|CS0535:I|}
{
}";

            const string expected = @"
using System;
interface I
{
    void Goo(ConsoleColor x = (ConsoleColor)(-1));
}

class C : I
{
    public void Goo(ConsoleColor x = (ConsoleColor)(-1))
    {
        throw new NotImplementedException();
    }
}";

            await TestWithAllCodeStyleOptionsOffAsync(code, expected);
        }

        [WorkItem(545737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545737")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCastedEnumValue()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface I
{
    void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue);
}

class C : {|CS0535:I|}
{
}",
@"using System;

interface I
{
    void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue);
}

class C : I
{
    public void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545785")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoCastFromZeroToEnum()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"enum E
{
    A = 1,
}

interface I
{
    void Goo(E x = 0);
}

class C : {|CS0535:I|}
{
}",
@"enum E
{
    A = 1,
}

interface I
{
    void Goo(E x = 0);
}

class C : I
{
    public void Goo(E x = 0)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545793")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMultiDimArray()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional][DefaultParameterValue(1)] int x, int[,] y);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional][DefaultParameterValue(1)] int x, int[,] y);
}

class C : I
{
    public void Goo([{|CS1745:DefaultParameterValue|}(1), {|CS1745:Optional|}] int x = {|CS8017:1|}, int[,] y = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545794")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestParametersAfterOptionalParameter()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional, DefaultParameterValue(1)] int x, int[] y, int[] z);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional, DefaultParameterValue(1)] int x, int[] y, int[] z);
}

class C : I
{
    public void Goo([{|CS1745:DefaultParameterValue|}(1), {|CS1745:Optional|}] int x = {|CS8017:1|}, int[] y = null, int[] z = null)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545605")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestAttributeInParameter()
        {
            var test = new VerifyCS.Test
            {
                TestCode =
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
}
class C : {|CS0535:I|}
{
}
",
                FixedCode =
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

interface I
{
    void Goo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
}
class C : I
{
    public void Goo([DateTimeConstant(100), Optional] DateTime d1, [IUnknownConstant, Optional] object d2)
    {
        throw new NotImplementedException();
    }
}
",
                // 🐛 the DateTimeConstant attribute is generated with 100L instead of 100
                CodeActionValidationMode = CodeActionValidationMode.None,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [WorkItem(545897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNameConflictBetweenMethodAndTypeParameter()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<S>
{
    void T1<T>(S x, T y);
}

class C<T> : {|CS0535:I<T>|}
{
}",
@"interface I<S>
{
    void T1<T>(S x, T y);
}

class C<T> : I<T>
{
    public void T1<T2>(T x, T2 y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545895")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestTypeParameterReplacementWithOuterType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections.Generic;

interface I<S>
{
    void Goo<T>(S y, List<T>.Enumerator x);
}

class D<T> : {|CS0535:I<T>|}
{
}",
@"using System.Collections.Generic;

interface I<S>
{
    void Goo<T>(S y, List<T>.Enumerator x);
}

class D<T> : I<T>
{
    public void Goo<T1>(T y, List<T1>.Enumerator x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545864, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545864")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestFloatConstant()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo(float x = 1E10F);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo(float x = 1E10F);
}

class C : I
{
    public void Goo(float x = 1E+10F)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(544640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestKeywordForTypeParameterName()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo<@class>();
}

class C : {|CS0535:I|}{|CS1513:|}{|CS1514:|}",
@"interface I
{
    void Goo<@class>();
}

class C : I
{
    public void Goo<@class>()
    {
        throw new System.NotImplementedException();
    }
}
");
        }

        [WorkItem(545922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545922")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExtremeDecimals()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo1(decimal x = 1E28M);
    void Goo2(decimal x = -1E28M);
}

class C : {|CS0535:{|CS0535:I|}|}
{
}",
@"interface I
{
    void Goo1(decimal x = 1E28M);
    void Goo2(decimal x = -1E28M);
}

class C : I
{
    public void Goo1(decimal x = 10000000000000000000000000000M)
    {
        throw new System.NotImplementedException();
    }

    public void Goo2(decimal x = -10000000000000000000000000000M)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(544659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544659")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonZeroScaleDecimals()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo(decimal x = 0.1M);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void Goo(decimal x = 0.1M);
}

class C : I
{
    public void Goo(decimal x = 0.1M)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(544639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544639")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnterminatedComment()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
 
// Implement interface
class C : {|CS0535:IServiceProvider|} {|CS1035:|}/*
{|CS1513:|}{|CS1514:|}",
@"using System;

// Implement interface
class C : IServiceProvider /*
*/
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(529920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529920")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNewLineBeforeDirective()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
 
// Implement interface
class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|}
#pragma warning disable
",
@"using System;

// Implement interface
class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
#pragma warning disable
");
        }

        [WorkItem(529947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529947")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCommentAfterInterfaceList1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
 
class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|} // Implement interface
",
@"using System;

class C : IServiceProvider // Implement interface
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(529947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529947")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCommentAfterInterfaceList2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
 
class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|} 
// Implement interface
",
@"using System;

class C : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
// Implement interface
");
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(958699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposable_NoDisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
@"using System;
class C : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 0));
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(958699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposable_DisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
$@"using System;
class C : IDisposable
{{
    private bool disposedValue;

{DisposePattern("protected virtual ", "C", "public void ")}
}}
", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 1));
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(958699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableExplicitly_NoDisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
@"using System;
class C : IDisposable
{
    void IDisposable.Dispose()
    {
        throw new NotImplementedException();
    }
}
", codeAction: ("True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 2));
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(941469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableExplicitly_DisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:System.IDisposable|}
{
    class IDisposable
    {
    }
}",
$@"using System;
class C : System.IDisposable
{{
    private bool disposedValue;

    class IDisposable
    {{
    }}

{DisposePattern("protected virtual ", "C", "void System.IDisposable.")}
}}", codeAction: ("True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 3));
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(958699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableAbstractly_NoDisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
abstract class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
@"using System;
abstract class C : IDisposable
{
    public abstract void Dispose();
}
", codeAction: ("False;True;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 2));
        }

        [WorkItem(994456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
        [WorkItem(958699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableThroughMember_NoDisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
class C : {|CS0535:IDisposable|}
{
    private IDisposable goo;
}",
@"using System;
class C : IDisposable
{
    private IDisposable goo;

    public void Dispose()
    {
        goo.Dispose();
    }
}", codeAction: ("False;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;goo", 2));
        }

        [WorkItem(941469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableExplicitly_NoNamespaceImportForSystem()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C : {|CS0535:System.IDisposable|}{|CS1513:|}{|CS1514:|}",
                FixedCode = $@"class C : System.IDisposable
{{
    private bool disposedValue;

{DisposePattern("protected virtual ", "C", "void System.IDisposable.", gcPrefix: "System.")}
}}
",
                CodeActionEquivalenceKey = "True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;",
                CodeActionIndex = 3,

                // 🐛 generated QualifiedName where SimpleMemberAccessExpression was expected
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [WorkItem(951968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableViaBaseInterface_NoDisposePattern()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : {|CS0535:{|CS0535:I|}|}
{
}",
@"using System;
interface I : IDisposable
{
    void F();
}
class C : I
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void F()
    {
        throw new NotImplementedException();
    }
}", codeAction: ("False;False;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 0));
        }

        [WorkItem(951968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableViaBaseInterface()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : {|CS0535:{|CS0535:I|}|}
{
}",
$@"using System;
interface I : IDisposable
{{
    void F();
}}
class C : I
{{
    private bool disposedValue;

    public void F()
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "public void ")}
}}", codeAction: ("False;False;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 1));
        }

        [WorkItem(951968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementIDisposableExplicitlyViaBaseInterface()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
interface I : IDisposable
{
    void F();
}
class C : {|CS0535:{|CS0535:I|}|}
{
}",
$@"using System;
interface I : IDisposable
{{
    void F();
}}
class C : I
{{
    private bool disposedValue;

    void I.F()
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "void IDisposable.")}
}}", codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 3));
        }

        [WorkItem(941469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDontImplementDisposePatternForLocallyDefinedIDisposable()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"namespace System
{
    interface IDisposable
    {
        void Dispose();
    }

    class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
}",
@"namespace System
{
    interface IDisposable
    {
        void Dispose();
    }

    class C : IDisposable
    {
        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}", codeAction: ("True;False;False:global::System.IDisposable;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDontImplementDisposePatternForStructures1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
struct S : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
@"using System;
struct S : IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDontImplementDisposePatternForStructures2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
struct S : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}",
@"using System;
struct S : IDisposable
{
    void IDisposable.Dispose()
    {
        throw new NotImplementedException();
    }
}
", codeAction: ("True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(545924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545924")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEnumNestedInGeneric()
        {
            var test = new VerifyCS.Test()
            {
                TestCode = @"class C<T>
{
    public enum E
    {
        X
    }
}

interface I
{
    void Goo<T>(C<T>.E x = C<T>.E.X);
}

class D : {|CS0535:I|}
{
}",
                FixedCode = @"class C<T>
{
    public enum E
    {
        X
    }
}

interface I
{
    void Goo<T>(C<T>.E x = C<T>.E.X);
}

class D : I
{
    public void Goo<T>(C<T>.E x = C<T>.E.X)
    {
        throw new System.NotImplementedException();
    }
}",
                // 🐛 generated QualifiedName where SimpleMemberAccessExpression was expected
                CodeActionValidationMode = CodeActionValidationMode.None,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [WorkItem(545939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnterminatedString1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class C : {|CS0535:IServiceProvider|} {|CS1039:|}@""{|CS1513:|}{|CS1514:|}",
@"using System;

class C : IServiceProvider {|CS1003:@""""|}{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(545939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnterminatedString2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class C : {|CS0535:IServiceProvider|} {|CS1010:|}""{|CS1513:|}{|CS1514:|}",
@"using System;

class C : IServiceProvider {|CS1003:""""|}{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(545939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnterminatedString3()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class C : {|CS0535:IServiceProvider|} {|CS1039:|}@""{|CS1513:|}{|CS1514:|}",
@"using System;

class C : IServiceProvider {|CS1003:@""""|}{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(545939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnterminatedString4()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class C : {|CS0535:IServiceProvider|} {|CS1010:|}""{|CS1513:|}{|CS1514:|}",
@"using System;

class C : IServiceProvider {|CS1003:""""|}{
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
}
");
        }

        [WorkItem(545940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545940")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDecimalENotation()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void Goo1(decimal x = 1E-25M);
    void Goo2(decimal x = -1E-25M);
    void Goo3(decimal x = 1E-24M);
    void Goo4(decimal x = -1E-24M);
}

class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}
{
}",
@"interface I
{
    void Goo1(decimal x = 1E-25M);
    void Goo2(decimal x = -1E-25M);
    void Goo3(decimal x = 1E-24M);
    void Goo4(decimal x = -1E-24M);
}

class C : I
{
    public void Goo1(decimal x = 0.0000000000000000000000001M)
    {
        throw new System.NotImplementedException();
    }

    public void Goo2(decimal x = -0.0000000000000000000000001M)
    {
        throw new System.NotImplementedException();
    }

    public void Goo3(decimal x = 0.000000000000000000000001M)
    {
        throw new System.NotImplementedException();
    }

    public void Goo4(decimal x = -0.000000000000000000000001M)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(545938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545938")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestGenericEnumWithRenamedTypeParameters()
        {
            var test = new VerifyCS.Test
            {
                TestCode = @"class C<T>
{
    public enum E
    {
        X
    }
}

interface I<S>
{
    void Goo<T>(S y, C<T>.E x = C<T>.E.X);
}

class D<T> : {|CS0535:I<T>|}
{
}",
                FixedCode = @"class C<T>
{
    public enum E
    {
        X
    }
}

interface I<S>
{
    void Goo<T>(S y, C<T>.E x = C<T>.E.X);
}

class D<T> : I<T>
{
    public void Goo<T1>(T y, C<T1>.E x = C<T1>.E.X)
    {
        throw new System.NotImplementedException();
    }
}",
                // 🐛 generated QualifiedName where SimpleMemberAccessExpression was expected
                CodeActionValidationMode = CodeActionValidationMode.None,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [WorkItem(545919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545919")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDoNotRenameTypeParameterToParameterName()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<S>
{
    void Goo<T>(S T1);
}

class C<T> : {|CS0535:I<T>|}
{
}",
@"interface I<S>
{
    void Goo<T>(S T1);
}

class C<T> : I<T>
{
    public void Goo<T2>(T T1)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(530265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530265")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestAttributes()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.U1)]
    bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.U1)]
    bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
}

class C : I
{
    [return: MarshalAs(UnmanagedType.U1)]
    public bool Goo([MarshalAs(UnmanagedType.U1)] bool x)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(530265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530265")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestAttributesExplicit()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.U1)]
    bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.U1)]
    bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
}

class C : I
{
    bool I.Goo(bool x)
    {
        throw new System.NotImplementedException();
    }
}",
codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(546443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546443")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestParameterNameWithTypeName()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

interface IGoo
{
    void Bar(DateTime DateTime);
}

class C : {|CS0535:IGoo|}
{
}",
@"using System;

interface IGoo
{
    void Bar(DateTime DateTime);
}

class C : IGoo
{
    public void Bar(DateTime DateTime)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(530521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530521")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnboundGeneric()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Collections.Generic;
using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
    void Goo();
}

class C : {|CS0535:I|}
{
}",
@"using System.Collections.Generic;
using System.Runtime.InteropServices;

interface I
{
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
    void Goo();
}

class C : I
{
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
    public void Goo()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(752436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752436")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestQualifiedNameImplicitInterface()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"namespace N
{
    public interface I
    {
        void M();
    }
}

class C : {|CS0535:N.I|}
{
}",
@"namespace N
{
    public interface I
    {
        void M();
    }
}

class C : N.I
{
    public void M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(752436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752436")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestQualifiedNameExplicitInterface()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"namespace N
{
    public interface I
    {
        void M();
    }
}

class C : {|CS0535:N.I|}
{
}",
@"using N;

namespace N
{
    public interface I
    {
        void M();
    }
}

class C : N.I
{
    void I.M()
    {
        throw new System.NotImplementedException();
    }
}", codeAction: ("True;False;False:global::N.I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(847464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForPartialType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface I
{
    void Goo();
}

partial class C
{
}

partial class C : {|CS0535:I|}
{
}",
@"public interface I
{
    void Goo();
}

partial class C
{
}

partial class C : I
{
    void I.Goo()
    {
        throw new System.NotImplementedException();
    }
}", codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(847464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForPartialType2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"public interface I
{
    void Goo();
}

partial class C : {|CS0535:I|}
{
}

partial class C
{
}",
@"public interface I
{
    void Goo();
}

partial class C : I
{
    void I.Goo()
    {
        throw new System.NotImplementedException();
    }
}

partial class C
{
}", codeAction: ("True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(847464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForPartialType3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"public interface I
{
    void Goo();
}

public interface I2
{
    void Goo2();
}

partial class C : {|CS0535:I|}
{
}

partial class C : {|CS0535:I2|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"public interface I
{
    void Goo();
}

public interface I2
{
    void Goo2();
}

partial class C : I
{
    void I.Goo()
    {
        throw new System.NotImplementedException();
    }
}

partial class C : {|CS0535:I2|}
{
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "True;False;False:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(752447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752447")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestExplicitImplOfIndexedProperty()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
public class Test : {|CS0535:{|CS0535:IGoo|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"
Public Interface IGoo
    Property IndexProp(ByVal p1 As Integer) As String
End Interface",
                            },
                        },
                    },
                    AdditionalProjectReferences =
                    {
                        "Assembly1",
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
public class Test : IGoo
{
    string IGoo.get_IndexProp(int p1)
    {
        throw new System.NotImplementedException();
    }

    void IGoo.set_IndexProp(int p1, string Value)
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                },
                CodeActionEquivalenceKey = "True;False;False:global::IGoo;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

        [WorkItem(602475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602475")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplicitImplOfIndexedProperty()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"using System;

class C : {|CS0535:{|CS0535:I|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Interface I
    Property P(x As Integer)
End Interface",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"using System;

class C : I
{
    public object get_P(int x)
    {
        throw new NotImplementedException();
    }

    public void set_P(int x, object Value)
    {
        throw new NotImplementedException();
    }
}",
                    },
                },
                CodeActionEquivalenceKey = "False;False;True:global::I;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementationOfIndexerWithInaccessibleAttributes()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"
using System;

class C : {|CS0535:I|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"
using System;
internal class ShouldBeRemovedAttribute : Attribute { }
public interface I
{
    string this[[ShouldBeRemovedAttribute] int i] { get; set; }
}"
                            },
                        },
                    },
                    AdditionalProjectReferences =
                    {
                        "Assembly1",
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"
using System;

class C : I
{
    public string this[int i]
    {
        get
        {
            throw new NotImplementedException();
        }

        set
        {
            throw new NotImplementedException();
        }
    }
}",
                    },
                },
                CodeActionEquivalenceKey = "False;False;True:global::I;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            };

            test.Options.AddRange(AllOptionsOff);
            await test.RunAsync();
        }

#if false
        [WorkItem(13677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoGenerateInVenusCase2()
        {
            await TestMissingAsync(
@"using System;
#line 1 ""Bar""
class Goo : [|IComparable|]
#line default
#line hidden");
        }
#endif

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForImplicitIDisposable()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class Program : {|CS0535:IDisposable|}
{
}",
$@"using System;

class Program : IDisposable
{{
    private bool disposedValue;

{DisposePattern("protected virtual ", "Program", "public void ")}
}}", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForExplicitIDisposable()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class Program : {|CS0535:IDisposable|}
{
    private bool DisposedValue;
}",
$@"using System;

class Program : IDisposable
{{
    private bool DisposedValue;
    private bool disposedValue;

{DisposePattern("protected virtual ", "Program", "void IDisposable.")}
}}", codeAction: ("True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 3));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForIDisposableNonApplicable1()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class Program : {|CS0535:IDisposable|}
{
    private bool disposedValue;
}",
@"using System;

class Program : IDisposable
{
    private bool disposedValue;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForIDisposableNonApplicable2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class Program : {|CS0535:IDisposable|}
{
    public void Dispose(bool flag)
    {
    }
}",
@"using System;

class Program : IDisposable
{
    public void Dispose(bool flag)
    {
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForExplicitIDisposableWithSealedClass()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

sealed class Program : {|CS0535:IDisposable|}
{
}",
$@"using System;

sealed class Program : IDisposable
{{
    private bool disposedValue;

{DisposePattern("private ", "Program", "void IDisposable.")}
}}", codeAction: ("True;False;False:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 3));
        }

        [WorkItem(9760, "https://github.com/dotnet/roslyn/issues/9760")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceForExplicitIDisposableWithExistingField()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

class Program : {|CS0535:IDisposable|}
{
    private bool disposedValue;
}",
$@"using System;

class Program : IDisposable
{{
    private bool disposedValue;
    private bool disposedValue1;

{DisposePattern("protected virtual ", "Program", "public void ", disposeField: "disposedValue1")}
}}", codeAction: ("False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 1));
        }

        [WorkItem(9760, "https://github.com/dotnet/roslyn/issues/9760")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceUnderscoreNameForFields()
        {
            await new VerifyCS.Test
            {
                TestCode = @"using System;

class Program : {|CS0535:IDisposable|}
{
}",
                FixedCode = $@"using System;

class Program : IDisposable
{{
    private bool _disposedValue;

{DisposePattern("protected virtual ", "Program", "public void ", disposeField: "_disposedValue")}
}}",
                Options =
                {
                    _options.FieldNamesAreCamelCaseWithUnderscorePrefix,
                },
                CodeActionEquivalenceKey = "False;False;True:global::System.IDisposable;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(939123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoComAliasNameAttributeOnMethodParameters()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    void M([System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p);
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    void M([System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p);
}

class C : I
{
    public void M(int p)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(939123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoComAliasNameAttributeOnMethodReturnType()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System.Runtime.InteropServices;

interface I
{
    [return: ComAliasName(""pAlias1"")]
    long M([ComAliasName(""pAlias2"")] int p);
}

class C : {|CS0535:I|}
{
}",
@"using System.Runtime.InteropServices;

interface I
{
    [return: ComAliasName(""pAlias1"")]
    long M([ComAliasName(""pAlias2"")] int p);
}

class C : I
{
    public long M(int p)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(939123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNoComAliasNameAttributeOnIndexerParameters()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I
{
    long this[[System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p] { get; }
}

class C : {|CS0535:I|}
{
}",
@"interface I
{
    long this[[System.Runtime.InteropServices.ComAliasName(""pAlias"")] int p] { get; }
}

class C : I
{
    public long this[int p]
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(947819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947819")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestMissingOpenBrace()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"namespace Scenarios
{
    public interface TestInterface
    {
        void M1();
    }

    struct TestStruct1 : {|CS0535:TestInterface|}{|CS1513:|}{|CS1514:|}


    // Comment
}",
@"namespace Scenarios
{
    public interface TestInterface
    {
        void M1();
    }

    struct TestStruct1 : TestInterface
    {
        public void M1()
        {
            throw new System.NotImplementedException();
        }
    }


    // Comment
}");
        }

        [WorkItem(994328, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994328")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDisposePatternWhenAdditionalUsingsAreIntroduced1()
        {
            //CSharpFeaturesResources.DisposePattern
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}

partial class C
{
}

partial class C : {|CS0535:{|CS0535:{|CS0535:I<System.Exception, System.AggregateException>|}|}|}, {|CS0535:System.IDisposable|}
{
}",
$@"using System;
using System.Collections.Generic;

interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}}

partial class C
{{
}}

partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
{{
    private bool disposedValue;

    public bool Equals(int other)
    {{
        throw new NotImplementedException();
    }}

    public List<AggregateException> M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
    {{
        throw new NotImplementedException();
    }}

    public List<UU> M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c) where UU : TT
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "public void ")}
}}", codeAction: ("False;False;True:global::I<global::System.Exception, global::System.AggregateException>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 1));
        }

        [WorkItem(994328, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994328")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDisposePatternWhenAdditionalUsingsAreIntroduced2()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}

partial class C : {|CS0535:{|CS0535:{|CS0535:I<System.Exception, System.AggregateException>|}|}|}, {|CS0535:System.IDisposable|}
{
}

partial class C
{
}",
$@"using System;
using System.Collections.Generic;

interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
{{
    System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
    System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
}}

partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
{{
    private bool disposedValue;

    bool IEquatable<int>.Equals(int other)
    {{
        throw new NotImplementedException();
    }}

    List<AggregateException> I<Exception, AggregateException>.M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
    {{
        throw new NotImplementedException();
    }}

    List<UU> I<Exception, AggregateException>.M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c)
    {{
        throw new NotImplementedException();
    }}

{DisposePattern("protected virtual ", "C", "void IDisposable.")}
}}

partial class C
{{
}}", codeAction: ("True;False;False:global::I<global::System.Exception, global::System.AggregateException>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction;", 3));
        }

        private static string DisposePattern(
            string disposeVisibility,
            string className,
            string implementationVisibility,
            string disposeField = "disposedValue",
            string gcPrefix = "")
        {
            return $@"    {disposeVisibility}void Dispose(bool disposing)
    {{
        if (!{disposeField})
        {{
            if (disposing)
            {{
                // {FeaturesResources.TODO_colon_dispose_managed_state_managed_objects}
            }}

            // {FeaturesResources.TODO_colon_free_unmanaged_resources_unmanaged_objects_and_override_finalizer}
            // {FeaturesResources.TODO_colon_set_large_fields_to_null}
            {disposeField} = true;
        }}
    }}

    // // {string.Format(FeaturesResources.TODO_colon_override_finalizer_only_if_0_has_code_to_free_unmanaged_resources, "Dispose(bool disposing)")}
    // ~{className}()
    // {{
    //     // {string.Format(FeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, "Dispose(bool disposing)")}
    //     Dispose(disposing: false);
    // }}

    {implementationVisibility}Dispose()
    {{
        // {string.Format(FeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, "Dispose(bool disposing)")}
        Dispose(disposing: true);
        {gcPrefix}GC.SuppressFinalize(this);
    }}";
        }

        [WorkItem(1132014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1132014")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleAttributes()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;

public class Goo : {|CS0535:Holder.SomeInterface|}
{
}

public class Holder
{
    public interface SomeInterface
    {
        void Something([SomeAttribute] string helloWorld);
    }

    private class SomeAttribute : Attribute
    {
    }
}",
@"using System;

public class Goo : Holder.SomeInterface
{
    public void Something(string helloWorld)
    {
        throw new NotImplementedException();
    }
}

public class Holder
{
    public interface SomeInterface
    {
        void Something([SomeAttribute] string helloWorld);
    }

    private class SomeAttribute : Attribute
    {
    }
}");
        }

        [WorkItem(2785, "https://github.com/dotnet/roslyn/issues/2785")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementInterfaceThroughStaticMemberInGenericClass()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Issue2785<T> : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:IList<object>|}|}|}|}|}|}|}|}|}|}|}|}|}
{
    private static List<object> innerList = new List<object>();
}",
@"using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Issue2785<T> : IList<object>
{
    private static List<object> innerList = new List<object>();

    public object this[int index]
    {
        get
        {
            return ((IList<object>)innerList)[index];
        }

        set
        {
            ((IList<object>)innerList)[index] = value;
        }
    }

    public int Count
    {
        get
        {
            return ((ICollection<object>)innerList).Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            return ((ICollection<object>)innerList).IsReadOnly;
        }
    }

    public void Add(object item)
    {
        ((ICollection<object>)innerList).Add(item);
    }

    public void Clear()
    {
        ((ICollection<object>)innerList).Clear();
    }

    public bool Contains(object item)
    {
        return ((ICollection<object>)innerList).Contains(item);
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
        ((ICollection<object>)innerList).CopyTo(array, arrayIndex);
    }

    public IEnumerator<object> GetEnumerator()
    {
        return ((IEnumerable<object>)innerList).GetEnumerator();
    }

    public int IndexOf(object item)
    {
        return ((IList<object>)innerList).IndexOf(item);
    }

    public void Insert(int index, object item)
    {
        ((IList<object>)innerList).Insert(index, item);
    }

    public bool Remove(object item)
    {
        return ((ICollection<object>)innerList).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<object>)innerList).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)innerList).GetEnumerator();
    }
}",
codeAction: ("False;False;False:global::System.Collections.Generic.IList<object>;mscorlib;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;innerList", 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface), CompilerTrait(CompilerFeature.Tuples)]
        public async Task LongTuple()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y);
}

class Class : {|CS0535:IInterface|}
{
    (int, string) x;
}",
@"interface IInterface
{
    (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y);
}

class Class : IInterface
{
    (int, string) x;

    public (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task LongTupleWithNames()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface
{
    (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y);
}

class Class : {|CS0535:IInterface|}
{
    (int, string) x;
}",
@"interface IInterface
{
    (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y);
}

class Class : IInterface
{
    (int, string) x;

    public (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task GenericWithTuple()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface<TA, TB>
{
    (TA, TB) Method1((TA, TB) y);
}

class Class : {|CS0535:IInterface<(int, string), int>|}
{
    (int, string) x;
}",
@"interface IInterface<TA, TB>
{
    (TA, TB) Method1((TA, TB) y);
}

class Class : IInterface<(int, string), int>
{
    (int, string) x;

    public ((int, string), int) Method1(((int, string), int) y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task GenericWithTupleWithNamess()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"interface IInterface<TA, TB>
{
    (TA a, TB b) Method1((TA a, TB b) y);
}

class Class : {|CS0535:IInterface<(int, string), int>|}
{
    (int, string) x;
}",
@"interface IInterface<TA, TB>
{
    (TA a, TB b) Method1((TA a, TB b) y);
}

class Class : IInterface<(int, string), int>
{
    (int, string) x;

    public ((int, string) a, int b) Method1(((int, string) a, int b) y)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [WorkItem(15387, "https://github.com/dotnet/roslyn/issues/15387")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithGroupingOff1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"interface IInterface
{
    int Prop { get; }
}

class Class : {|CS0535:IInterface|}
{
    void M() { }
}",
                FixedCode = @"interface IInterface
{
    int Prop { get; }
}

class Class : IInterface
{
    void M() { }

    public int Prop => throw new System.NotImplementedException();
}",
                Options =
                {
                    { ImplementTypeOptions.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd },
                },
            }.RunAsync();
        }

        [WorkItem(15387, "https://github.com/dotnet/roslyn/issues/15387")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDoNotReorderComImportMembers_01()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Runtime.InteropServices;

[ComImport]
[Guid(""00000000-0000-0000-0000-000000000000"")]
interface IComInterface
{
    void MOverload();
    void X();
    void MOverload(int i);
    int Prop { get; }
}

class Class : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IComInterface|}|}|}|}
{
}",
@"
using System.Runtime.InteropServices;

[ComImport]
[Guid(""00000000-0000-0000-0000-000000000000"")]
interface IComInterface
{
    void MOverload();
    void X();
    void MOverload(int i);
    int Prop { get; }
}

class Class : IComInterface
{
    public void MOverload()
    {
        throw new System.NotImplementedException();
    }

    public void X()
    {
        throw new System.NotImplementedException();
    }

    public void MOverload(int i)
    {
        throw new System.NotImplementedException();
    }

    public int Prop => throw new System.NotImplementedException();
}");
        }

        [WorkItem(15387, "https://github.com/dotnet/roslyn/issues/15387")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDoNotReorderComImportMembers_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode =
@"
using System.Runtime.InteropServices;

[ComImport]
[Guid(""00000000-0000-0000-0000-000000000000"")]
interface IComInterface
{
    void MOverload() { }
    void X() { }
    void MOverload(int i) { }
    int Prop { get; }
}

class Class : {|CS0535:IComInterface|}
{
}",
                FixedCode =
@"
using System.Runtime.InteropServices;

[ComImport]
[Guid(""00000000-0000-0000-0000-000000000000"")]
interface IComInterface
{
    void MOverload() { }
    void X() { }
    void MOverload(int i) { }
    int Prop { get; }
}

class Class : IComInterface
{
    public int Prop => throw new System.NotImplementedException();
}",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRefReturns()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

interface I {
    ref int IGoo();
    ref int Goo { get; }
    ref int this[int i] { get; }
}

class C : {|CS0535:{|CS0535:{|CS0535:I|}|}|}
{
}",
@"
using System;

interface I {
    ref int IGoo();
    ref int Goo { get; }
    ref int this[int i] { get; }
}

class C : I
{
    public ref int this[int i] => throw new NotImplementedException();

    public ref int Goo => throw new NotImplementedException();

    public ref int IGoo()
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(13932, "https://github.com/dotnet/roslyn/issues/13932")]
        [WorkItem(5898, "https://github.com/dotnet/roslyn/issues/5898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestAutoProperties()
        {
            await new VerifyCS.Test()
            {
                TestCode = @"interface IInterface
{
    int ReadOnlyProp { get; }
    int ReadWriteProp { get; set; }
    int WriteOnlyProp { set; }
}

class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                FixedCode = @"interface IInterface
{
    int ReadOnlyProp { get; }
    int ReadWriteProp { get; set; }
    int WriteOnlyProp { set; }
}

class Class : IInterface
{
    public int ReadOnlyProp { get; }
    public int ReadWriteProp { get; set; }
    public int WriteOnlyProp { set => throw new System.NotImplementedException(); }
}",
                Options =
                {
                    { ImplementTypeOptions.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestOptionalParameterWithDefaultLiteral()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp7_1,
                TestCode = @"
using System.Threading;

interface IInterface
{
    void Method1(CancellationToken cancellationToken = default(CancellationToken));
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"
using System.Threading;

interface IInterface
{
    void Method1(CancellationToken cancellationToken = default(CancellationToken));
}

class Class : IInterface
{
    public void Method1(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInWithMethod_Parameters()
        {
            await TestInRegularAndScriptAsync(
@"interface ITest
{
    void Method(in int p);
}
public class Test : {|CS0535:ITest|}
{
}",
@"interface ITest
{
    void Method(in int p);
}
public class Test : ITest
{
    public void Method(in int p)
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRefReadOnlyWithMethod_ReturnType()
        {
            await TestInRegularAndScriptAsync(
@"interface ITest
{
    ref readonly int Method();
}
public class Test : {|CS0535:ITest|}
{
}",
@"interface ITest
{
    ref readonly int Method();
}
public class Test : ITest
{
    public ref readonly int Method()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRefReadOnlyWithProperty()
        {
            await TestInRegularAndScriptAsync(
@"interface ITest
{
    ref readonly int Property { get; }
}
public class Test : {|CS0535:ITest|}
{
}",
@"interface ITest
{
    ref readonly int Property { get; }
}
public class Test : ITest
{
    public ref readonly int Property => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInWithIndexer_Parameters()
        {
            await TestInRegularAndScriptAsync(
@"interface ITest
{
    int this[in int p] { set; }
}
public class Test : {|CS0535:ITest|}
{
}",
@"interface ITest
{
    int this[in int p] { set; }
}
public class Test : ITest
{
    public int this[in int p] { set => throw new System.NotImplementedException(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestRefReadOnlyWithIndexer_ReturnType()
        {
            await TestInRegularAndScriptAsync(
@"interface ITest
{
    ref readonly int this[int p] { get; }
}
public class Test : {|CS0535:ITest|}
{
}",
@"interface ITest
{
    ref readonly int this[int p] { get; }
}
public class Test : ITest
{
    public ref readonly int this[int p] => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnmanagedConstraint()
        {
            await TestInRegularAndScriptAsync(
@"public interface ITest
{
    void M<T>() where T : unmanaged;
}
public class Test : {|CS0535:ITest|}
{
}",
@"public interface ITest
{
    void M<T>() where T : unmanaged;
}
public class Test : ITest
{
    public void M<T>() where T : unmanaged
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestSealedMember_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestSealedMember_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

class Class : IInterface
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestSealedMember_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

abstract class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    sealed void M1() {}
    sealed int P1 => 1;
}

abstract class Class : IInterface
{
    public abstract void Method1();
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicMember_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    protected void M1();
    protected int P1 {get;}
}

class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    void Method1();

    protected void M1();
    protected int P1 {get;}
}

class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;False;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicMember_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    protected void M1();
    protected int P1 {get;}
}

class Class : {|CS0535:{|CS0535:IInterface|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    protected void M1();
    protected int P1 {get;}
}

class Class : IInterface
{
    int IInterface.P1
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    void IInterface.M1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                DiagnosticSelector = diagnostics => diagnostics[1],
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicMember_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    protected void M1();
    protected int P1 {get;}
}

abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    void Method1();

    protected void M1();
    protected int P1 {get;}
}

abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public abstract void Method1();
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicAccessor_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    void Method1();

    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;False;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicAccessor_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

class Class : {|CS0535:{|CS0535:IInterface|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

class Class : IInterface
{
    int IInterface.P1
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }

    int IInterface.P2
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNonPublicAccessor_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                FixedState =
                {
                    Sources =
                    {
                        @"interface IInterface
{
    void Method1();

    int P1 {get; protected set;}
    int P2 {protected get; set;}
}

abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public abstract void Method1();
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestPrivateAccessor_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestPrivateAccessor_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

class Class : IInterface
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestPrivateAccessor_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

abstract class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    int P1 {get => 0; private set {}}
    int P2 {private get => 0; set {}}
}

abstract class Class : IInterface
{
    public abstract void Method1();
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleMember_01()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    internal void M1();
    internal int P1 {get;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },

                // Specify the code action by equivalence key only to avoid trying to implement the interface explicitly with a second code fix pass.
                CodeActionEquivalenceKey = "False;False;True:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleMember_02()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    internal void M1();
    internal int P1 {get;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleMember_03()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    internal void M1();
    internal int P1 {get;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public abstract void Method1();
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },

                // Specify the code action by equivalence key only to avoid trying to execute a second code fix pass with a different action
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleAccessor_01()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    int P1 {get; internal set;}
    int P2 {internal get; set;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },

                // Specify the code action by equivalence key only to avoid trying to implement the interface explicitly with a second code fix pass.
                CodeActionEquivalenceKey = "False;False;True:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleAccessor_02()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    int P1 {get; internal set;}
    int P2 {internal get; set;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestInaccessibleAccessor_03()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources =
                    {
                        @"abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
{
}",
                    },
                    AdditionalProjects =
                    {
                        ["Assembly1"] =
                        {
                            Sources =
                            {
                                @"public interface IInterface
{
    void Method1();

    int P1 {get; internal set;}
    int P2 {internal get; set;}
}",
                            },
                        },
                    },
                    AdditionalProjectReferences = { "Assembly1" },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
{
    public abstract void Method1();
}",
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                Options = { AllOptionsOff },

                // Specify the code action by equivalence key only to avoid trying to execute a second code fix pass with a different action
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;Assembly1;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestVirtualMember_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestVirtualMember_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

class Class : IInterface
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestVirtualMember_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

abstract class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    virtual void M1() {}
    virtual int P1 => 1;
}

abstract class Class : IInterface
{
    public abstract void Method1();
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticMember_01()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

class Class : IInterface
{
    public void Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticMember_02()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

class Class : IInterface
{
    void IInterface.Method1()
    {
        throw new System.NotImplementedException();
    }
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "True;False;False:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticMember_03()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

abstract class Class : {|CS0535:IInterface|}
{
}",
                FixedCode = @"interface IInterface
{
    void Method1();

    static void M1() {}
    static int P1 => 1;
    static int F1;
    public abstract class C {}
}

abstract class Class : IInterface
{
    public abstract void Method1();
}",
                Options = { AllOptionsOff },
                CodeActionEquivalenceKey = "False;True;True:global::IInterface;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNotNullConstraint()
        {
            await TestInRegularAndScriptAsync(
@"public interface ITest
{
    void M<T>() where T : notnull;
}
public class Test : {|CS0535:ITest|}
{
}",
@"public interface ITest
{
    void M<T>() where T : notnull;
}
public class Test : ITest
{
    public void M<T>() where T : notnull
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithNullableProperty()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable 

public interface ITest
{
    string? P { get; }
}
public class Test : {|CS0535:ITest|}
{
}",
@"#nullable enable 

public interface ITest
{
    string? P { get; }
}
public class Test : ITest
{
    public string? P => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithNullablePropertyAlreadyImplemented()
        {
            var code =
@"#nullable enable 

public interface ITest
{
    string? P { get; }
}
public class Test : ITest
{
    public string? P => throw new System.NotImplementedException();
}";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithNullableMethod()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable 

public interface ITest
{
    string? P();
}
public class Test : {|CS0535:ITest|}
{
}",
@"#nullable enable 

public interface ITest
{
    string? P();
}
public class Test : ITest
{
    public string? P()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithNullableEvent()
        {
            // Question whether this is needed,
            // see https://github.com/dotnet/roslyn/issues/36673 
            await TestInRegularAndScriptAsync(
@"#nullable enable 

using System;

public interface ITest
{
    event EventHandler? SomeEvent;
}
public class Test : {|CS0535:ITest|}
{
}",
@"#nullable enable 

using System;

public interface ITest
{
    event EventHandler? SomeEvent;
}
public class Test : ITest
{
    public event EventHandler? SomeEvent;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestWithNullableDisabled()
        {
            await TestInRegularAndScriptAsync(
@"#nullable enable 

public interface ITest
{
    string? P { get; }
}

#nullable disable

public class Test : {|CS0535:ITest|}
{
}",
@"#nullable enable 

public interface ITest
{
    string? P { get; }
}

#nullable disable

public class Test : ITest
{
    public string P => throw new System.NotImplementedException();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task GenericInterfaceNotNull1()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestCode = @"#nullable enable 

using System.Diagnostics.CodeAnalysis;

interface IFoo<T>
{
    [return: NotNull]
    T Bar([DisallowNull] T bar);

    [return: MaybeNull]
    T Baz([AllowNull] T bar);
}

class A : {|CS0535:{|CS0535:IFoo<int>|}|}
{
}",
                FixedCode = @"#nullable enable 

using System.Diagnostics.CodeAnalysis;

interface IFoo<T>
{
    [return: NotNull]
    T Bar([DisallowNull] T bar);

    [return: MaybeNull]
    T Baz([AllowNull] T bar);
}

class A : IFoo<int>
{
    [return: NotNull]
    public int Bar([DisallowNull] int bar)
    {
        throw new System.NotImplementedException();
    }

    [return: MaybeNull]
    public int Baz([AllowNull] int bar)
    {
        throw new System.NotImplementedException();
    }
}",
            }.RunAsync();
        }

        [WorkItem(13427, "https://github.com/dotnet/roslyn/issues/13427")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestDoNotAddNewWithGenericAndNonGenericMethods()
        {
            await TestWithAllCodeStyleOptionsOffAsync(
@"class B
{
    public void M<T>() { }
}

interface I
{
    void M();
}

class D : B, {|CS0535:I|}
{
}",
@"class B
{
    public void M<T>() { }
}

interface I
{
    void M();
}

class D : B, I
{
    public void M()
    {
        throw new System.NotImplementedException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task ImplementRemainingExplicitlyWhenPartiallyImplemented()
        {
            await TestInRegularAndScriptAsync(@"
interface I
{
    void M1();
    void M2();
}

class C : {|CS0535:I|}
{
    public void M1(){}
}",
@"
interface I
{
    void M1();
    void M2();
}

class C : {|CS0535:I|}
{
    public void M1(){}

    void I.M2()
    {
        throw new System.NotImplementedException();
    }
}", codeAction: ("True;False;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 2));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task ImplementInitOnlyProperty()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
interface I
{
    int Property { get; init; }
}

class C : {|CS0535:I|}
{
}",
                FixedCode = @"
interface I
{
    int Property { get; init; }
}

class C : I
{
    public int Property { get => throw new System.NotImplementedException(); init => throw new System.NotImplementedException(); }
}",
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task ImplementRemainingExplicitlyMissingWhenAllImplemented()
        {
            var code = @"
interface I
{
    void M1();
    void M2();
}

class C : I
{
    public void M1(){}
    public void M2(){}
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task ImplementRemainingExplicitlyMissingWhenAllImplementedAreExplicit()
        {
            var code = @"
interface I
{
    void M1();
    void M2();
}

class C : {|CS0535:I|}
{
    void I.M1(){}
}";
            var fixedCode = @"
interface I
{
    void M1();
    void M2();
}

class C : I
{
    public void M2()
    {
        throw new System.NotImplementedException();
    }

    void I.M1(){}
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementRemainingExplicitlyNonPublicMember()
        {
            await TestInRegularAndScriptAsync(@"
interface I
{
    void M1();
    internal void M2();
}

class C : {|CS0535:I|}
{
    public void M1(){}
}",
@"
interface I
{
    void M1();
    internal void M2();
}

class C : {|CS0535:I|}
{
    public void M1(){}

    void I.M2()
    {
        throw new System.NotImplementedException();
    }
}", codeAction: ("True;False;True:global::I;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;", 1));
        }

        [WorkItem(48295, "https://github.com/dotnet/roslyn/issues/48295")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementOnRecord_WithSemiColon()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface I
{
    void M1();
}

record C : {|CS0535:I|};
",
                FixedCode = @"
interface I
{
    void M1();
}

record C : {|CS0535:I|}
{
    public void M1()
    {
        throw new System.NotImplementedException();
    }
}
",
            }.RunAsync();
        }

        [WorkItem(48295, "https://github.com/dotnet/roslyn/issues/48295")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementOnRecord_WithBracesAndTrivia()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface I
{
    void M1();
}

record C : {|CS0535:I|} { } // hello
",
                FixedCode = @"
interface I
{
    void M1();
}

record C : {|CS0535:I|}
{
    public void M1()
    {
        throw new System.NotImplementedException();
    }
} // hello
",
            }.RunAsync();
        }

        [WorkItem(48295, "https://github.com/dotnet/roslyn/issues/48295")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task TestImplementOnRecord_WithSemiColonAndTrivia(string record)
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = $@"
interface I
{{
    void M1();
}}

{record} C : {{|CS0535:I|}}; // hello
",
                FixedCode = $@"
interface I
{{
    void M1();
}}

{record} C : {{|CS0535:I|}} // hello
{{
    public void M1()
    {{
        throw new System.NotImplementedException();
    }}
}}
",
            }.RunAsync();
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnconstrainedGenericInstantiatedWithValueType()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"#nullable enable
interface IGoo<T>
{
    void Bar(T? x);
}

class C : {|CS0535:IGoo<int>|}
{
}
",
                FixedCode = @"#nullable enable
interface IGoo<T>
{
    void Bar(T? x);
}

class C : IGoo<int>
{
    public void Bar(int x)
    {
        throw new System.NotImplementedException();
    }
}
",
            }.RunAsync();
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConstrainedGenericInstantiatedWithValueType()
        {
            await TestInRegularAndScriptAsync(@"
interface IGoo<T> where T : struct
{
    void Bar(T? x);
}

class C : {|CS0535:IGoo<int>|}
{
}
",
@"
interface IGoo<T> where T : struct
{
    void Bar(T? x);
}

class C : IGoo<int>
{
    public void Bar(int? x)
    {
        throw new System.NotImplementedException();
    }
}
");
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnconstrainedGenericInstantiatedWithReferenceType()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
interface IGoo<T>
{
#nullable enable
    void Bar(T? x);
#nullable restore
}

class C : {|CS0535:IGoo<string>|}
{
}
",
                FixedCode = @"
interface IGoo<T>
{
#nullable enable
    void Bar(T? x);
#nullable restore
}

class C : IGoo<string>
{
    public void Bar(string x)
    {
        throw new System.NotImplementedException();
    }
}
",
            }.RunAsync();
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUnconstrainedGenericInstantiatedWithReferenceType_NullableEnable()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
#nullable enable

interface IGoo<T>
{
    void Bar(T? x);
}

class C : {|CS0535:IGoo<string>|}
{
}
",
                FixedCode = @"
#nullable enable

interface IGoo<T>
{
    void Bar(T? x);
}

class C : IGoo<string>
{
    public void Bar(string? x)
    {
        throw new System.NotImplementedException();
    }
}
",
            }.RunAsync();
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConstrainedGenericInstantiatedWithReferenceType()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = @"
#nullable enable
interface IGoo<T> where T : class
{
    void Bar(T? x);
}

class C : {|CS0535:IGoo<string>|}
{
}
",
                FixedCode = @"
#nullable enable
interface IGoo<T> where T : class
{
    void Bar(T? x);
}

class C : IGoo<string>
{
    public void Bar(string? x)
    {
        throw new System.NotImplementedException();
    }
}
",
            }.RunAsync();
        }

        [WorkItem(49019, "https://github.com/dotnet/roslyn/issues/49019")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestConstrainedGenericInstantiatedWithReferenceType_NullableEnable()
        {
            await TestInRegularAndScriptAsync(@"
#nullable enable

interface IGoo<T> where T : class
{
    void Bar(T? x);
}

class C : {|CS0535:IGoo<string>|}
{
}
",
@"
#nullable enable

interface IGoo<T> where T : class
{
    void Bar(T? x);
}

class C : IGoo<string>
{
    public void Bar(string? x)
    {
        throw new System.NotImplementedException();
    }
}
");
        }

        [WorkItem(51779, "https://github.com/dotnet/roslyn/issues/51779")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestImplementTwoPropertiesOfCSharp5()
        {
            await new VerifyCS.Test
            {
                LanguageVersion = LanguageVersion.CSharp5,
                TestCode = @"
interface ITest
{
    int Bar { get; }
    int Foo { get; }
}

class Program : {|CS0535:{|CS0535:ITest|}|}
{
}
",
                FixedCode = @"
interface ITest
{
    int Bar { get; }
    int Foo { get; }
}

class Program : ITest
{
    public int Bar
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public int Foo
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}
",
            }.RunAsync();
        }

        [WorkItem(53925, "https://github.com/dotnet/roslyn/issues/53925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceMember()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract void M1();
}

class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract void M1();
}

class C : ITest
{
    public static void M1()
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface, codeAction.Title),
                CodeActionEquivalenceKey = "False;False;True:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [WorkItem(53925, "https://github.com/dotnet/roslyn/issues/53925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceMemberExplicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract void M1();
}

class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract void M1();
}

class C : ITest
{
    static void ITest.M1()
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_all_members_explicitly, codeAction.Title),
                CodeActionEquivalenceKey = "True;False;False:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(53925, "https://github.com/dotnet/roslyn/issues/53925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceMember_ImplementAbstractly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract void M1();
}

abstract class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract void M1();
}

abstract class C : ITest
{
    public static void M1()
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface_abstractly, codeAction.Title),
                CodeActionEquivalenceKey = "False;True;True:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceOperator_OnlyExplicitlyImplementable()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract int operator -(ITest x);
}
class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract int operator -(ITest x);
}
class C : ITest
{
    static int ITest.operator -(ITest x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_all_members_explicitly, codeAction.Title),
                CodeActionEquivalenceKey = "True;False;False:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceOperator_ImplementImplicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
    static abstract int operator -(T x, int y);
}
class C : {|CS0535:{|CS0535:ITest<C>|}|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
    static abstract int operator -(T x, int y);
}
class C : ITest<C>
{
    public static int operator -(C x)
    {
        throw new System.NotImplementedException();
    }

    public static int operator -(C x, int y)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface, codeAction.Title),
                CodeActionEquivalenceKey = "False;False;True:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceOperator_ImplementExplicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
}
class C : {|CS0535:ITest<C>|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
}
class C : ITest<C>
{
    static int ITest<C>.operator -(C x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_all_members_explicitly, codeAction.Title),
                CodeActionEquivalenceKey = "True;False;False:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,
            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterfaceOperator_ImplementAbstractly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
}
abstract class C : {|CS0535:ITest<C>|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int operator -(T x);
}
abstract class C : ITest<C>
{
    public static int operator -(C x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface_abstractly, codeAction.Title),
                CodeActionEquivalenceKey = "False;True;True:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,

            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterface_Explicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract int M(ITest x);
}
class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract int M(ITest x);
}
class C : ITest
{
    static int ITest.M(ITest x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_all_members_explicitly, codeAction.Title),
                CodeActionEquivalenceKey = "True;False;False:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,

            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterface_Implicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest
{
    static abstract int M(ITest x);
}
class C : {|CS0535:ITest|}
{
}
",
                FixedCode = @"
interface ITest
{
    static abstract int M(ITest x);
}
class C : ITest
{
    public static int M(ITest x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface, codeAction.Title),
                CodeActionEquivalenceKey = "False;False;True:global::ITest;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,

            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterface_ImplementImplicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
class C : {|CS0535:ITest<C>|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
class C : ITest<C>
{
    public static int M(C x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface, codeAction.Title),
                CodeActionEquivalenceKey = "False;False;True:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 0,

            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterface_ImplementExplicitly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
class C : {|CS0535:ITest<C>|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
class C : ITest<C>
{
    static int ITest<C>.M(C x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_all_members_explicitly, codeAction.Title),
                CodeActionEquivalenceKey = "True;False;False:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,

            }.RunAsync();
        }

        [WorkItem(53927, "https://github.com/dotnet/roslyn/issues/53927")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestStaticAbstractInterface_ImplementAbstractly()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                LanguageVersion = LanguageVersion.Preview,
                TestCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
abstract class C : {|CS0535:ITest<C>|}
{
}
",
                FixedCode = @"
interface ITest<T> where T : ITest<T>
{
    static abstract int M(T x);
}
abstract class C : ITest<C>
{
    public static int M(C x)
    {
        throw new System.NotImplementedException();
    }
}
",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal(FeaturesResources.Implement_interface_abstractly, codeAction.Title),
                CodeActionEquivalenceKey = "False;True;True:global::ITest<global::C>;TestProject;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
                CodeActionIndex = 1,

            }.RunAsync();
        }
    }
}
