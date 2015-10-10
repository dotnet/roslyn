// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class VoidKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStackAlloc()
        {
            VerifyAbsence(
@"class C {
     int* foo = stackalloc $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InFixedStatement()
        {
            VerifyKeyword(
@"fixed ($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InDelegateReturnType()
        {
            VerifyKeyword(
@"public delegate $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCastType()
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCastType2()
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InTypeOf()
        {
            VerifyKeyword(AddInsideMethod(
@"typeof($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCompilationUnit()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterExtern()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"extern alias Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"extern alias Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"using Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsing_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"using Foo;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNamespace()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"namespace N {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTypeDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"delegate void Foo();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Foo() {}
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Foo;");
        }

        [WpfFact(Skip = "528041"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Foo;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAssemblyAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"[assembly: foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterAssemblyAttribute_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"[assembly: foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterRootAttribute()
        {
            VerifyAbsence(@"[foo]
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [foo]
  $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPartial()
        {
            VerifyAbsence(@"partial $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedPartial()
        {
            VerifyKeyword(
@"class C {
    partial $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAbstract()
        {
            VerifyAbsence(@"abstract $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInternal()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"internal $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterInternal_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"internal $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPublic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPublic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterProtected()
        {
            VerifyAbsence(
@"protected $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterSealed()
        {
            VerifyAbsence(@"sealed $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStatic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"static $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStatic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"static $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStaticPublic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"static public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStaticPublic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"static public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegate()
        {
            VerifyKeyword(
@"delegate $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAnonymousDelegate()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = delegate $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNew()
        {
            VerifyAbsence(
@"new $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe {
    $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeMethod()
        {
            VerifyKeyword(
@"class C {
   unsafe void Foo() {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeClass()
        {
            VerifyKeyword(
@"unsafe class C {
   void Foo() {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInParameter()
        {
            VerifyAbsence(
@"class C {
   void Foo($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeParameter1()
        {
            VerifyKeyword(
@"class C {
   unsafe void Foo($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeParameter2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Foo($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCast()
        {
            VerifyAbsence(
@"class C {
   void Foo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCast2()
        {
            VerifyAbsence(
@"class C {
   void Foo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeCast()
        {
            VerifyKeyword(
@"unsafe class C {
   void Foo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeCast2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Foo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeConversionOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe implicit operator int(C c) {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe operator int ++(C c) {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeConstructor()
        {
            VerifyKeyword(
@"class C {
   unsafe C() {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeDestructor()
        {
            VerifyKeyword(
@"class C {
   unsafe ~C() {
     $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeProperty()
        {
            VerifyKeyword(
@"class C {
   unsafe int Foo {
     get {
       $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeIndexer()
        {
            VerifyKeyword(
@"class C {
   unsafe int this[int i] {
     get {
       $$");
        }

        [WorkItem(538804)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InSizeOf()
        {
            VerifyKeyword(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544347)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUnsafeDefaultExpression()
        {
            VerifyKeyword(
@"unsafe class C
{
    static void Method1(void* p1 = default($$");
        }

        [WorkItem(544347)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefaultExpression()
        {
            VerifyAbsence(
@"class C
{
    static void Method1(void* p1 = default($$");
        }

        [WorkItem(18374)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterAsync()
        {
            VerifyKeyword(@"class c { async $$ }");
        }

        [WorkItem(18374)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAsyncAsType()
        {
            VerifyAbsence(@"class c { async async $$ }");
        }
    }
}
