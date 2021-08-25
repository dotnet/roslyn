﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using static Roslyn.Test.Utilities.TestHelpers;
using KeyValuePair = Roslyn.Utilities.KeyValuePairUtil;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTreeTests : ParsingTests
    {
        public SyntaxTreeTests(ITestOutputHelper output) : base(output) { }

        protected SyntaxTree UsingTree(string text, CSharpParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var tree = base.UsingTree(text, options);

            var actualErrors = tree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);

            return tree;
        }

        // Diagnostic options on syntax trees are now obsolete
#pragma warning disable CS0618
        [Fact]
        public void CreateTreeWithDiagnostics()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.Create(SyntaxFactory.ParseCompilationUnit(""),
                options: null,
                path: "",
                encoding: null,
                diagnosticOptions: options);
            Assert.Same(options, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeWithChangesPreservesDiagnosticOptions()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                options: null,
                path: "",
                diagnosticOptions: options,
                isGeneratedCode: null,
                cancellationToken: default);
            Assert.Same(options, tree.DiagnosticOptions);
            var newTree = tree.WithChangedText(SourceText.From("class C { }"));
            Assert.Same(options, newTree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeNullDiagnosticOptions()
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                options: null,
                path: "",
                diagnosticOptions: null,
                isGeneratedCode: null,
                cancellationToken: default);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
            // The default options are case insensitive but the default empty ImmutableDictionary is not
            Assert.NotSame(ImmutableDictionary<string, ReportDiagnostic>.Empty, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeEmptyDiagnosticOptions()
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                options: null,
                path: "",
                diagnosticOptions: ImmutableDictionary<string, ReportDiagnostic>.Empty,
                isGeneratedCode: null,
                cancellationToken: default);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
            Assert.Same(ImmutableDictionary<string, ReportDiagnostic>.Empty, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeCustomDiagnosticOptions()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                options: null,
                path: "",
                diagnosticOptions: options,
                isGeneratedCode: null,
                cancellationToken: default);
            Assert.Same(options, tree.DiagnosticOptions);
        }

        [Fact]
        public void DefaultTreeDiagnosticOptions()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
        }

        [Fact]
        public void WithDiagnosticOptionsNull()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var newTree = tree.WithDiagnosticOptions(null);
            Assert.NotNull(newTree.DiagnosticOptions);
            Assert.True(newTree.DiagnosticOptions.IsEmpty);
            Assert.Same(tree, newTree);
        }

        [Fact]
        public void WithDiagnosticOptionsEmpty()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var newTree = tree.WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(newTree.DiagnosticOptions.IsEmpty);
            // Default empty immutable dictionary is case sensitive
            Assert.NotSame(tree.DiagnosticOptions, newTree.DiagnosticOptions);
        }

        [Fact]
        public void PerTreeDiagnosticOptionsNewDict()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var map = ImmutableDictionary.CreateRange(
                new[] { KeyValuePair.Create("CS00778", ReportDiagnostic.Suppress) });
            var newTree = tree.WithDiagnosticOptions(map);
            Assert.NotNull(newTree.DiagnosticOptions);
            Assert.Same(map, newTree.DiagnosticOptions);
            Assert.NotEqual(tree, newTree);
        }
