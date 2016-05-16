// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateMethod;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateMethod
{
    public class GenerateConversionTest : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new GenerateConversionCodeFixProvider());
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateImplicitConversionGenericClass()
        {
            await TestAsync(
@"class Program { void Test ( int [ ] a ) { C < int > x1 = [|1|] ; } } class C < T > { } ",
@"using System ; class Program { void Test ( int [ ] a ) { C < int > x1 = 1 ; } } class C < T > { public static implicit operator C < T > ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateImplicitConversionClass()
        {
            await TestAsync(
@"class Program { void Test ( int [ ] a ) { C x1 = [|1|] ; } } class C { } ",
@"using System ; class Program { void Test ( int [ ] a ) { C x1 = 1 ; } } class C { public static implicit operator C ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateImplicitConversionAwaitExpression()
        {
            await TestAsync(
@"using System ; using System . Threading . Tasks ; class Program { async void Test ( ) { var a = Task . FromResult ( 1 ) ; Program x1 = [|await a|] ; } } ",
@"using System ; using System . Threading . Tasks ; class Program { async void Test ( ) { var a = Task . FromResult ( 1 ) ; Program x1 = await a ; } public static implicit operator Program ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateImplicitConversionTargetTypeNotInSource()
        {
            await TestAsync(
@"class Digit { public Digit ( double d ) { val = d ; } public double val ; } class Program { static void Main ( string [ ] args ) { Digit dig = new Digit ( 7 ) ; double num = [|dig|] ; } } ",
@"using System ; class Digit { public Digit ( double d ) { val = d ; } public double val ; public static implicit operator double ( Digit v ) { throw new NotImplementedException ( ) ; } } class Program { static void Main ( string [ ] args ) { Digit dig = new Digit ( 7 ) ; double num = dig ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateExplicitConversionGenericClass()
        {
            await TestAsync(
@"class Program { void Test ( int [ ] a ) { C < int > x1 = [|( C < int > ) 1|] ; } } class C < T > { } ",
@"using System ; class Program { void Test ( int [ ] a ) { C < int > x1 = ( C < int > ) 1 ; } } class C < T > { public static explicit operator C < T > ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateExplicitConversionClass()
        {
            await TestAsync(
@"class Program { void Test ( int [ ] a ) { C x1 = [|( C ) 1|] ; } } class C { } ",
@"using System ; class Program { void Test ( int [ ] a ) { C x1 = ( C ) 1 ; } } class C { public static explicit operator C ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateExplicitConversionAwaitExpression()
        {
            await TestAsync(
@"using System ; using System . Threading . Tasks ; class Program { async void Test ( ) { var a = Task . FromResult ( 1 ) ; Program x1 = [|( Program ) await a|] ; } } ",
@"using System ; using System . Threading . Tasks ; class Program { async void Test ( ) { var a = Task . FromResult ( 1 ) ; Program x1 = ( Program ) await a ; } public static explicit operator Program ( int v ) { throw new NotImplementedException ( ) ; } } ");
        }

        [WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task TestGenerateExplicitConversionTargetTypeNotInSource()
        {
            await TestAsync(
@"class Digit { public Digit ( double d ) { val = d ; } public double val ; } class Program { static void Main ( string [ ] args ) { Digit dig = new Digit ( 7 ) ; double num = [|( double ) dig|] ; } } ",
@"using System ; class Digit { public Digit ( double d ) { val = d ; } public double val ; public static explicit operator double ( Digit v ) { throw new NotImplementedException ( ) ; } } class Program { static void Main ( string [ ] args ) { Digit dig = new Digit ( 7 ) ; double num = ( double ) dig ; } } ");
        }
    }
}
