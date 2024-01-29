// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateOverrides;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateOverrides
{
    public class GenerateOverridesTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new GenerateOverridesCodeRefactoringProvider((IPickMembersService)parameters.fixProviderData);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task Test1()
        {
            await TestWithPickMembersDialogAsync(
                """
                class C
                {
                    [||]
                }
                """,
                """
                class C
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);
                    }

                    public override int GetHashCode()
                    {
                        return base.GetHashCode();
                    }

                    public override string ToString()
                    {
                        return base.ToString();
                    }
                }
                """, ["Equals", "GetHashCode", "ToString"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestAtEndOfFile()
        {
            await TestWithPickMembersDialogAsync(
                """
                class C[||]
                """,
                """
                class C
                {
                    public override bool Equals(object obj)
                    {
                        return base.Equals(obj);
                    }

                    public override int GetHashCode()
                    {
                        return base.GetHashCode();
                    }

                    public override string ToString()
                    {
                        return base.ToString();
                    }
                }

                """, ["Equals", "GetHashCode", "ToString"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/48295")]
        public async Task TestOnRecordWithSemiColon()
        {
            await TestWithPickMembersDialogAsync("""
                record C[||];
                """, """
                record C
                {
                    public override int GetHashCode()
                    {
                        return base.GetHashCode();
                    }

                    public override string ToString()
                    {
                        return base.ToString();
                    }
                }

                """, ["GetHashCode", "ToString"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/17698")]
        public async Task TestRefReturns()
        {
            await TestWithPickMembersDialogAsync(
                """
                using System;

                class Base
                {
                    public virtual ref int X() => throw new NotImplementedException();

                    public virtual ref int Y => throw new NotImplementedException();

                    public virtual ref int this[int i] => throw new NotImplementedException();
                }

                class Derived : Base
                {
                     [||]
                }
                """,
                """
                using System;

                class Base
                {
                    public virtual ref int X() => throw new NotImplementedException();

                    public virtual ref int Y => throw new NotImplementedException();

                    public virtual ref int this[int i] => throw new NotImplementedException();
                }

                class Derived : Base
                {
                    public override ref int this[int i] => ref base[i];

                    public override ref int Y => ref base.Y;

                    public override ref int X()
                    {
                        return ref base.X();
                    }
                }
                """, ["X", "Y", "this[]"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestInitOnlyProperty()
        {
            await TestWithPickMembersDialogAsync(
                """
                class Base
                {
                    public virtual int Property { init => throw new NotImplementedException(); }
                }

                class Derived : Base
                {
                     [||]
                }
                """,
                """
                class Base
                {
                    public virtual int Property { init => throw new NotImplementedException(); }
                }

                class Derived : Base
                {
                    public override int Property { init => base.Property = value; }
                }
                """, ["Property"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestInitOnlyIndexer()
        {
            await TestWithPickMembersDialogAsync(
                """
                class Base
                {
                    public virtual int this[int i] { init => throw new NotImplementedException(); }
                }

                class Derived : Base
                {
                     [||]
                }
                """,
                """
                class Base
                {
                    public virtual int this[int i] { init => throw new NotImplementedException(); }
                }

                class Derived : Base
                {
                    public override int this[int i] { init => base[i] = value; }
                }
                """, ["this[]"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/21601")]
        public async Task TestMissingInStaticClass1()
        {
            await TestMissingAsync(
                """
                static class C
                {
                    [||]
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/21601")]
        public async Task TestMissingInStaticClass2()
        {
            await TestMissingAsync(
                """
                static class [||]C
                {

                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/53012")]
        public async Task TestNullableTypeParameter()
        {
            await TestWithPickMembersDialogAsync(
                """
                class C
                {
                    public virtual void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d) {}
                }

                class D : C
                {
                    [||]
                }
                """,
                """
                class C
                {
                    public virtual void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d) {}
                }

                class D : C
                {
                    public override void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
                        where T1 : default
                        where T3 : default
                    {
                        base.M(a, b, c, d);
                    }
                }
                """, ["M"]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)]
        public async Task TestRequiredProperty()
        {
            await TestWithPickMembersDialogAsync(
                """
                class Base
                {
                    public virtual required int Property { get; set; }
                }

                class Derived : Base
                {
                     [||]
                }
                """,
                """
                class Base
                {
                    public virtual required int Property { get; set; }
                }

                class Derived : Base
                {
                    public override required int Property { get => base.Property; set => base.Property = value; }
                }
                """, ["Property"]);
        }
    }
}
