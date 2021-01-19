// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class InitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public void TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
   int Goo { $$");
        }

        [Fact]
        public void TestAfterPropertyPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { private $$");
        }

        [Fact]
        public void TestAfterPropertyAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] $$");
        }

        [Fact]
        public void TestAfterPropertyAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { [Bar] private $$");
        }

        [Fact]
        public void TestAfterPropertyGet()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; $$");
        }

        [Fact]
        public void TestAfterPropertyGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; private $$");
        }

        [Fact]
        public void TestAfterPropertyGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] $$");
        }

        [Fact]
        public void TestAfterPropertyGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get; [Bar] private $$");
        }

        [Fact]
        public void TestAfterGetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } $$");
        }

        [Fact]
        public void TestAfterSetAccessorBlock()
        {
            VerifyKeyword(
@"class C {
   int Goo { set { } $$");
        }

        [Fact]
        public void TestAfterGetAccessorBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } private $$");
        }

        [Fact]
        public void TestAfterGetAccessorBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] $$");
        }

        [Fact]
        public void TestAfterGetAccessorBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int Goo { get { } [Bar] private $$");
        }

        [Fact]
        public void TestNotAfterPropertySetKeyword()
        {
            VerifyAbsence(
@"class C {
   int Goo { set $$");
        }

        [Fact]
        public void TestAfterPropertySetAccessor()
        {
            VerifyKeyword(
@"class C {
   int Goo { set; $$");
        }

        [Fact]
        public void TestNotInEvent()
        {
            VerifyAbsence(
@"class C {
   event Goo E { $$");
        }

        [Fact]
        public void TestAfterIndexer()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { $$");
        }

        [Fact]
        public void TestAfterIndexerPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { private $$");
        }

        [Fact]
        public void TestAfterIndexerAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] $$");
        }

        [Fact]
        public void TestAfterIndexerAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { [Bar] private $$");
        }

        [Fact]
        public void TestAfterIndexerGet()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; $$");
        }

        [Fact]
        public void TestAfterIndexerGetAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; private $$");
        }

        [Fact]
        public void TestAfterIndexerGetAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] $$");
        }

        [Fact]
        public void TestAfterIndexerGetAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get; [Bar] private $$");
        }

        [Fact]
        public void TestAfterIndexerGetBlock()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } $$");
        }

        [Fact]
        public void TestAfterIndexerGetBlockAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } private $$");
        }

        [Fact]
        public void TestAfterIndexerGetBlockAndAttribute()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] $$");
        }

        [Fact]
        public void TestAfterIndexerGetBlockAndAttributeAndPrivate()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { get { } [Bar] private $$");
        }

        [Fact]
        public void TestNotAfterIndexerSetKeyword()
        {
            VerifyAbsence(
@"class C {
   int this[int i] { set $$");
        }

        [Fact]
        public void TestAfterIndexerSetAccessor()
        {
            VerifyKeyword(
@"class C {
   int this[int i] { set; $$");
        }
    }
}