#pragma warning restore CS0618

        [Fact]
        public void WithRootAndOptions_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}");
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.Unicode, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
            Assert.Same(Encoding.Unicode, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_DummyTree()
        {
            var dummy = new CSharpSyntaxTree.DummySyntaxTree();
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = dummy.WithRootAndOptions(newRoot, newOptions);
            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
        }

        [Fact]
        public void WithFilePath_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}", path: "old.cs");
            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.Unicode, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path: "old.cs");

            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Same(Encoding.Unicode, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_DummyTree()
        {
            var oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            var newTree = oldTree.WithFilePath("new.cs");

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());
        }

        [Fact, WorkItem(12638, "https://github.com/dotnet/roslyn/issues/12638")]
        public void WithFilePath_Null()
        {
            SyntaxTree oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            Assert.Equal(string.Empty, oldTree.WithFilePath(null).FilePath);
            oldTree = SyntaxFactory.ParseSyntaxTree("", path: "old.cs");
            Assert.Equal(string.Empty, oldTree.WithFilePath(null).FilePath);
            Assert.Equal(string.Empty, SyntaxFactory.ParseSyntaxTree("", path: null).FilePath);
            Assert.Equal(string.Empty, CSharpSyntaxTree.Create((CSharpSyntaxNode)oldTree.GetRoot(), path: null).FilePath);
        }

        [Fact]
        public void GlobalUsingDirective_01()
        {
            var test = "global using ns1;";

            UsingTree(test, TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_02()
        {
            var test = "global using ns1;";

            UsingTree(test, TestOptions.Regular9,
                // (1,1): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // global using ns1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_03()
        {
            var test = "namespace ns { global using ns1; }";

            UsingTree(test, TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_04()
        {
            var test = "namespace ns { global using ns1; }";

            UsingTree(test, TestOptions.Regular9,
                // (1,16): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // namespace ns { global using ns1; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 16)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_05()
        {
            var test = "global using static ns1;";

            UsingTree(test, TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_06()
        {
            var test = "global using static ns1;";

            UsingTree(test, TestOptions.Regular9,
                // (1,1): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // global using static ns1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using static ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_07()
        {
            var test = "namespace ns { global using static ns1; }";

            UsingTree(test, TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_08()
        {
            var test = "namespace ns { global using static ns1; }";

            UsingTree(test, TestOptions.Regular9,
                // (1,16): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // namespace ns { global using static ns1; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using static ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 16)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_09()
        {
            var test = "global using alias = ns1;";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_10()
        {
            var test = "global using alias = ns1;";

            UsingTree(test, TestOptions.Regular9,
                // (1,1): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // global using alias = ns1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using alias = ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_11()
        {
            var test = "namespace ns { global using alias = ns1; }";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.NameEquals);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "alias");
                            }
                            N(SyntaxKind.EqualsToken);
                        }
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_12()
        {
            var test = "namespace ns { global using alias = ns1; }";

            UsingTree(test, TestOptions.Regular9,
                // (1,16): error CS8773: Feature 'global using directive' is not available in C# 9.0. Please use language version 10.0 or greater.
                // namespace ns { global using alias = ns1; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "global using alias = ns1;").WithArguments("global using directive", "10.0").WithLocation(1, 16)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingDirective);
                    {
                        N(SyntaxKind.GlobalKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.NameEquals);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "alias");
                            }
                            N(SyntaxKind.EqualsToken);
                        }
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns1");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_13()
        {
            var test = @"
namespace ns {}
global using ns1;
";

            UsingTree(test, TestOptions.RegularPreview,
                // (3,1): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                // global using ns1;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "global using ns1;").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_14()
        {
            var test = @"
global using ns1;
extern alias a;
";

            UsingTree(test, TestOptions.RegularPreview,
                // (3,1): error CS0439: An extern alias declaration must precede all other elements defined in the namespace
                // extern alias a;
                Diagnostic(ErrorCode.ERR_ExternAfterElements, "extern").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_15()
        {
            var test = @"
namespace ns2
{
    namespace ns {}
    global using ns1;
}
";

            UsingTree(test, TestOptions.RegularPreview,
                // (5,5): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                //     global using ns1;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "global using ns1;").WithLocation(5, 5)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NamespaceDeclaration);
                    {
                        N(SyntaxKind.NamespaceKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ns");
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_16()
        {
            var test = @"
global using ns1;
namespace ns {}
";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns");
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_01()
        {
            var test = "d using ns1;";

            UsingTree(test, TestOptions.Regular,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // d using ns1;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_17()
        {
            var test = "d global using ns1;";

            UsingTree(test, TestOptions.RegularPreview,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // d global using ns1;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "d").WithLocation(1, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void UsingDirective_02()
        {
            var test = "using ns1; p using ns2;";

            UsingTree(test, TestOptions.Regular,
                // (1,12): error CS0116: A namespace cannot directly contain members such as fields or methods
                // using ns1; p using ns2;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "p").WithLocation(1, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_18()
        {
            var test = "global using ns1; p global using ns2;";

            UsingTree(test, TestOptions.RegularPreview,
                // (1,19): error CS0116: A namespace cannot directly contain members such as fields or methods
                // global using ns1; p global using ns2;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "p").WithLocation(1, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_19()
        {
            var test = @"
M();
global using ns1;
";

            UsingTree(test, TestOptions.RegularPreview,
                // (3,1): error CS1529: A using clause must precede all other elements defined in the namespace except extern alias declarations
                // global using ns1;
                Diagnostic(ErrorCode.ERR_UsingAfterElements, "global using ns1;").WithLocation(3, 1)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_20()
        {
            var test = @"
global using ns1;
using ns2;
M();
";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_21()
        {
            var test = @"
global using alias1 = ns1;
using alias2 = ns2;
M();
";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias1");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias2");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void GlobalUsingDirective_22()
        {
            var test = @"
global using static ns1;
using static ns2;
M();
";

            UsingTree(test, TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.GlobalKeyword);
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ns2");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
