// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class WhereKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NewClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          where x > y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousContinuationClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          group x by y into g
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtEndOfPreviousClause()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BetweenClauses()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterWhere()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          where $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass()
        {
            VerifyAbsence(
@"class C $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGenericClass()
        {
            VerifyKeyword(
@"class C<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClassBaseList()
        {
            VerifyAbsence(
@"class C : IFoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGenericClassBaseList()
        {
            VerifyKeyword(
@"class C<T> : IFoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegate()
        {
            VerifyAbsence(
@"delegate void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGenericDelegate()
        {
            VerifyKeyword(
@"delegate void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousClassConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousStructConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousNewConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousDelegateClassConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousDelegateStructConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousDelegateNewConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousDelegateConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGenericMethod()
        {
            VerifyKeyword(
@"class C {
    void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousMethodClassConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousMethodStructConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousMethodNewConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousMethodConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : IList<T> $$");
        }

        [WorkItem(550715)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterWhereTypeConstraint()
        {
            VerifyAbsence(
@"public class Foo<T> : System.Object where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterWhereWhere()
        {
            VerifyAbsence(
@"public class Foo<T> : System.Object where where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterWhereWhereWhere()
        {
            VerifyAbsence(
@"public class Foo<T> : System.Object where where where $$
{
}");
        }

        [WorkItem(550720)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NoWhereAfterDot()
        {
            VerifyAbsence(
@"public class Foo<where> : System.$$
{
}");
        }
    }
}
