// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRecord
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertToRecordRefactoringProvider>;

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
        public async Task TestSetterProperty_NoAction()
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
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSetterPropertyOnReadonlyStruct_NoAction()
        {
            var initialMarkup = @"
namespace N
{
    public readonly struct [|C|]
    {
        public int P { get; }
    }
}
";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestInitPropertyOnStruct_NoAction()
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
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
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
    public record C(int P)
    {
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
    public record C(int P, bool B)
    {
    }
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
    public record struct C(int P, bool B)
    {
    }
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
    public readonly record struct C(int P, bool B)
    {
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMultiplePropertiesOnGeneric()
        {
            var initialMarkup = @"
namespace N
{
    public class [|C<TA, TB>|]
    {
        public TA P { get; init; }
        public TB B { get; init; }
    }
}
";
            var changedMarkup = @"
namespace N
{
    public record C<TA, TB>(TA P, TB B)
    {
    }
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
        public TA P { get; init; }
        public TB B { get; init; }
    }
}
";
            var changedMarkup = @"
using System;
using System.Collections.Generic;

namespace N
{
    public record C<TA, TB>(TA P, TB B)
        where TA : Exception where TB : IEnumerable<TA>
    {
    }
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
    public record C([property: Obsolete(""P is Obsolete"", error: true)] int P, [property: Obsolete(""B will be obsolete, error: false"")] bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
            return c2.Equals(c1);
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
            return c1.Equals(c2);
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
//    public record C(int P, bool B)
//    {
//    }
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
    public record C(int P, bool B)
    {
    }
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
//    public record C(int P, bool B)
//    {
//    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
            //    public record C(int P, bool B)
            //    {
            //    }
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
    public record C(int P, bool B)
    {
    }
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
//    public record C(int P, bool B)
//    {
//    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
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
    public record C(int P, bool B)
    {
    }
}
";
            await TestRefactoringAsync(initialMarkup, changedMarkup).ConfigureAwait(false);
        }

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
                var project = solution.GetProject(projectId);
                if (project == null)
                {
                    return solution;
                }

                var compilationOptions = project.CompilationOptions!;
                // remove the nullable warnings set in the basic workspace
                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.RemoveRange(CSharpVerifierHelper.NullableWarnings.Keys));
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
