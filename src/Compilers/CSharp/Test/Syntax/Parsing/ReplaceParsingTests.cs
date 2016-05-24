// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.SourceGenerators)]
    public class ReplaceParsingTests : CSharpTestBase
    {
        [Fact]
        public void ReplaceClass()
        {
            var root = SyntaxFactory.ParseCompilationUnit("abstract replace override class C { }", options: TestOptions.Regular.WithReplaceFeature());
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(
                type.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.Abstract | DeclarationModifiers.Override | DeclarationModifiers.Replace);
        }
        
        [Fact]
        public void ReplaceMethod()
        {
            var root = SyntaxFactory.ParseCompilationUnit("class C { virtual replace protected void M() { } }", options: TestOptions.Regular.WithReplaceFeature());
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(
                type.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.None);
            var method = (MethodDeclarationSyntax)type.Members[0];
            Assert.Equal(
                method.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.Virtual | DeclarationModifiers.Protected | DeclarationModifiers.Replace);
        }
        
        [Fact]
        public void ReplaceMethodNoFeature()
        {
            var source = "class C { virtual replace protected void M() { } }";
            CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll).VerifyDiagnostics(
                // (1,19): error CS8058: Feature 'replaced members' is experimental and unsupported; use '/features:replace' to enable.
                // class C { virtual replace protected void M() { } }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "replace").WithArguments("replaced members", "replace").WithLocation(1, 19)
            );
        }

        [Fact]
        public void OriginalExpression()
        {
            var expr = SyntaxFactory.ParseExpression("original");
            expr.Errors().Verify();
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            Assert.Equal(SyntaxKind.OriginalKeyword, ((IdentifierNameSyntax)expr).Identifier.ContextualKind());
            var opKind = SyntaxFacts.GetInstanceExpression(SyntaxKind.OriginalKeyword);
            Assert.Equal(SyntaxKind.None, opKind);
        }
        
        [Fact]
        public void OriginalNoReplace()
        {
            OriginalInMember("class C { void F() { original(); } }", inReplace: false);
            OriginalInMember("class C { object P { get { return original; } } }", inReplace: false);
            OriginalInMember("class C { object this[int index] { set { original[index] = value; } } }", inReplace: false);
            OriginalInMember("class C { event System.EventHandler E { add { original.Add(value); } } }", inReplace: false);
            OriginalInMember("class C { object F() => original(); }", inReplace: false);
            OriginalInMember("class C { object P => original; }", inReplace: false);
            OriginalInMember("class C { object this[int index] => original[index]; }", inReplace: false);
        }
        
        [Fact]
        public void OriginalInReplace()
        {
            OriginalInMember("class C { replace void F() { original(); } }", inReplace: true);
            OriginalInMember("class C { replace object P { get { return original; } } }", inReplace: true);
            OriginalInMember("class C { replace object this[int index] { set { original[index] = value; } } }", inReplace: true);
            OriginalInMember("class C { replace event System.EventHandler E { add { original.Add(value); } } }", inReplace: true);
            OriginalInMember("class C { replace object F() => original(); }", inReplace: true);
            OriginalInMember("class C { replace object P => original; }", inReplace: true);
            OriginalInMember("class C { replace object this[int index] => original[index]; }", inReplace: true);
        }

        private void OriginalInMember(string text, bool inReplace)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Regular.WithReplaceFeature());
            var root = tree.GetCompilationUnitRoot();
            root.Errors().Verify();
            var token = root.DescendantTokens().Where(t => t.Text == "original").Single();
            var expr = token.Parent;
            var expectedKind = inReplace ? SyntaxKind.OriginalExpression : SyntaxKind.IdentifierName;
            Assert.Equal(expectedKind, expr.Kind());
        }

        [Fact]
        [WorkItem(11503, "https://github.com/dotnet/roslyn/issues/11503")]
        public void MethodCalledReplace()
        {
            var root = SyntaxFactory.ParseCompilationUnit("class C { public static object replace() { } }", options: TestOptions.Regular.WithReplaceFeature());
            root.Errors().Verify();
        }
    }
}
