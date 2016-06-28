// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ExtensionEverything)]
    public class ExtensionParsingTests : CSharpTestBase
    {
        private static readonly CSharpParseOptions parseOptions = TestOptions.Regular.WithExtensionEverythingFeature();

        [Fact]
        public void EmptyExtensionClass()
        {
            var root = SyntaxFactory.ParseCompilationUnit("extension class C { }", options: parseOptions);
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(type.Modifiers.ToDeclarationModifiers(), DeclarationModifiers.Extension);
        }

        private void ExtensionModifiersHelper(string modifier, DeclarationModifiers expected)
        {
            var root = SyntaxFactory.ParseCompilationUnit($"{modifier} extension class C {{ }}", options: parseOptions);
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(type.Modifiers.ToDeclarationModifiers(), DeclarationModifiers.Extension | expected);

            root = SyntaxFactory.ParseCompilationUnit($"extension {modifier} class C {{ }}", options: parseOptions);
            root.Errors().Verify();
            type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(type.Modifiers.ToDeclarationModifiers(), DeclarationModifiers.Extension | expected);
        }

        [Fact]
        public void ExtensionModifiers()
        {
            ExtensionModifiersHelper("public", DeclarationModifiers.Public);
            ExtensionModifiersHelper("private", DeclarationModifiers.Private);
            ExtensionModifiersHelper("internal", DeclarationModifiers.Internal);
            ExtensionModifiersHelper("protected", DeclarationModifiers.Protected);
            ExtensionModifiersHelper("protected internal", DeclarationModifiers.ProtectedInternal);
            ExtensionModifiersHelper("unsafe", DeclarationModifiers.Unsafe);
            // partial is weird - "partial extension class C" isn't allowed, but "extension partial class C" is. Inlined the helper for the weirdness.
            //   (partial is only allowed directly before 'class' - need to bring this to LDM, since the first case seems natural but is an error)
            //ExtensionModifiersHelper("partial", DeclarationModifiers.Partial);
            {
                var root = SyntaxFactory.ParseCompilationUnit("partial extension class C { }", options: parseOptions);
                root.Errors().Verify(
                    // error CS0267: The 'partial' modifier can only appear immediately before 'class', 'struct', 'interface', or 'void'
                    Diagnostic(ErrorCode.ERR_PartialMisplaced).WithLocation(1, 1)
                );

                root = SyntaxFactory.ParseCompilationUnit("extension partial class C { }", options: parseOptions);
                root.Errors().Verify();
                var type = (TypeDeclarationSyntax)root.Members[0];
                Assert.Equal(type.Modifiers.ToDeclarationModifiers(), DeclarationModifiers.Extension | DeclarationModifiers.Partial);
            }

            // abstract, sealed, and static are not allowed on extension classes,
            // but the error is reported later (not a syntax error)
            ExtensionModifiersHelper("abstract", DeclarationModifiers.Abstract);
            ExtensionModifiersHelper("sealed", DeclarationModifiers.Sealed);
            ExtensionModifiersHelper("static", DeclarationModifiers.Static);
            ExtensionModifiersHelper("volatile", DeclarationModifiers.Volatile);
        }

        /*
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
        */
    }
}
