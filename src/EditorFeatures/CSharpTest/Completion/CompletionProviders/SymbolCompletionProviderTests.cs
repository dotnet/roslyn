// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    public partial class SymbolCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public SymbolCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new SymbolCompletionProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFile()
        {
            VerifyItemIsAbsent(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFile_Interactive()
        {
            VerifyItemIsAbsent(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemExists(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFileWithUsing()
        {
            VerifyItemIsAbsent(@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFileWithUsing_Interactive()
        {
            VerifyItemExists(@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemExists(@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterHashR()
        {
            VerifyItemIsAbsent(@"#r $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterHashLoad()
        {
            VerifyItemIsAbsent(@"#load $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirective()
        {
            VerifyItemIsAbsent(@"using $$", @"String");
            VerifyItemIsAbsent(@"using $$ = System", @"System");
            VerifyItemExists(@"using $$", @"System");
            VerifyItemExists(@"using T = $$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InactiveRegion()
        {
            VerifyItemIsAbsent(@"class C {
#if false 
$$
#endif", @"String");
            VerifyItemIsAbsent(@"class C {
#if false 
$$
#endif", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ActiveRegion()
        {
            VerifyItemIsAbsent(@"class C {
#if true 
$$
#endif", @"String");
            VerifyItemExists(@"class C {
#if true 
$$
#endif", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InactiveRegionWithUsing()
        {
            VerifyItemIsAbsent(@"using System;

class C {
#if false 
$$
#endif", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
#if false 
$$
#endif", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ActiveRegionWithUsing()
        {
            VerifyItemExists(@"using System;

class C {
#if true 
$$
#endif", @"String");
            VerifyItemExists(@"using System;

class C {
#if true 
$$
#endif", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment1()
        {
            VerifyItemIsAbsent(@"using System;

class C {
// $$", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
// $$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment2()
        {
            VerifyItemIsAbsent(@"using System;

class C {
// $$
", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
// $$
", @"System");
            VerifyItemIsAbsent(@"using System;

class C {
  // $$
", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineComment()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/*  $$", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
/*  $$", @"System");
            VerifyItemIsAbsent(@"using System;

class C {
/*  $$   */", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
/*  $$   */", @"System");
            VerifyItemExists(@"using System;

class C {
/*    */$$", @"System");
            VerifyItemExists(@"using System;

class C {
/*    */$$
", @"System");
            VerifyItemExists(@"using System;

class C {
  /*    */$$
", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineXmlComment1()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/// $$", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
/// $$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineXmlComment2()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/// $$
", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
/// $$
", @"System");
            VerifyItemIsAbsent(@"using System;

class C {
  /// $$
", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineXmlComment()
        {
            VerifyItemIsAbsent(@"using System;

class C {
/**  $$   */", @"String");
            VerifyItemIsAbsent(@"using System;

class C {
/**  $$   */", @"System");
            VerifyItemExists(@"using System;

class C {
/**     */$$", @"System");
            VerifyItemExists(@"using System;

class C {
/**     */$$
", @"System");
            VerifyItemExists(@"using System;

class C {
  /**     */$$
", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenStringLiteral()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenStringLiteralInDirective()
        {
            VerifyItemIsAbsent("#r \"$$", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemIsAbsent("#r \"$$", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StringLiteral()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"System");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StringLiteralInDirective()
        {
            VerifyItemIsAbsent("#r \"$$\"", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemIsAbsent("#r \"$$\"", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenCharLiteral()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"System");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssemblyAttribute1()
        {
            VerifyItemExists(@"[assembly: $$]", @"System");
            VerifyItemIsAbsent(@"[assembly: $$]", @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssemblyAttribute2()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"[assembly: $$]"), @"System");
            VerifyItemExists(AddUsingDirectives("using System;", @"[assembly: $$]"), @"AttributeUsage");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SystemAttributeIsNotAnAttribute()
        {
            var content = @"[$$]
class CL {}";

            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"Attribute");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeAttribute()
        {
            var content = @"[$$]
class CL {}";

            VerifyItemExists(AddUsingDirectives("using System;", content), @"AttributeUsage");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParamAttribute()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"AttributeUsage");
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodAttribute()
        {
            var content = @"class CL {
    [$$]
    void Method() {}
}";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"AttributeUsage");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodTypeParamAttribute()
        {
            var content = @"class CL{
    void Method<[A$$]T> () {}
}";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"AttributeUsage");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodParamAttribute()
        {
            var content = @"class CL{
    void Method ([$$]int i) {}
}";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"AttributeUsage");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceName1()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"namespace $$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"namespace $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceName2()
        {
            VerifyItemIsAbsent(@"namespace $$", @"String");
            VerifyItemIsAbsent(@"namespace $$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnderNamespace()
        {
            VerifyItemIsAbsent(@"namespace NS { $$", @"String");
            VerifyItemIsAbsent(@"namespace NS { $$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OutsideOfType1()
        {
            VerifyItemIsAbsent(@"namespace NS {
class CL {}
$$", @"String");
            VerifyItemIsAbsent(@"namespace NS {
class CL {}
$$", @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OutsideOfType2()
        {
            var content = @"namespace NS {
class CL {}
$$";
            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionInsideProperty()
        {
            var content = @"class C
{
    private string name;
    public string Name
    {
        set
        {
            name = $$";
            VerifyItemExists(content, @"value");
            VerifyItemExists(content, @"C");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterDot()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingAlias()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"using MyType = $$"), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", @"using MyType = $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMember()
        {
            var content = @"class CL {
    $$
";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMemberAccessibility()
        {
            var content = @"class CL {
    public $$
";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BadStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameter()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<$$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<$$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameterList()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T, $$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T, $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionTypePart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ObjectCreationExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayCreationExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StackAllocArrayCreationExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseTypeOptPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClause()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DeclarationStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VariableDeclaration()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementNoToken()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CatchDeclaration()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldDeclaration()
        {
            var content = @"class CL {
    $$ i";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventFieldDeclaration()
        {
            var content = @"class CL {
    event $$";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclaration()
        {
            var content = @"class CL {
    explicit operator $$";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclarationNoToken()
        {
            var content = @"class CL {
    explicit $$";
            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PropertyDeclaration()
        {
            var content = @"class CL {
    $$ Prop {";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventDeclaration()
        {
            var content = @"class CL {
    event $$ Event {";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IndexerDeclaration()
        {
            var content = @"class CL {
    $$ this";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Parameter()
        {
            var content = @"class CL {
    void Method($$";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayType()
        {
            var content = @"class CL {
    $$ [";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PointerType()
        {
            var content = @"class CL {
    $$ *";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NullableType()
        {
            var content = @"class CL {
    $$ ?";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DelegateDeclaration()
        {
            var content = @"class CL {
    delegate $$";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodDeclaration()
        {
            var content = @"class CL {
    $$ M(";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OperatorDeclaration()
        {
            var content = @"class CL {
    $$ operator";
            VerifyItemExists(AddUsingDirectives("using System;", content), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", content), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ParenthesizedExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvocationExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ElementAccessExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Argument()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseInPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LetClauseExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OrderingExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SelectClauseExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExpressionStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ReturnStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ThrowStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"System");
        }

        [WorkItem(760097)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void YieldReturnStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStatementExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LockStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EqualsValueClause()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementInitializersPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementConditionOptPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementIncrementorsPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoStatementConditionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhileStatementConditionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayRankSpecifierSizesPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PrefixUnaryExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PostfixUnaryExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionLeftPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionRightPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionLeftPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionRightPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionConditionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenTruePart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenFalsePart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseInExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseLeftExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseRightExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhereClauseConditionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseGroupExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseByExpressionPart()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IfStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchStatement()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchLabelCase()
        {
            var content = @"switch(i)
    {
        case $$";
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InitializerExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClause()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseList()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseAnotherWhere()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"System");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeSymbolOfTypeParameterConstraintClause1()
        {
            VerifyItemExists(@"class CL<T> where $$", @"T");
            VerifyItemExists(@"class CL{ delegate void F<T>() where $$} ", @"T");
            VerifyItemExists(@"class CL{ void F<T>() where $$", @"T");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeSymbolOfTypeParameterConstraintClause2()
        {
            VerifyItemIsAbsent(@"class CL<T> where $$", @"System");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T> where $$"), @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeSymbolOfTypeParameterConstraintClause3()
        {
            VerifyItemIsAbsent(@"class CL<T1> { void M<T2> where $$", @"T1");
            VerifyItemExists(@"class CL<T1> { void M<T2>() where $$", @"T2");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList1()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL : $$"), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL : $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList2()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL : B, $$"), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", @"class CL : B, $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseListWhere()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AliasedName()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AliasedNamespace()
        {
            VerifyItemExists(AddUsingDirectives("using S = System;", AddInsideMethod(@"S.$$")), @"String");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AliasedType()
        {
            VerifyItemExists(AddUsingDirectives("using S = System.String;", AddInsideMethod(@"S.$$")), @"Empty");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstructorInitializer()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class C { C() : $$"), @"String");
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class C { C() : $$"), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Typeof1()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Typeof2()
        {
            VerifyItemIsAbsent(AddInsideMethod(@"var x = 0; typeof($$"), @"x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Sizeof1()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Sizeof2()
        {
            VerifyItemIsAbsent(AddInsideMethod(@"var x = 0; sizeof($$"), @"x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Default1()
        {
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"String");
            VerifyItemExists(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"System");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Default2()
        {
            VerifyItemIsAbsent(AddInsideMethod(@"var x = 0; default($$"), @"x");
        }

        [WorkItem(543819)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Checked()
        {
            VerifyItemExists(AddInsideMethod(@"var x = 0; checked($$"), @"x");
        }

        [WorkItem(543819)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Unchecked()
        {
            VerifyItemExists(AddInsideMethod(@"var x = 0; unchecked($$"), @"x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Locals()
        {
            VerifyItemExists(@"class c { void M() { string foo; $$", "foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Parameters()
        {
            VerifyItemExists(@"class c { void M(string args) { $$", "args");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommonTypesInNewExpressionContext()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"class c { void M() { new $$"), "Exception");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoCompletionForUnboundTypes()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class c { void M() { foo.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParametersInTypeOf()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class c { void M(int x) { typeof($$"), "x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParametersInDefault()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"class c { void M(int x) { default($$"), "x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParametersInSizeOf()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"public class C { void M(int x) { unsafe { sizeof($$"), "x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParametersInGenericParameterList()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"public class Generic<T> { void M(int x) { Generic<$$"), "x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoMembersAfterNullLiteral()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"public class C { void M() { null.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterTrueLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { true.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterFalseLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { false.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterCharLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { 'c'.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterStringLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { """".$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterVerbatimStringLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { @"""".$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterNumericLiteral()
        {
            // NOTE: the Completion command handler will suppress this case if the user types '.',
            // but we still need to show members if the user specifically invokes statement completion here.
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { 2.$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoMembersAfterParenthesizedNullLiteral()
        {
            VerifyItemIsAbsent(AddUsingDirectives("using System;", @"public class C { void M() { (null).$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedTrueLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { (true).$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedFalseLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { (false).$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedCharLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { ('c').$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedStringLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { ("""").$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedVerbatimStringLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { (@"""").$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterParenthesizedNumericLiteral()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { (2).$$"), "Equals");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MembersAfterArithmeticExpression()
        {
            VerifyItemExists(AddUsingDirectives("using System;", @"public class C { void M() { (1 + 1).$$"), "Equals");
        }

        [WorkItem(539332)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceTypesAvailableInUsingAlias()
        {
            VerifyItemExists(@"using S = System.$$", "String");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedMember1()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedMember2()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        this.$$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedMember3()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Foo() { }
}
class B : A
{
    void Bar()
    {
        base.$$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
            VerifyItemIsAbsent(markup, "Bar");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedStaticMember1()
        {
            var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedStaticMember2()
        {
            var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        B.$$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedStaticMember3()
        {
            var markup = @"
class A
{
     private static void Hidden() { }
     protected static void Foo() { }
}
class B : A
{
    void Bar()
    {
        A.$$
    }
}
";
            VerifyItemIsAbsent(markup, "Hidden");
            VerifyItemExists(markup, "Foo");
        }

        [WorkItem(539812)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InheritedInstanceAndStaticMembers()
        {
            var markup = @"
class A
{
     private static void HiddenStatic() { }
     protected static void FooStatic() { }

     private void HiddenInstance() { }
     protected void FooInstance() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            VerifyItemIsAbsent(markup, "HiddenStatic");
            VerifyItemExists(markup, "FooStatic");
            VerifyItemIsAbsent(markup, "HiddenInstance");
            VerifyItemExists(markup, "FooInstance");
        }

        [WorkItem(540155)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForLoopIndexer1()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i = 0; $$
";
            VerifyItemExists(markup, "i");
        }

        [WorkItem(540155)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForLoopIndexer2()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i = 0; i < 10; $$
";
            VerifyItemExists(markup, "i");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersAfterType1()
        {
            var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

            VerifyItemIsAbsent(markup, "Dispose");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersAfterType2()
        {
            var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
            VerifyItemIsAbsent(markup, "Dispose");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersAfterType3()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

            VerifyItemIsAbsent(markup, "Dispose");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersAfterType4()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

            VerifyItemIsAbsent(markup, "Dispose");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersAfterType1()
        {
            var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

            VerifyItemExists(markup, "ReferenceEquals");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersAfterType2()
        {
            var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
            VerifyItemIsAbsent(markup, "ReferenceEquals");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersAfterType3()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

            VerifyItemExists(markup, "ReferenceEquals");
        }

        [WorkItem(540012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersAfterType4()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

            VerifyItemIsAbsent(markup, "ReferenceEquals");
        }

        [WorkItem(540197)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParametersInClass()
        {
            var markup = @"
class C<T, R>
{
    $$
}
";
            VerifyItemExists(markup, "T");
        }

        [WorkItem(540212)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterRefInLambda()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        Func<int, int> f = (ref $$
    }
}
";
            VerifyItemExists(markup, "String");
        }

        [WorkItem(540212)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterOutInLambda()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        Func<int, int> f = (out $$
    }
}
";
            VerifyItemExists(markup, "String");
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType1()
        {
            var markup = @"
class Q
{
    $$
    class R
    {

    }
}
";
            VerifyItemExists(markup, "Q");
            VerifyItemExists(markup, "R");
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType2()
        {
            var markup = @"
class Q
{
    class R
    {
        $$
    }
}
";
            VerifyItemExists(markup, "Q");
            VerifyItemExists(markup, "R");
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType3()
        {
            var markup = @"
class Q
{
    class R
    {
    }
    $$
}
";
            VerifyItemExists(markup, "Q");
            VerifyItemExists(markup, "R");
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType4_Regular()
        {
            var markup = @"
class Q
{
    class R
    {
    }
}
$$"; // At EOF
            VerifyItemIsAbsent(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType4_Script()
        {
            var markup = @"
class Q
{
    class R
    {
    }
}
$$"; // At EOF
            VerifyItemExists(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            VerifyItemIsAbsent(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType5()
        {
            var markup = @"
class Q
{
    class R
    {
    }
    $$"; // At EOF
            VerifyItemExists(markup, "Q");
            VerifyItemExists(markup, "R");
        }

        [WorkItem(539217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedType6()
        {
            var markup = @"
class Q
{
    class R
    {
        $$"; // At EOF
            VerifyItemExists(markup, "Q");
            VerifyItemExists(markup, "R");
        }

        [WorkItem(540574)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AmbiguityBetweenTypeAndLocal()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public void foo() {
        int i = 5;
        i.$$
        List<string> ml = new List<string>();
    }
}";

            VerifyItemExists(markup, "CompareTo");
        }

        [WorkItem(540750)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionAfterNewInScript()
        {
            var markup = @"
using System;

new $$";

            VerifyItemExists(markup, "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(540933)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExtensionMethodsInScript()
        {
            var markup = @"
using System.Linq;
var a = new int[] { 1, 2 };
a.$$";

            VerifyItemExists(markup, "ElementAt<>", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(541019)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExpressionsInForLoopInitializer()
        {
            var markup = @"
public class C
{
    public void M()
    {
        int count = 0;
        for ($$
";

            VerifyItemExists(markup, "count");
        }

        [WorkItem(541108)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterLambdaExpression1()
        {
            var markup = @"
public class C
{
    public void M()
    {
        System.Func<int, int> f = arg => { arg = 2; return arg; }.$$
    }
}
";

            VerifyItemIsAbsent(markup, "ToString");
        }

        [WorkItem(541108)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterLambdaExpression2()
        {
            var markup = @"
public class C
{
    public void M()
    {
        ((System.Func<int, int>)(arg => { arg = 2; return arg; })).$$
    }
}
";

            VerifyItemExists(markup, "ToString");
            VerifyItemExists(markup, "Invoke");
        }

        [WorkItem(541216)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InMultiLineCommentAtEndOfFile()
        {
            var markup = @"
using System;
/*$$";

            VerifyItemIsAbsent(markup, "Console", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(541218)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParametersAtEndOfFile()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Outer<T>
{
class Inner<U>
{
static void F(T t, U u)
{
return;
}
public static void F(T t)
{
Outer<$$";

            VerifyItemExists(markup, "T");
        }

        [WorkItem(552717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelInCaseSwitchAbsentForCase()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

            VerifyItemIsAbsent(markup, "case 0:");
        }

        [WorkItem(552717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelInCaseSwitchAbsentForDefaultWhenAbsent()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

            VerifyItemIsAbsent(markup, "default:");
        }

        [WorkItem(552717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelInCaseSwitchPresentForDefault()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            default:
                goto $$";

            VerifyItemExists(markup, "default:");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelAfterGoto1()
        {
            var markup = @"
class Program
{
    static void Main()
    {
    Foo:
        int Foo;
        goto $$";

            VerifyItemExists(markup, "Foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelAfterGoto2()
        {
            var markup = @"
class Program
{
    static void Main()
    {
    Foo:
        int Foo;
        goto Foo $$";

            VerifyItemIsAbsent(markup, "Foo");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeName()
        {
            var markup = @"
using System;
[$$";

            VerifyItemExists(markup, "CLSCompliant");
            VerifyItemIsAbsent(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterSpecifier()
        {
            var markup = @"
using System;
[assembly:$$
";

            VerifyItemExists(markup, "CLSCompliant");
            VerifyItemIsAbsent(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameInAttributeList()
        {
            var markup = @"
using System;
[CLSCompliant, $$";

            VerifyItemExists(markup, "CLSCompliant");
            VerifyItemIsAbsent(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameBeforeClass()
        {
            var markup = @"
using System;
[$$
class C { }";

            VerifyItemExists(markup, "CLSCompliant");
            VerifyItemIsAbsent(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterSpecifierBeforeClass()
        {
            var markup = @"
using System;
[assembly:$$
class C { }";

            VerifyItemExists(markup, "CLSCompliant");
            VerifyItemIsAbsent(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameInAttributeArgumentList()
        {
            var markup = @"
using System;
[CLSCompliant($$
class C { }";

            VerifyItemExists(markup, "CLSCompliantAttribute");
            VerifyItemIsAbsent(markup, "CLSCompliant");
        }

        [WorkItem(542225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameInsideClass()
        {
            var markup = @"
using System;
class C { $$ }";

            VerifyItemExists(markup, "CLSCompliantAttribute");
            VerifyItemIsAbsent(markup, "CLSCompliant");
        }

        [WorkItem(542954)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceAliasInAttributeName1()
        {
            var markup = @"
using Alias = System;

[$$
class C { }";

            VerifyItemExists(markup, "Alias");
        }

        [WorkItem(542954)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceAliasInAttributeName2()
        {
            var markup = @"
using Alias = Foo;

namespace Foo { }

[$$
class C { }";

            VerifyItemIsAbsent(markup, "Alias");
        }

        [WorkItem(542954)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceAliasInAttributeName3()
        {
            var markup = @"
using Alias = Foo;

namespace Foo { class A : System.Attribute { } }

[$$
class C { }";

            VerifyItemExists(markup, "Alias");
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterNamespace()
        {
            var markup = @"
namespace Test
{
    class MyAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
            VerifyItemExists(markup, "My");
            VerifyItemIsAbsent(markup, "MyAttribute");
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterNamespace2()
        {
            var markup = @"
namespace Test
{
    namespace Two
    {
        class MyAttribute : System.Attribute { }
        [Test.Two.$$
        class Program { }
    }
}";
            VerifyItemExists(markup, "My");
            VerifyItemIsAbsent(markup, "MyAttribute");
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [$$
    class Program { }
}";
            VerifyItemExists(markup, "namespaceAttribute");
            VerifyItemIsAbsent(markup, "namespace");
            VerifyItemIsAbsent(markup, "@namespace");
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterNamespaceWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
            VerifyItemExists(markup, "namespaceAttribute");
            VerifyItemIsAbsent(markup, "namespace");
            VerifyItemIsAbsent(markup, "@namespace");
        }

        [WpfFact]
        [WorkItem(545348)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void KeywordsUsedAsLocals()
        {
            var markup = @"
class C
{
    void M()
    {
        var error = 0;
        var method = 0;
        var @int = 0;
        Console.Write($$
    }
}";

            // preprocessor keyword
            VerifyItemExists(markup, "error");
            VerifyItemIsAbsent(markup, "@error");

            // contextual keyword
            VerifyItemExists(markup, "method");
            VerifyItemIsAbsent(markup, "@method");

            // full keyword
            VerifyItemExists(markup, "@int");
            VerifyItemIsAbsent(markup, "int");
        }

        [WpfFact]
        [WorkItem(545348)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void QueryContextualKeywords1()
        {
            var markup = @"
class C
{
    void M()
    {
        var from = new[]{1,2,3};
        var r = from x in $$
    }
}";

            VerifyItemExists(markup, "@from");
            VerifyItemIsAbsent(markup, "from");
        }

        [WpfFact]
        [WorkItem(545348)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void QueryContextualKeywords2()
        {
            var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where $$ == @where.Length
                select @from;
    }
}";

            VerifyItemExists(markup, "@from");
            VerifyItemIsAbsent(markup, "from");
            VerifyItemExists(markup, "@where");
            VerifyItemIsAbsent(markup, "where");
        }

        [WpfFact]
        [WorkItem(545348)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void QueryContextualKeywords3()
        {
            var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where @from == @where.Length
                select $$;
    }
}";

            VerifyItemExists(markup, "@from");
            VerifyItemIsAbsent(markup, "from");
            VerifyItemExists(markup, "@where");
            VerifyItemIsAbsent(markup, "where");
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterGlobalAlias()
        {
            var markup = @"
class MyAttribute : System.Attribute { }
[global::$$
class Program { }";
            VerifyItemExists(markup, "My", sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(markup, "MyAttribute", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact]
        [WorkItem(545121)]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public void AttributeNameAfterGlobalAliasWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
class namespaceAttribute : System.Attribute { }
[global::$$
class Program { }";
            VerifyItemExists(markup, "namespaceAttribute", sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(markup, "namespace", sourceCodeKind: SourceCodeKind.Regular);
            VerifyItemIsAbsent(markup, "@namespace", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(542230)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void RangeVariableInQuerySelect()
        {
            var markup = @"
using System.Linq;
class P
{
    void M()
    {
        var src = new string[] { ""Foo"", ""Bar"" };
        var q = from x in src
                select x.$$";

            VerifyItemExists(markup, "Length");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInSwitchCase()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case $$";

            VerifyItemExists(markup, "MAX_SIZE");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInSwitchGotoCase()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case MAX_SIZE:
                break;
            case FOO:
                goto case $$";

            VerifyItemExists(markup, "MAX_SIZE");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInEnumMember()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    enum E
    {
        A = $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInAttribute1()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage($$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInAttribute2()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(FOO, $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInAttribute3()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(validOn: $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInAttribute4()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    [System.AttributeUsage(AllowMultiple = $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInParameterDefaultValue()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    void M(int x = $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInConstField()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    const int BAR = $$";

            VerifyItemExists(markup, "FOO");
        }

        [WorkItem(542429)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstantsInConstLocal()
        {
            var markup = @"
class C
{
    public const int FOO = 0;
    void M()
    {
        const int BAR = $$";

            VerifyItemExists(markup, "FOO");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionWith1Overload()
        {
            var markup = @"
class C
{
    void M(int i) { }
    void M()
    {
        $$";

            VerifyItemExists(markup, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 1 {FeaturesResources.Overload})");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionWith2Overloads()
        {
            var markup = @"
class C
{
    void M(int i) { }
    void M(out int i) { }
    void M()
    {
        $$";

            VerifyItemExists(markup, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 2 {FeaturesResources.Overloads})");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionWith1GenericOverload()
        {
            var markup = @"
class C
{
    void M<T>(T i) { }
    void M<T>()
    {
        $$";

            VerifyItemExists(markup, "M<>", expectedDescriptionOrNull: $"void C.M<T>(T i) (+ 1 {FeaturesResources.GenericOverload})");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionWith2GenericOverloads()
        {
            var markup = @"
class C
{
    void M<T>(int i) { }
    void M<T>(out int i) { }
    void M<T>()
    {
        $$";

            VerifyItemExists(markup, "M<>", expectedDescriptionOrNull: $"void C.M<T>(int i) (+ 2 {FeaturesResources.GenericOverloads})");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionNamedGenericType()
        {
            var markup = @"
class C<T>
{
    void M()
    {
        $$";

            VerifyItemExists(markup, "C<>", expectedDescriptionOrNull: "class C<T>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionParameter()
        {
            var markup = @"
class C<T>
{
    void M(T foo)
    {
        $$";

            VerifyItemExists(markup, "foo", expectedDescriptionOrNull: $"({FeaturesResources.Parameter}) T foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionGenericTypeParameter()
        {
            var markup = @"
class C<T>
{
    void M()
    {
        $$";

            VerifyItemExists(markup, "T", expectedDescriptionOrNull: $"T {FeaturesResources.In} C<T>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionAnonymousType()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = new { };
        $$
";

            var expectedDescription =
$@"({FeaturesResources.LocalVariable}) 'a a

{FeaturesResources.AnonymousTypes}
    'a {FeaturesResources.Is} new {{  }}";

            VerifyItemExists(markup, "a", expectedDescription);
        }

        [WorkItem(543288)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterNewInAnonymousType()
        {
            var markup = @"
class Program {
    string field = 0;
    static void Main()     {
        var an = new {  new $$  }; 
    }
}
";

            VerifyItemExists(markup, "Program");
        }

        [WorkItem(543601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceFieldsInStaticMethod()
        {
            var markup = @"
class C
{
    int x = 0;
    static void M()
    {
        $$
    }
}
";

            VerifyItemIsAbsent(markup, "x");
        }

        [WorkItem(543601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceFieldsInStaticFieldInitializer()
        {
            var markup = @"
class C
{
    int x = 0;
    static int y = $$
}
";

            VerifyItemIsAbsent(markup, "x");
        }

        [WorkItem(543601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticFieldsInStaticMethod()
        {
            var markup = @"
class C
{
    static int x = 0;
    static void M()
    {
        $$
    }
}
";

            VerifyItemExists(markup, "x");
        }

        [WorkItem(543601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticFieldsInStaticFieldInitializer()
        {
            var markup = @"
class C
{
    static int x = 0;
    static int y = $$
}
";

            VerifyItemExists(markup, "x");
        }

        [WorkItem(543680)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceFieldsFromOuterClassInInstanceMethod()
        {
            var markup = @"
class outer
{
    int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

            VerifyItemIsAbsent(markup, "i");
        }

        [WorkItem(543680)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticFieldsFromOuterClassInInstanceMethod()
        {
            var markup = @"
class outer
{
    static int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

            VerifyItemExists(markup, "i");
        }

        [WorkItem(543104)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OnlyEnumMembersInEnumMemberAccess()
        {
            var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        x.$$
    }
}
";

            VerifyItemExists(markup, "a");
            VerifyItemExists(markup, "b");
            VerifyItemExists(markup, "c");
            VerifyItemIsAbsent(markup, "Equals");
        }

        [WorkItem(543104)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoEnumMembersInEnumLocalAccess()
        {
            var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        var y = x.a;
        y.$$
    }
}
";

            VerifyItemIsAbsent(markup, "a");
            VerifyItemIsAbsent(markup, "b");
            VerifyItemIsAbsent(markup, "c");
            VerifyItemExists(markup, "Equals");
        }

        [WorkItem(529138)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterLambdaParameterDot()
        {
            var markup = @"
using System;
using System.Linq;
class A
{
    public event Func<String, String> E;
}
 
class Program
{
    static void Main(string[] args)
    {
        new A().E += ss => ss.$$
    }
}
";

            VerifyItemExists(markup, "Substring");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAtRoot_Interactive()
        {
            VerifyItemIsAbsent(
@"$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterClass_Interactive()
        {
            VerifyItemIsAbsent(
@"class C { }
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterGlobalStatement_Interactive()
        {
            VerifyItemIsAbsent(
@"System.Console.WriteLine();
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyItemIsAbsent(
@"int i = 0;
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotInUsingAlias()
        {
            VerifyItemIsAbsent(
@"using Foo = $$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotInEmptyStatement()
        {
            VerifyItemIsAbsent(AddInsideMethod(
@"$$"),
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueInsideSetter()
        {
            VerifyItemExists(
@"class C {
    int Foo {
      set {
        $$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueInsideAdder()
        {
            VerifyItemExists(
@"class C {
    event int Foo {
      add {
        $$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueInsideRemover()
        {
            VerifyItemExists(
@"class C {
    event int Foo {
      remove {
        $$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterDot()
        {
            VerifyItemIsAbsent(
@"class C {
    int Foo {
      set {
        this.$$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterArrow()
        {
            VerifyItemIsAbsent(
@"class C {
    int Foo {
      set {
        a->$$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotAfterColonColon()
        {
            VerifyItemIsAbsent(
@"class C {
    int Foo {
      set {
        a::$$",
"value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ValueNotInGetter()
        {
            VerifyItemIsAbsent(
@"class C {
    int Foo {
      get {
        $$",
"value");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterNullableType()
        {
            VerifyItemIsAbsent(
@"class C {
    void M() {
        int foo = 0;
        C? $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterNullableTypeAlias()
        {
            VerifyItemIsAbsent(
@"using A = System.Int32;
class C {
    void M() {
        int foo = 0;
        A? $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNullableTypeAndPartialIdentifier()
        {
            VerifyItemIsAbsent(
@"class C {
    void M() {
        int foo = 0;
        C? f$$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterQuestionMarkInConditional()
        {
            VerifyItemExists(
@"class C {
    void M() {
        bool b = false;
        int foo = 0;
        b? $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterQuestionMarkAndPartialIdentifierInConditional()
        {
            VerifyItemExists(
@"class C {
    void M() {
        bool b = false;
        int foo = 0;
        b? f$$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterPointerType()
        {
            VerifyItemIsAbsent(
@"class C {
    void M() {
        int foo = 0;
        C* $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterPointerTypeAlias()
        {
            VerifyItemIsAbsent(
@"using A = System.Int32;
class C {
    void M() {
        int foo = 0;
        A* $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterPointerTypeAndPartialIdentifier()
        {
            VerifyItemIsAbsent(
@"class C {
    void M() {
        int foo = 0;
        C* f$$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsteriskInMultiplication()
        {
            VerifyItemExists(
@"class C {
    void M() {
        int i = 0;
        int foo = 0;
        i* $$",
"foo");
        }

        [WorkItem(544205)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsteriskAndPartialIdentifierInMultiplication()
        {
            VerifyItemExists(
@"class C {
    void M() {
        int i = 0;
        int foo = 0;
        i* f$$",
"foo");
        }

        [WorkItem(543868)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterEventFieldDeclaredInSameType()
        {
            VerifyItemExists(
@"class C {
    public event System.EventHandler E;
    void M() {
        E.$$",
"Invoke");
        }

        [WorkItem(543868)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterFullEventDeclaredInSameType()
        {
            VerifyItemIsAbsent(
@"class C {
        public event System.EventHandler E { add { } remove { } }
    void M() {
        E.$$",
"Invoke");
        }

        [WorkItem(543868)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterEventDeclaredInDifferentType()
        {
            VerifyItemIsAbsent(
@"class C {
    void M() {
        System.Console.CancelKeyPress.$$",
"Invoke");
        }

        [WorkItem(544219)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectInitializerMemberContext()
        {
            VerifyItemIsAbsent(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$",
"x");
        }

        [WorkItem(544219)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPointerMemberAccess()
        {
            VerifyItemExists(@"
struct MyStruct
{
    public int MyField;
}

class Program
{
    static unsafe void Main(string[] args)
    {
        MyStruct s = new MyStruct();
        MyStruct* ptr = &s;
        ptr->$$
    }}",
"MyField");
        }

        // After @ both X and XAttribute are legal. We think this is an edge case in the language and
        // are not fixing the bug 11931. This test captures that XAttribute doesn't show up indeed.
        [WorkItem(11931, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void VerbatimAttributes()
        {
            var code = @"
using System;
public class X : Attribute
{ }
 
public class XAttribute : Attribute
{ }
 
 
[@X$$]
class Class3 { }
";
            VerifyItemExists(code, "X");
            AssertEx.Throws<Xunit.Sdk.TrueException>(() => VerifyItemExists(code, "XAttribute"));
        }

        [WorkItem(544928)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForLoopIncrementor1()
        {
            VerifyItemExists(@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; $$
    }
}
", "Console");
        }

        [WorkItem(544928)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForLoopIncrementor2()
        {
            VerifyItemExists(@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; Console.WriteLine(), $$
    }
}
", "Console");
        }

        [WorkItem(544931)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForLoopInitializer1()
        {
            VerifyItemExists(@"
using System;
 
class Program
{
    static void Main()
    {
        for ($$
    }
}
", "Console");
        }

        [WorkItem(544931)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForLoopInitializer2()
        {
            VerifyItemExists(@"
using System;
 
class Program
{
    static void Main()
    {
        for (Console.WriteLine(), $$
    }
}
", "Console");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableInItsDeclaration()
        {
            // "int foo = foo = 1" is a legal declaration
            VerifyItemExists(@"
class Program
{
    void M()
    {
        int foo = $$
    }
}", "foo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableInItsDeclarator()
        {
            // "int bar = bar = 1" is legal in a declarator
            VerifyItemExists(@"
class Program
{
    void M()
    {
        int foo = 0, int bar = $$, int baz = 0;
    }
}", "bar");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableNotBeforeDeclaration()
        {
            VerifyItemIsAbsent(@"
class Program
{
    void M()
    {
        $$
        int foo = 0;
    }
}", "foo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableNotBeforeDeclarator()
        {
            VerifyItemIsAbsent(@"
class Program
{
    void M()
    {
        int foo = $$, bar = 0;
    }
}", "bar");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableAfterDeclarator()
        {
            VerifyItemExists(@"
class Program
{
    void M()
    {
        int foo = 0, int bar = $$
    }
}", "foo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalVariableAsOutArgumentInInitializerExpression()
        {
            VerifyItemExists(@"
class Program
{
    void M()
    {
        int foo = Bar(out $$
    }
    int Bar(out int x)
    {
        x = 3;
        return 5;
    }
}", "foo");
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static void Bar() 
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_Overloads_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(int x) 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(int x) 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Method_Overloads_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Foo.$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(int x) 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_ExtensionMethod_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Foo foo, int x)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_ExtensionMethod_BrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(this Foo foo, int x)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_ExtensionMethod_BrowsableAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static void Bar(this Foo foo, int x)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_ExtensionMethod_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Foo foo, int x)
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(this Foo foo, int x, int y)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public void Bar(int x)
    {
    }
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Foo foo, int x, int y)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x)
    {
    }
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Foo foo, int x, int y)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_SameSigExtensionMethodAndMethod_InstanceMethodBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x)
    {
    }
}

public static class FooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Foo foo, int x)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OverriddenSymbolsFilteredFromCompletionList()
        {
            var markup = @"
class Program
{
    void M()
    {
        D d = new D();
        d.$$
    }
}";

            var referencedCode = @"
public class B
{
    public virtual void Foo(int original) 
    {
    }
}

public class D : B
{
    public override void Foo(int derived) 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        C c = new C();
        c.$$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class C
{
    public void Foo() 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        D d = new D();
        d.$$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class B
{
    public void Foo() 
    {
    }
}

public class D : B
{
    public void Foo(int x)
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
        {
            var markup = @"
class Program : B
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
public class B
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo() 
    {
    }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Foo(T t) { }
    public void Foo(int i) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    public void Foo(int i) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(int i) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(int i) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    public void Foo(T t) { }
    public void Foo(U u) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    public void Foo(U u) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Foo(U u) { }
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Field_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int bar;
}";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Field_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Field_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(522440)]
        [WorkItem(674611)]
        [WpfFact(Skip = "674611"), Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Property_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int Bar {get; set;}
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    public int Bar {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        get { return 5; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        set { }
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Property_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public int Bar {get; set;}
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Property_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public int Bar {get; set;}
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Constructor_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Constructor_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public Foo()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Constructor_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public Foo()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Constructor_MixedOverloads1()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo()
    {
    }

    public Foo(int x)
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Constructor_MixedOverloads2()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo()
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Foo(int x)
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Event_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public event Handler Changed;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Event_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public event Handler Changed;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Event_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public event Handler Changed;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Delegate_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public delegate void Handler();";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Delegate_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public delegate void Handler();";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Delegate_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public delegate void Handler();";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateNever_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class Foo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAlways_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public class Foo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_BrowsableStateAdvanced_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public class Foo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Class_IgnoreBaseClassBrowsableNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
public class Foo : Bar
{
}

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Bar
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Struct_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public struct Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Enum_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public enum Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Enum_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public enum Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Enum_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public enum Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public interface Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_CrossLanguage_CStoVB_Always()
        {
            var markup = @"
class Program
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Foo
End Class";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_CrossLanguage_CStoVB_Never()
        {
            var markup = @"
class Program
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Foo
End Class";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 0,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibType_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed))]
public class Foo
{
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Foo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable))]
    public void Bar()
    {
    }
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EditorBrowsable_TypeLibVar_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Foo().$$
    }
}";

            var referencedCode = @"
public class Foo
{
    [System.Runtime.InteropServices.TypeLibVar((short)(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable))]
    public int bar;
}";
            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(545557)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestColorColor1()
        {
            var markup = @"
class A
{
    static void Foo() { }
    void Bar() { }
 
    static void Main()
    {
        A A = new A();
        A.$$
    }
}";

            VerifyItemExists(markup, "Foo");
            VerifyItemExists(markup, "Bar");
        }

        [WorkItem(545647)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestLaterLocalHidesType1()
        {
            var markup = @"
using System;
class C
{
    public static void Main()
    {
        $$
        Console.WriteLine();
    }
}";

            VerifyItemExists(markup, "Console");
        }

        [WorkItem(545647)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestLaterLocalHidesType2()
        {
            var markup = @"
using System;
class C
{
    public static void Main()
    {
        C$$
        Console.WriteLine();
    }
}";

            VerifyItemExists(markup, "Console");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestIndexedProperty()
        {
            var markup = @"class Program
{
    void M()
    {
            CCC c = new CCC();
            c.$$
    }
}";

            // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
            var referencedCode = @"Imports System.Runtime.InteropServices

<ComImport()>
<GuidAttribute(CCC.ClassId)>
Public Class CCC

#Region ""COM GUIDs""
    Public Const ClassId As String = ""9d965fd2-1514-44f6-accd-257ce77c46b0""
    Public Const InterfaceId As String = ""a9415060-fdf0-47e3-bc80-9c18f7f39cf6""
    Public Const EventsId As String = ""c6a866a5-5f97-4b53-a5df-3739dc8ff1bb""
# End Region

            ''' <summary>
    ''' An index property from VB
    ''' </summary>
    ''' <param name=""p1"">p1 is an integer index</param>
    ''' <returns>A string</returns>
    Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class";

            VerifyItemInEditorBrowsableContexts(
                markup: markup,
                referencedCode: referencedCode,
                item: "IndexProp",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic);
        }

        [WorkItem(546841)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestDeclarationAmbiguity()
        {
            var markup = @"
using System;

class Program
{
    void Main()
    {
        Environment.$$
        var v;
    }
}";

            VerifyItemExists(markup, "CommandLine");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestCursorOnClassCloseBrace()
        {
            var markup = @"
using System;

class Outer
{
    class Inner { }

$$}";

            VerifyItemExists(markup, "Inner");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsync1()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async $$
}";

            VerifyItemExists(markup, "Task");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterAsync2()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    public async T$$
}";

            VerifyItemExists(markup, "Task");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAfterAsyncInMethodBody()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    void foo()
    {
        var x = async $$
    }
}";

            VerifyItemIsAbsent(markup, "Task");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAwaitable1()
        {
            var markup = @"
class Program
{
    void foo()
    {
        $$
    }
}";

            VerifyItemWithMscorlib45(markup, "foo", "void Program.foo()", "C#");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NotAwaitable2()
        {
            var markup = @"
class Program
{
    async void foo()
    {
        $$
    }
}";

            VerifyItemWithMscorlib45(markup, "foo", "void Program.foo()", "C#");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Awaitable1()
        {
            var markup = @"
using System.Threading;
using System.Threading.Tasks;

class Program
{
    async Task foo()
    {
        $$
    }
}";

            var description = $@"({CSharpFeaturesResources.Awaitable}) Task Program.foo()
{WorkspacesResources.Usage}
  {CSharpFeaturesResources.Await} foo();";

            VerifyItemWithMscorlib45(markup, "foo", description, "C#");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Awaitable2()
        {
            var markup = @"
using System.Threading.Tasks;

class Program
{
    async Task<int> foo()
    {
        $$
    }
}";

            var description = $@"({CSharpFeaturesResources.Awaitable}) Task<int> Program.foo()
{WorkspacesResources.Usage}
  int x = {CSharpFeaturesResources.Await} foo();";

            VerifyItemWithMscorlib45(markup, "foo", description, "C#");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ObsoleteItem()
        {
            var markup = @"
using System;

class Program
{
    [Obsolete]
    public void foo()
    {
        $$
    }
}";
            VerifyItemExists(markup, "foo", $"[{CSharpFeaturesResources.Deprecated}] void Program.foo()");
        }

        [WorkItem(568986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoMembersOnDottingIntoUnboundType()
        {
            var markup = @"
class Program
{
    RegistryKey foo;
 
    static void Main(string[] args)
    {
        foo.$$
    }
}";
            VerifyNoItemsExist(markup);
        }

        [WorkItem(550717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeArgumentsInConstraintAfterBaselist()
        {
            var markup = @"
public class Foo<T> : System.Object where $$
{
}";
            VerifyItemExists(markup, "T");
        }

        [WorkItem(647175)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoDestructor()
        {
            var markup = @"
class C
{
    ~C()
    {
        $$
";
            VerifyItemIsAbsent(markup, "Finalize");
        }

        [WorkItem(669624)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExtensionMethodOnCovariantInterface()
        {
            var markup = @"
class Schema<T> { }

interface ISet<out T> { }

static class SetMethods
{
    public static void ForSchemaSet<T>(this ISet<Schema<T>> set) { }
}

class Context
{
    public ISet<T> Set<T>() { return null; }
}

class CustomSchema : Schema<int> { }

class Program
{
    static void Main(string[] args)
    {
        var set = new Context().Set<CustomSchema>();

        set.$$
";

            VerifyItemExists(markup, "ForSchemaSet<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(667752)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachInsideParentheses()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        foreach($$)
";

            VerifyItemExists(markup, "String");
        }

        [WorkItem(766869)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestFieldInitializerInP2P()
        {
            var markup = @"
class Class
{
    int i = Consts.$$;
}";

            var referencedCode = @"
public static class Consts
{
    public const int C = 1;
}";
            VerifyItemWithProjectReference(markup, referencedCode, "C", 1, LanguageNames.CSharp, LanguageNames.CSharp, false);
        }

        [WorkItem(834605)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowWithEqualsSign()
        {
            var markup = @"
class c { public int value {set; get; }}

class d
{
    void foo()
    {
       c foo = new c { value$$=
    }
}";

            VerifyNoItemsExist(markup);
        }

        [WorkItem(825661)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingAfterThisDotInStaticContext()
        {
            var markup = @"
class C
{
    void M1() { }

    static void M2()
    {
        this.$$
    }
}";

            VerifyNoItemsExist(markup);
        }

        [WorkItem(825661)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingAfterBaseDotInStaticContext()
        {
            var markup = @"
class C
{
    void M1() { }

    static void M2()
    {
        base.$$
    }
}";

            VerifyNoItemsExist(markup);
        }

        [WorkItem(858086)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoNestedTypeWhenDisplayingInstance()
        {
            var markup = @"
class C
{
    class D
    {
    }

    void M2()
    {
        new C().$$
    }
}";

            VerifyItemIsAbsent(markup, "D");
        }

        [WorkItem(876031)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CatchVariableInExceptionFilter()
        {
            var markup = @"
class C
{
    void M()
    {
        try
        {
        }
        catch (System.Exception myExn) when ($$";

            VerifyItemExists(markup, "myExn");
        }

        [WorkItem(849698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionAfterExternAlias()
        {
            var markup = @"
class C
{
    void foo()
    {
        global::$$
    }
}";

            VerifyItemExists(markup, "System", usePreviousCharAsTrigger: true);
        }

        [WorkItem(849698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExternAliasSuggested()
        {
            var markup = @"
extern alias Bar;
class C
{
    void foo()
    {
        $$
    }
}";
            VerifyItemWithAliasedMetadataReferences(markup, "Bar", "Bar", 1, "C#", "C#", false);
        }

        [WorkItem(635957)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ClassDestructor()
        {
            var markup = @"
class C
{
    class N
    {
    ~$$
    }
}";
            VerifyItemExists(markup, "N");
            VerifyItemIsAbsent(markup, "C");
        }

        [WorkItem(635957)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TildeOutsideClass()
        {
            var markup = @"
class C
{
    class N
    {
    }
}
~$$";
            VerifyNoItemsExist(markup, SourceCodeKind.Regular);
        }

        [WorkItem(635957)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StructDestructor()
        {
            var markup = @"
struct C
{
   ~$$
}";
            VerifyItemExists(markup, "C");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldAvailableInBothLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    int x;
    void foo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            VerifyItemInLinkedFiles(markup, "x", $"({FeaturesResources.Field}) int C.x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    int x;
#endif
    void foo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.Field}) int C.x\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}";

            VerifyItemInLinkedFiles(markup, "x", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldUnavailableInTwoLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    int x;
#endif
    void foo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.Field}) int C.x\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}";

            VerifyItemInLinkedFiles(markup, "x", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO,BAR"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    int x;
#endif

#if BAR
    void foo()
    {
        $$
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.Field}) int C.x\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}";

            VerifyItemInLinkedFiles(markup, "x", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnionOfItemsFromBothContexts()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""FOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if FOO
    int x;
#endif

#if BAR
    class G
    {
        public void DoGStuff() {}
    }
#endif
    void foo()
    {
        new G().$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"void G.DoGStuff()\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.NotAvailable)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj3", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}";

            VerifyItemInLinkedFiles(markup, "DoGStuff", expectedDescription);
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
        int xyz;
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.LocalVariable}) int xyz";
            VerifyItemInLinkedFiles(markup, "xyz", expectedDescription);
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LocalWarningInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""PROJ1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
#if PROJ1
        int xyz;
#endif
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.LocalVariable}) int xyz\r\n\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources.ProjectAvailability, "Proj2", FeaturesResources.NotAvailable)}\r\n\r\n{FeaturesResources.UseTheNavigationBarToSwitchContext}";
            VerifyItemInLinkedFiles(markup, "xyz", expectedDescription);
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LabelsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
LABEL:  int xyz;
        goto $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.Label}) LABEL";
            VerifyItemInLinkedFiles(markup, "LABEL", expectedDescription);
        }

        [WorkItem(1020944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void RangeVariablesValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System.Linq;
class C
{
    void M()
    {
        var x = from y in new[] { 1, 2, 3 } select $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.RangeVariable}) ? y";
            VerifyItemInLinkedFiles(markup, "y", expectedDescription);
        }

        [WorkItem(1063403)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodOverloadDifferencesIgnored()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif
#if TWO
    void Do(string x){}
#endif

    void Shared()
    {
        $$
    }

}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void C.Do(int x)";
            VerifyItemInLinkedFiles(markup, "Do", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodOverloadDifferencesIgnored_ExtensionMethod()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif

    void Shared()
    {
        this.$$
    }

}

public static class Extensions
{
#if TWO
    public static void Do (this C c, string x)
    {
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void C.Do(int x)";
            VerifyItemInLinkedFiles(markup, "Do", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodOverloadDifferencesIgnored_ExtensionMethod2()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""TWO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif

    void Shared()
    {
        this.$$
    }

}

public static class Extensions
{
#if TWO
    public static void Do (this C c, string x)
    {
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""ONE"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"({CSharpFeaturesResources.Extension}) void C.Do(string x)";
            VerifyItemInLinkedFiles(markup, "Do", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodOverloadDifferencesIgnored_ContainingType()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void Shared()
    {
        var x = GetThing();
        x.$$
    }

#if ONE
    private Methods1 GetThing()
    {
        return new Methods1();
    }
#endif

#if TWO
    private Methods2 GetThing()
    {
        return new Methods2();
    }
#endif
}

#if ONE
public class Methods1
{
    public void Do(string x) { }
}
#endif

#if TWO
public class Methods2
{
    public void Do(string x) { }
}
#endif
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void Methods1.Do(string x)";
            VerifyItemInLinkedFiles(markup, "Do", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SharedProjectFieldAndPropertiesTreatedAsIdentical()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    public int x;
#endif
#if TWO
    public int x {get; set;}
#endif
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"(field) int C.x";
            VerifyItemInLinkedFiles(markup, "x", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SharedProjectFieldAndPropertiesTreatedAsIdentical2()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if TWO
    public int x;
#endif
#if ONE
    public int x {get; set;}
#endif
    void foo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = "int C.x { get; set; }";
            VerifyItemInLinkedFiles(markup, "x", expectedDescription);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalAccessWalkUp()
        {
            var markup = @"
public class B
{
    public A BA;
    public B BB;
}

class A
{
    public A AA;
    public A AB;
    public int? x;

    public void foo()
    {
        A a = null;
        var q = a?.$$AB.BA.AB.BA;
    }
}";
            VerifyItemExists(markup, "AA", experimental: true);
            VerifyItemExists(markup, "AB", experimental: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalAccessNullableIsUnwrapped()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S? s;

    public void foo()
    {
        A a = null;
        var q = a?.s?.$$;
    }
}";
            VerifyItemExists(markup, "i", experimental: true);
            VerifyItemIsAbsent(markup, "value", experimental: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalAccessNullableIsUnwrapped2()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S? s;

    public void foo()
    {
        var q = s?.$$i?.ToString();
    }
}";
            VerifyItemExists(markup, "i", experimental: true);
            VerifyItemIsAbsent(markup, "value", experimental: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionAfterConditionalIndexing()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S[] s;

    public void foo()
    {
        A a = null;
        var q = a?.s?[$$;
    }
}";
            VerifyItemExists(markup, "System", experimental: true);
        }

        [WorkItem(1109319)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WithinChainOfConditionalAccesses()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        A a;
        var x = a?.$$b?.c?.d.e;
    }
}

class A { public B b; }
class B { public C c; }
class C { public D d; }
class D { public int e; }";
            VerifyItemExists(markup, "b");
        }

        [WorkItem(843466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedAttributeAccessibleOnSelf()
        {
            var markup = @"using System;
[My]
class X
{
    [My$$]
    class MyAttribute : Attribute
    {

    }
}";
            VerifyItemExists(markup, "My");
        }

        [WorkItem(843466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedAttributeAccessibleOnOuterType()
        {
            var markup = @"using System;

[My]
class Y
{

}

[$$]
class X
{
    [My]
    class MyAttribute : Attribute
    {

    }
}";
            VerifyItemExists(markup, "My");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType()
        {
            var markup = @"abstract class Test
{
  private int _field;

  public sealed class InnerTest : Test 
  {
    
    public void SomeTest() 
    {
        $$
    }
  }
}";
            VerifyItemExists(markup, "_field");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType2()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            $$ // M recommended and accessible
        }

        class NN
        {
            void Test2()
            {
                // M inaccessible and not recommended
            }
        }
    }
}";
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType3()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            M(); // M recommended and accessible
        }

        class NN
        {
            void Test2()
            {
                $$ // M inaccessible and not recommended
            }
        }
    }
}";
            VerifyItemIsAbsent(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType4()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            M(); // M recommended and accessible
        }

        class NN : N
        {
            void Test2()
            {
                $$ // M accessible and recommended.
            }
        }
    }
}";
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType5()
        {
            var markup = @"
class D
{
    public void Q() { }
}
class C<T> : D
{
    class N
    {
        void Test()
        {
            $$
        }
    }
}";
            VerifyItemIsAbsent(markup, "Q");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersFromBaseOuterType6()
        {
            var markup = @"
class Base<T>
{
    public int X;
}

class Derived : Base<int>
{
    class Nested
    {
        void Test()
        {
            $$
        }
    }
}";
            VerifyItemIsAbsent(markup, "X");
        }

        [WorkItem(983367)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoTypeParametersDefinedInCrefs()
        {
            var markup = @"using System;

/// <see cref=""Program{T$$}""/>
class Program<T> { }";
            VerifyItemIsAbsent(markup, "T");
        }

        [WorkItem(988025)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowTypesInGenericMethodTypeParameterList1()
        {
            var markup = @"
class Class1<T, D>
{
    public static Class1<T, D> Create() { return null; }
}
static class Class2
{
    public static void Test<T,D>(this Class1<T, D> arg)
    {
    }
}
class Program
{
    static void Main(string[] args)
    {
        Class1<string, int>.Create().Test<$$
    }
}
";
            VerifyItemExists(markup, "Class1<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(988025)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowTypesInGenericMethodTypeParameterList2()
        {
            var markup = @"
class Class1<T, D>
{
    public static Class1<T, D> Create() { return null; }
}
static class Class2
{
    public static void Test<T,D>(this Class1<T, D> arg)
    {
    }
}
class Program
{
    static void Main(string[] args)
    {
        Class1<string, int>.Create().Test<string,$$
    }
}
";
            VerifyItemExists(markup, "Class1<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(991466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DescriptionInAliasedType()
        {
            var markup = @"
using IAlias = IFoo;
///<summary>summary for interface IFoo</summary>
interface IFoo {  }
class C 
{ 
    I$$
}
";
            VerifyItemExists(markup, "IAlias", expectedDescriptionOrNull: "interface IFoo\r\nsummary for interface IFoo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WithinNameOf()
        {
            var markup = @"
class C 
{ 
    void foo()
    {
        var x = nameof($$)
    }
}
";
            VerifyAnyItemExists(markup);
        }

        [WorkItem(997410)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMemberInNameOfInStaticContext()
        {
            var markup = @"
class C
{
  int y1 = 15;
  static int y2 = 1;
  static string x = nameof($$
";
            VerifyItemExists(markup, "y1");
        }

        [WorkItem(997410)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMemberInNameOfInStaticContext()
        {
            var markup = @"
class C
{
  int y1 = 15;
  static int y2 = 1;
  static string x = nameof($$
";
            VerifyItemExists(markup, "y2");
        }

        [WorkItem(883293)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteDeclarationExpressionType()
        {
            var markup = @"
using System;
class C
{
  void foo()
    {
        var x = Console.$$
        var y = 3;
    }
}
";
            VerifyItemExists(markup, "WriteLine", experimental: true);
        }

        [WorkItem(1024380)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticAndInstanceInNameOf()
        {
            var markup = @"
using System;
class C
{
    class D
    {
        public int x;
        public static int y;   
    }

  void foo()
    {
        var z = nameof(C.D.$$
    }
}
";
            VerifyItemExists(markup, "x");
            VerifyItemExists(markup, "y");
        }

        [WorkItem(1663, "https://github.com/dotnet/roslyn/issues/1663")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameOfMembersListedForLocals()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(T.z.$$)
    }
}
 
public class T
{
    public U z; 
}
 
public class U
{
    public int nope;
}
";
            VerifyItemExists(markup, "nope");
        }

        [WorkItem(1029522)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameOfMembersListedForNamespacesAndTypes2()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(U.$$)
    }
}
 
public class T
{
    public U z; 
}
 
public class U
{
    public int nope;
}
";
            VerifyItemExists(markup, "nope");
        }

        [WorkItem(1029522)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameOfMembersListedForNamespacesAndTypes3()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(N.$$)
    }
}

namespace N
{
public class U
{
    public int nope;
}
} ";
            VerifyItemExists(markup, "U");
        }

        [WorkItem(1029522)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameOfMembersListedForNamespacesAndTypes4()
        {
            var markup = @"
using z = System;
class C
{
    void M()
    {
        var x = nameof(z.$$)
    }
}
";
            VerifyItemExists(markup, "Console");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings1()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{$$
";
            VerifyItemExists(markup, "a");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings2()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{$$}"";
    }
}";
            VerifyItemExists(markup, "a");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings3()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {$$
";
            VerifyItemExists(markup, "b");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings4()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {$$}"";
    }
}";
            VerifyItemExists(markup, "b");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings5()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {$$
";
            VerifyItemExists(markup, "b");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InterpolatedStrings6()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {$$}"";
    }
}";
            VerifyItemExists(markup, "b");
        }

        [WorkItem(1064811)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeFirstStringHole()
        {
            VerifyNoItemsExist(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBetweenStringHoles()
        {
            VerifyNoItemsExist(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStringHoles()
        {
            VerifyNoItemsExist(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1087171)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void CompletionAfterTypeOfGetType()
        {
            VerifyItemExists(AddInsideMethod(
"typeof(int).GetType().$$"), "GUID");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives1()
        {
            var markup = @"
using $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemIsAbsent(markup, "A");
            VerifyItemIsAbsent(markup, "B");
            VerifyItemExists(markup, "N");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives2()
        {
            var markup = @"
using N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemIsAbsent(markup, "C");
            VerifyItemIsAbsent(markup, "D");
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives3()
        {
            var markup = @"
using G = $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "A");
            VerifyItemExists(markup, "B");
            VerifyItemExists(markup, "N");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives4()
        {
            var markup = @"
using G = N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "C");
            VerifyItemExists(markup, "D");
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives5()
        {
            var markup = @"
using static $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "A");
            VerifyItemExists(markup, "B");
            VerifyItemExists(markup, "N");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirectives6()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "C");
            VerifyItemExists(markup, "D");
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticDoesNotShowDelegates1()
        {
            var markup = @"
using static $$

class A { }
delegate void B();

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "A");
            VerifyItemIsAbsent(markup, "B");
            VerifyItemExists(markup, "N");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticDoesNotShowDelegates2()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    delegate void D();

    namespace M { }
}";

            VerifyItemExists(markup, "C");
            VerifyItemIsAbsent(markup, "D");
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticDoesNotShowInterfaces1()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    interface I { }

    namespace M { }
}";

            VerifyItemExists(markup, "C");
            VerifyItemIsAbsent(markup, "I");
            VerifyItemExists(markup, "M");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticDoesNotShowInterfaces2()
        {
            var markup = @"
using static $$

class A { }
interface I { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            VerifyItemExists(markup, "A");
            VerifyItemIsAbsent(markup, "I");
            VerifyItemExists(markup, "N");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods1()
        {
            var markup = @"
using static A;
using static B;

static class A
{
    public static void Foo(this string s) { }
}

static class B
{
    public static void Bar(this string s) { }
}

class C
{
    void M()
    {
        $$
    }
}
";

            VerifyItemIsAbsent(markup, "Foo");
            VerifyItemIsAbsent(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods2()
        {
            var markup = @"
using N;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        $$
    }
}
";

            VerifyItemIsAbsent(markup, "Foo");
            VerifyItemIsAbsent(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods3()
        {
            var markup = @"
using N;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            VerifyItemExists(markup, "Foo");
            VerifyItemExists(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods4()
        {
            var markup = @"
using static N.A;
using static N.B;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            VerifyItemExists(markup, "Foo");
            VerifyItemExists(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods5()
        {
            var markup = @"
using static N.A;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            VerifyItemExists(markup, "Foo");
            VerifyItemIsAbsent(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods6()
        {
            var markup = @"
using static N.B;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            VerifyItemIsAbsent(markup, "Foo");
            VerifyItemExists(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStaticAndExtensionMethods7()
        {
            var markup = @"
using N;
using static N.B;

namespace N
{
    static class A
    {
        public static void Foo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$;
    }
}
";

            VerifyItemExists(markup, "Foo");
            VerifyItemExists(markup, "Bar");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExceptionFilter1()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch when ($$
";

            VerifyItemExists(markup, "x");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExceptionFilter2()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch (Exception ex) when ($$
";

            VerifyItemExists(markup, "x");
        }

        [WorkItem(717, "https://github.com/dotnet/roslyn/issues/717")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExpressionContextCompletionWithinCast()
        {
            var markup = @"
class Program
{
    void M()
    {
        for (int i = 0; i < 5; i++)
        {
            var x = ($$)
            var y = 1;
        }
    }
}
";
            VerifyItemExists(markup, "i");
        }

        [WorkItem(1277, "https://github.com/dotnet/roslyn/issues/1277")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersInPropertyInitializer()
        {
            var markup = @"
class A {
    int abc;
    int B { get; } = $$
}
";
            VerifyItemIsAbsent(markup, "abc");
        }

        [WorkItem(1277, "https://github.com/dotnet/roslyn/issues/1277")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersInPropertyInitializer()
        {
            var markup = @"
class A {
    static Action s_abc;
    event Action B = $$
}
";
            VerifyItemExists(markup, "s_abc");
        }


        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoInstanceMembersInFieldLikeEventInitializer()
        {
            var markup = @"
class A {
    Action abc;
    event Action B = $$
}
";
            VerifyItemIsAbsent(markup, "abc");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StaticMembersInFieldLikeEventInitializer()
        {
            var markup = @"
class A {
    static Action s_abc;
    event Action B = $$
}
";
            VerifyItemExists(markup, "s_abc");
        }

        [WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersInTopLevelFieldInitializer()
        {
            var markup = @"
int aaa = 1;
int bbb = $$
";
            VerifyItemExists(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InstanceMembersInTopLevelFieldLikeEventInitializer()
        {
            var markup = @"
Action aaa = null;
event Action bbb = $$
";
            VerifyItemExists(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoConditionalAccessCompletionOnTypes1()
        {
            var markup = @"
using A = System
class C
{
    A?.$$
}
";
            VerifyNoItemsExist(markup);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoConditionalAccessCompletionOnTypes2()
        {
            var markup = @"
class C
{
    System?.$$
}
";
            VerifyNoItemsExist(markup);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoConditionalAccessCompletionOnTypes3()
        {
            var markup = @"
class C
{
    System.Console?.$$
}
";
            VerifyNoItemsExist(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionInIncompletePropertyDeclaration()
        {
            var markup = @"
class Class1
{
    public string Property1 { get; set; }
}

class Class2
{
    public string Property { get { return this.Source.$$
    public Class1 Source { get; set; }
}";
            VerifyItemExists(markup, "Property1");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoCompletionInShebangComments()
        {
            VerifyNoItemsExist("#!$$", sourceCodeKind: SourceCodeKind.Script);
            VerifyNoItemsExist("#! S$$", sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: true);
        }
    }
}
