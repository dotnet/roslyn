// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertToRecordRefactoringProvider>;

    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRecord)]
    public class ConvertToRecordTests
    {
        [Fact]
        public async Task TestNoProperties_NoAction()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        // field, not property
        public int f = 0;
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestPartialClass_NoAction()
        {
            var initialMarkup = @"
namespace N
{
    public partial class [|C|]
    {
        public int F { get; init; }
    }

    public partial class C
    {
        public bool B { get; init; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestExplicitProperty_NoAction1()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        private int f;

        public int P 
        {
            get => f; 
            init => f = value;
        }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestExplicitProperty_NoAction2()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        private int f;

        public int P 
        {
            get
            {
                return f;
            }

            init
            {
                f = value;
            }
        }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestExplicitProperty_NoAction3()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; } = 4;
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestPrivateGetProperty_NoAction()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { private get; init; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSetProperty()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; set; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record [|C|](int P);
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestInitPropertyOnStruct()
        {
            var initialMarkup = @"
namespace N
{
    public struct [|C|]
    {
        public int P { get; init; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record struct [|C|](int P)
    {
        public int P { get; init; } = P;
    }
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestPrivateSetProperty()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; private set; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record [|C|](int P)
    {
        public int P { get; private set; } = P;
    }
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveSimpleProperty()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestReadonlyProperty()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record [|C|](int P);
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestInitPropertyOnReadonlyStruct()
        {
            var initialMarkup = @"
namespace N
{
    public readonly struct [|C|]
    {
        public int P { get; init; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public readonly record struct [|C|](int P);
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertySimpleInheritance()
        {
            var initialMarkup = @"
namespace N
{
    public class B
    {
    }

    public class [|C|] : B
    {
        public int P { get; init; }
    }
}
";
            // three of the same error on C because the generated
            // EqualityConstract, Equals, and PrintMembers are all declared override
            // and there's nothing to override.
            // The other errors are copy constructor expected in B, and the
            // "records can't inherit from class" on B as well
            var changedMarkup = @"
namespace N
{
    public class B
    {
    }

    public record {|CS0115:{|CS0115:{|CS0115:{|CS8867:C|}|}|}|}(int P) : {|CS8864:B|};
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertySimpleInterfaceInheritance()
        {
            var initialMarkup = @"
namespace N
{
    public interface IInterface
    {
        public int Foo();
    }

    public class [|C|] : IInterface
    {
        public int P { get; init; }

        public int Foo()
        {
            return P;
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public interface IInterface
    {
        public int Foo();
    }

    public record C(int P) : IInterface
    {

        public int Foo()
        {
            return P;
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultipleProperties()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultiplePropertiesOnStruct()
        {
            var initialMarkup = @"
namespace N
{
    public struct [|C|]
    {
        public int P { get; set; }
        public bool B { get; set; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record struct C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultiplePropertiesOnReadonlyStruct()
        {
            var initialMarkup = @"
namespace N
{
    public readonly struct [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public readonly record struct C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        // if there are both init and set properties, convert both but keep set property override
        [Fact]
        public async Task TestSetAndInitProperties()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; set; }

        public int Q { get; init; }
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record [|C|](int P, int Q)
    {
        public int P { get; set; } = P;
    }
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultiplePropertiesOnGeneric()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C<TA, TB>|]
    {
        public TA? P { get; init; }
        public TB? B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C<TA, TB>(TA? P, TB? B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultiplePropertiesOnGenericWithConstraints()
        {
            var initialMarkup = @"
using System;
using System.Collections.Generic;

namespace N
{
    public class [|C|]<TA, TB>
        where TA : Exception
        where TB : IEnumerable<TA>
    {
        public TA? P { get; init; }
        public TB? B { get; init; }
    }
}
";
            var changedMarkup = @"
using System;
using System.Collections.Generic;

namespace N
{
    public record C<TA, TB>(TA? P, TB? B) where TA : Exception
            where TB : IEnumerable<TA>;
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithAttributes()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        [Obsolete(""P is Obsolete"", error: true)]
        public int P { get; init; }

        [Obsolete(""B will be obsolete, error: false"")]
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C([property: Obsolete(""P is Obsolete"", error: true)] int P, [property: Obsolete(""B will be obsolete, error: false"")] bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithAttributesAndComments1()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        // comment before
        [Obsolete(""P is Obsolete"", error: true)]
        public int P { get; init; }

        [Obsolete(""B will be obsolete, error: false"")]
        // comment after
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    // comment before
    // comment after
    public record C([property: Obsolete(""P is Obsolete"", error: true)] int P, [property: Obsolete(""B will be obsolete, error: false"")] bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithAttributesAndComments2()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        [/*comment before*/ Obsolete(""P is Obsolete"", error: true)]
        public int P { get; init; }

        [Obsolete(""B will be obsolete, error: false"") /* comment after*/]
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C([/*comment before*/ property: Obsolete(""P is Obsolete"", error: true)] int P, [property: Obsolete(""B will be obsolete, error: false"") /* comment after*/] bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam1()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2) {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam2()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2) {
            return !c1.Equals(c2);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableObjectParam3()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2) {
            return c2!.Equals(c1);
        }

        public static bool operator !=(C c1, object? c2) {
            return !(c2 == c1);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithObjectParam()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object c2) {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteEqualOperatorsWithExpressionBodies()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object c2)
            => c1.Equals(c2);

        public static bool operator !=(C c1, object c2)
            => !(c1 == c2);
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithSameTypeParams()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, C c2) {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, C c2) {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleEqualOperatorsWithNullableTypeParams()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C? c1, C? c2) {
            return c1!.Equals(c2);
        }

        public static bool operator !=(C? c1, C? c2) {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndKeepSideEffectOperator1()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2)
        {
            Console.WriteLine(""checking equality"");
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public static bool operator ==(C c1, object? c2)
        {
            Console.WriteLine(""checking equality"");
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c2);
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndKeepSideEffectOperator2()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            Console.WriteLine(""checking equality"");
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            Console.WriteLine(""checking equality"");
            return !(c1 == c2);
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed1()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c1);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c2);
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c1);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c2);
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndKeepSideEffectOperator_WhenSameParamUsed2()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c1);
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public static bool operator ==(C c1, object? c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(C c1, object? c2)
        {
            return !(c1 == c1);
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteClone()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public C Clone()
        {
            return this;
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimplePrimaryConstructor()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public C(int p, bool b)
        {
            P = p;
            B = b;
        }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDeleteSimpleExpressionPrimaryConstructor()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {
        public int P { get; init; }

        public C(int p)
            => P = p;
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDontDeleteComplexPrimaryConstructor1()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public C(int p, bool b)
        {
            Console.WriteLine(""Constructing C..."");
            P = p;
            B = b;
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public {|CS0111:{|CS8862:C|}|}(int p, bool b)
        {
            Console.WriteLine(""Constructing C..."");
            P = p;
            B = b;
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDontDeleteComplexPrimaryConstructor2()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public C(int p, bool b)
        {
            P = p + 1;
            B = b;
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public {|CS0111:{|CS8862:C|}|}(int p, bool b)
        {
            P = p + 1;
            B = b;
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesAndDontDeleteComplexPrimaryConstructor3()
        {
            var initialMarkup = @"
using System;

namespace N
{
    public class [|C|]
    {
        public int P { get; init; }
        public bool B { get; init; }

        public C(int p, bool b)
        {
            B = b;
        }
    }
}
";
            var changedMarkup = @"
using System;

namespace N
{
    public record C(int P, bool B)
    {

        public {|CS0111:{|CS8862:C|}|}(int p, bool b)
        {
            B = b;
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithSimpleDocComments()
        {
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    public class [|C|]
    {

        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithMultilineDocComments()
        {
            var initialMarkup = @"
namespace N
{

    /** 
     * <summary>
     * some summary
     * </summary>
     */
    public class [|C|]
    {

        /** 
         * <summary>
         * P is an int
         * </summary>
         */
        public int P { get; init; }

        /** 
         * <summary>
         * B is a bool
         * </summary>
         */
        public bool B { get; init; }
    }
}
";
            // this is what it should be
            //            var changedMarkup = @"
            //namespace N
            //{
            //    /** 
            //     * <summary>
            //     * some summary
            //     * </summary>
            //     * <param name=""P""> P is an int </param>
            //     * <param name=""B""> B is a bool </param>
            //     */
            //    public record C(int P, bool B);
            //}
            //";

            // this is what it is currently
            var changedMarkup = @"
namespace N
{
    /** 
         * <summary>
         * some summary
         * </summary>
         * <param name=""P""> P is an int </param>
         * <param name=""B""> B is a bool </param>
         */
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithMultilineDocComments_NoClassSummary()
        {
            var initialMarkup = @"
namespace N
{

    public class [|C|]
    {

        /** 
         * <summary>
         * P is an int
         * </summary>
         */
        public int P { get; init; }

        /** 
         * <summary>
         * B is a bool
         * </summary>
         */
        public bool B { get; init; }
    }
}
";
            // this is what it should be
            //            var changedMarkup = @"
            //namespace N
            //{
            //    /**
            //     * <param name=""P""> P is an int </param>
            //     * <param name=""B""> B is a bool </param>
            //     */
            //    public record C(int P, bool B);
            //}
            //";
            // this is what it is currently
            var changedMarkup = @"
namespace N
{
    /**
             *<param name=""P""> P is an int </param>
             * <param name=""B""> B is a bool </param>
    */
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithMixedDocComments1()
        {
            // class-level comment should be default
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    public class [|C|]
    {

        /** 
         * <summary>
         * P is an int
         * </summary>
         */
        public int P { get; init; }

        /** 
         * <summary>
         * B is a bool
         * </summary>
         */
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithMixedDocComments2()
        {
            // class-level comment should be default
            var initialMarkup = @"
namespace N
{

    /** 
     * <summary>
     * some summary
     * </summary>
     */
    public class [|C|]
    {

        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            // This is what it should be
            //            var changedMarkup = @"
            //namespace N
            //{
            //    /** 
            //     * <summary>
            //     * some summary
            //     * </summary>
            //     * <param name=""P""> P is an int </param>
            //     * <param name=""B""> B is a bool </param>
            //     */
            //    public record C(int P, bool B);
            //}
            //";

            // this is what it is right now
            var changedMarkup = @"
namespace N
{
    /** 
         * <summary>
         * some summary
         * </summary>
         * <param name=""P""> P is an int </param>
         * <param name=""B""> B is a bool </param>
         */
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithMixedDocComments3()
        {
            var initialMarkup = @"
namespace N
{

    /** 
     * <summary>
     * some summary
     * </summary>
     */
    public class [|C|]
    {

        /// <summary>
        /// P is an int
        /// with a multiline comment
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            // this is what it should be
            //            var changedMarkup = @"
            //namespace N
            //{
            //    /** 
            //     * <summary>
            //     * some summary
            //     * </summary>
            //     * <param name=""P""> P is an int
            //     * with a multiline comment </param>
            //     * <param name=""B""> B is a bool </param>
            //     */
            //    public record C(int P, bool B);
            //}
            //";

            // this is what it actually is
            var changedMarkup = @"
namespace N
{
    /** 
         * <summary>
         * some summary
         * </summary>
         * <param name=""P""> P is an int
         * with a multiline comment </param>
         * <param name=""B""> B is a bool </param>
         */
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocComments_NoClassSummary()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C|]
    {

        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocComments_MissingPropertySummary()
        {
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    public class [|C|]
    {

        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""></param>
    /// <param name=""B""> B is a bool </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocComments_AdditionalClassSection()
        {
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    /// <remarks>
    /// Some remarks as well
    /// </reamrks>
    public class [|C|]
    {

        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    /// <remarks>
    /// Some remarks as well
    /// </reamrks>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocComments_NestedPropertyElements()
        {
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    public class [|C|]
    {

        /// <summary>
        /// P is an int <see cref=""C.B""/>
        /// </summary>
        public int P { get; init; }

        /// <summary>
        /// B is a bool
        /// <c> Some code text </c>
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int <see cref=""C.B""/> </param>
    /// <param name=""B""> B is a bool
    /// <c> Some code text </c> </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocAndNonDocComments1()
        {
            // we should try to keep the order in the same as the order on the class comments
            var initialMarkup = @"
namespace N
{

    // Non-Doc comment before class
    /// <summary>
    /// some summary
    /// </summary>
    public class [|C|]
    {

        // Non-Doc property comment for P
        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        // Non-Doc property comment for B
        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    // Non-Doc comment before class
    // Non-Doc property comment for P
    // Non-Doc property comment for B
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertiesWithDocAndNonDocComments2()
        {
            // we should try to keep the order in the same as the order on the class comments
            var initialMarkup = @"
namespace N
{

    /// <summary>
    /// some summary
    /// </summary>
    // Non-Doc comment after class
    public class [|C|]
    {

        // Non-Doc property comment for P
        /// <summary>
        /// P is an int
        /// </summary>
        public int P { get; init; }

        // Non-Doc property comment for B
        /// <summary>
        /// B is a bool
        /// </summary>
        public bool B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    /// <summary>
    /// some summary
    /// </summary>
    /// <param name=""P""> P is an int </param>
    /// <param name=""B""> B is a bool </param>
    // Non-Doc comment after class
    // Non-Doc property comment for P
    // Non-Doc property comment for B
    public record C(int P, bool B);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        #region selection

        [Fact]
        public async Task TestSelectOnProperty_NoAction()
        {
            var initialMarkup = @"
namespace N
{
    public class C
    {
        public int [|P|] { get; init; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectOnNamespace_NoAction()
        {
            var initialMarkup = @"
namespace [|N|]
{
    public class C
    {
        public int P { get; init; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectLargeRegionIncludingNamespace_NoAction()
        {
            var initialMarkup = @"
namespace [|N
{
    public clas|]s C
    {
        public int P { get; init; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMultipleMembersWithinClass()
        {
            var initialMarkup = @"
namespace N
{
    public class C
    {
        [|public int P { get; init; }

        public int Foo()
        {
            return 0;
        }|]
    }
}
";
            var fixedMarkup = @"
namespace N
{
    public record C(int P)
    {

        public int Foo()
        {
            return 0;
        }
    }
}
";
            await TestRefactoringAsync(initialMarkup, fixedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectRegionIncludingClass()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C
    {
        public int P { get; init; }|]
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectClassKeyword()
        {
            var initialMarkup = @"
namespace N
{
    public cl[||]ass C
    {
        public int P { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C(int P);
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        #endregion

        private class Test : VerifyCS.Test
        {
            public Test() { }

            protected override Workspace CreateWorkspaceImpl()
            {
                var workspace = new AdhocWorkspace();

                return workspace;
            }
        }

        private static async Task TestRefactoringAsync(
            string initialMarkup,
            string changedMarkup)
        {
            var test = new Test()
            {
                TestCode = initialMarkup,
                FixedCode = changedMarkup,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
            };
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;

                var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
                // enable nullable
                compilationOptions = compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable);
                solution = solution
                    .WithProjectCompilationOptions(projectId, compilationOptions)
                    .WithProjectMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.Net60));

                return solution;
            });
            await test.RunAsync().ConfigureAwait(false);
        }

        private static Task TestNoRefactoringAsync(
            string initialMarkup) => TestRefactoringAsync(initialMarkup, initialMarkup);
    }
}
