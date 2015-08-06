// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class StackAllocKeywordRecommenderTests : KeywordRecommenderTests
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
        public void NotInEmptySpace()
        {
            VerifyAbsence(AddInsideMethod(
@"var v = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeEmptySpace()
        {
            VerifyKeyword(
@"unsafe class C {
    void Foo() {
      var v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeEmptySpace_NotAfterNonPointer()
        {
            VerifyAbsence(
@"unsafe class C {
    void Foo() {
      int v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeEmptySpace_AfterPointer()
        {
            VerifyKeyword(
@"unsafe class C {
    void Foo() {
      int* v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInField()
        {
            VerifyAbsence(
@"unsafe class C {
    int* v = $$");
        }

        [WorkItem(544504)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideForStatementVarDecl1()
        {
            VerifyKeyword(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (var i = $$");
        }

        [WorkItem(544504)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideForStatementVarDecl2()
        {
            VerifyKeyword(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (int* i = $$");
        }

        [WorkItem(544504)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideForStatementVarDecl3()
        {
            VerifyAbsence(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (string i = $$");
        }
    }
}
