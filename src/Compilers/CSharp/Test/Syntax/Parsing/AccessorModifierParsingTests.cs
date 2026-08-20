// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class AccessorModifierParsingTests(ITestOutputHelper output) : ParsingTests(output)
{
    public static TheoryData<LanguageVersion, string, string, string> NewlyParsedAccessorModifiers
    {
        get
        {
            var data = new TheoryData<LanguageVersion, string, string, string>();
            var accessorKinds = new[]
            {
                ("property", "get"),
                ("property", "set"),
                ("property", "init"),
                ("indexer", "get"),
                ("event", "add"),
                ("event", "remove"),
            };

            add(LanguageVersion.CSharp10, "partial", "async", "required", "file", "closed", "scoped");
            add(LanguageVersion.CSharp11, "partial", "async", "closed", "scoped");
            add(LanguageVersion.CSharp14, "partial", "async", "closed", "scoped");
            add(LanguageVersion.Preview, "partial", "async", "scoped");

            return data;

            void add(LanguageVersion languageVersion, params string[] modifiers)
            {
                foreach (var modifier in modifiers)
                {
                    foreach (var (declarationKind, accessorKind) in accessorKinds)
                    {
                        data.Add(languageVersion, modifier, declarationKind, accessorKind);
                    }
                }
            }
        }
    }

    public static TheoryData<LanguageVersion, string, string, string> NewlyParsedAccessorModifierOrderings
    {
        get
        {
            var data = new TheoryData<LanguageVersion, string, string, string>();
            var modifiers = new[]
            {
                "public", "internal", "protected", "private", "sealed", "abstract", "static", "virtual",
                "extern", "new", "override", "readonly", "volatile", "unsafe", "partial", "async", "ref",
                "required", "file", "closed", "safe", "scoped",
            };
            var partialFollowers = new[] { "virtual", "extern", "override", "readonly", "volatile", "ref" };

            add(LanguageVersion.CSharp10, "partial", "async", "required", "file", "closed", "scoped");
            add(LanguageVersion.CSharp11, "partial", "async", "closed", "scoped");
            add(LanguageVersion.CSharp14, "partial", "async", "closed", "scoped");
            add(LanguageVersion.Preview, "partial", "async", "scoped");

            return data;

            void add(LanguageVersion languageVersion, params string[] secondModifiers)
            {
                var pairs = new HashSet<(string first, string second)>();

                foreach (var first in modifiers)
                {
                    foreach (var second in secondModifiers)
                    {
                        pairs.Add((first, second));
                    }
                }

                foreach (var second in modifiers)
                {
                    pairs.Add(("scoped", second));
                }

                foreach (var second in partialFollowers)
                {
                    pairs.Add(("partial", second));
                }

                foreach (var (first, second) in pairs)
                {
                    data.Add(languageVersion, first, second, "property");
                    data.Add(languageVersion, first, second, "event");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(NewlyParsedAccessorModifiers))]
    public void NewlyParsedAccessorModifier(
        LanguageVersion languageVersion,
        string modifier,
        string declarationKind,
        string accessorKind)
    {
        var source = (declarationKind, accessorKind) switch
        {
            ("property", "get") => $"class C {{ int P {{ {modifier} get; set; }} }}",
            ("property", "set") => $"class C {{ int P {{ get; {modifier} set; }} }}",
            ("property", "init") => $"class C {{ int P {{ get; {modifier} init; }} }}",
            ("indexer", "get") => $"class C {{ int this[int i] {{ {modifier} get; set; }} }}",
            ("event", "add") => $"class C {{ event System.Action E {{ {modifier} add {{ }} remove {{ }} }} }}",
            ("event", "remove") => $"class C {{ event System.Action E {{ add {{ }} {modifier} remove {{ }} }} }}",
            _ => throw ExceptionUtilities.Unreachable(),
        };
        var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
        var tree = SyntaxFactory.ParseSyntaxTree(source, options);

        Assert.Contains(
            tree.GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)(declarationKind == "event"
                ? ErrorCode.ERR_AddOrRemoveExpected
                : ErrorCode.ERR_GetOrSetExpected));

        var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single(
            accessor => accessor.Keyword.ValueText == accessorKind);
        Assert.Empty(accessor.Modifiers);

        Assert.Contains(
            CreateCompilation(source, parseOptions: options).GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)(declarationKind == "event"
                ? ErrorCode.ERR_AddOrRemoveExpected
                : ErrorCode.ERR_GetOrSetExpected));
    }

    [Theory]
    [MemberData(nameof(NewlyParsedAccessorModifierOrderings))]
    public void NewlyParsedAccessorModifierOrdering(
        LanguageVersion languageVersion,
        string firstModifier,
        string secondModifier,
        string declarationKind)
    {
        var source = declarationKind switch
        {
            "property" => $"class C {{ int P {{ {firstModifier} {secondModifier} get; set; }} }}",
            "event" => $"class C {{ event System.Action E {{ {firstModifier} {secondModifier} add {{ }} remove {{ }} }} }}",
            _ => throw ExceptionUtilities.Unreachable(),
        };
        var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
        var tree = SyntaxFactory.ParseSyntaxTree(source, options);

        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            CreateCompilation(source, parseOptions: options).GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [MemberData(nameof(NewlyParsedAccessorModifiers))]
    public void NewlyParsedAccessorModifierAfterAttribute(
        LanguageVersion languageVersion,
        string modifier,
        string declarationKind,
        string accessorKind)
    {
        var source = (declarationKind, accessorKind) switch
        {
            ("property", "get") => $"class C {{ int P {{ [System.Obsolete] {modifier} get; set; }} }}",
            ("property", "set") => $"class C {{ int P {{ get; [System.Obsolete] {modifier} set; }} }}",
            ("property", "init") => $"class C {{ int P {{ get; [System.Obsolete] {modifier} init; }} }}",
            ("indexer", "get") => $"class C {{ int this[int i] {{ [System.Obsolete] {modifier} get; set; }} }}",
            ("event", "add") => $"class C {{ event System.Action E {{ [System.Obsolete] {modifier} add {{ }} remove {{ }} }} }}",
            ("event", "remove") => $"class C {{ event System.Action E {{ add {{ }} [System.Obsolete] {modifier} remove {{ }} }} }}",
            _ => throw ExceptionUtilities.Unreachable(),
        };
        var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
        var tree = SyntaxFactory.ParseSyntaxTree(source, options);

        Assert.Contains(
            tree.GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)(declarationKind == "event"
                ? ErrorCode.ERR_AddOrRemoveExpected
                : ErrorCode.ERR_GetOrSetExpected));

        var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single(
            accessor => accessor.Keyword.ValueText == accessorKind);
        Assert.Empty(accessor.AttributeLists);
        Assert.Empty(accessor.Modifiers);

        Assert.Contains(
            CreateCompilation(source, parseOptions: options).GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)(declarationKind == "event"
                ? ErrorCode.ERR_AddOrRemoveExpected
                : ErrorCode.ERR_GetOrSetExpected));
    }

    [Fact]
    public void ModifierBeforeAttributeRecovery()
    {
        const string source = "class C { int P { private [System.Obsolete] get; set; } }";
        var tree = SyntaxFactory.ParseSyntaxTree(source);

        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Code == (int)ErrorCode.ERR_RbraceExpected);
        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Code == (int)ErrorCode.ERR_TypeExpected);
        Assert.Contains(
            CreateCompilation(source).GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("property", ";")]
    [InlineData("property", "{ }")]
    [InlineData("event", ";")]
    [InlineData("event", "{ }")]
    public void NewlyParsedModifierWithMissingAccessorName(string declarationKind, string body)
    {
        var modifiers = new[]
        {
            "public", "internal", "protected", "private", "sealed", "abstract", "static", "virtual",
            "extern", "new", "override", "readonly", "volatile", "unsafe", "partial", "async", "ref",
            "required", "file", "closed", "safe", "scoped",
        };
        var options = TestOptions.Regular10;

        foreach (var modifier in modifiers)
        {
            var source = declarationKind switch
            {
                "property" => $"class C {{ int P {{ {modifier} {body} }} }}",
                "event" => $"class C {{ event System.Action E {{ {modifier} {body} }} }}",
                _ => throw ExceptionUtilities.Unreachable(),
            };
            var tree = SyntaxFactory.ParseSyntaxTree(source, options);
            Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(
                CreateCompilation(source, parseOptions: options).GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("property", "get")]
    [InlineData("property", "set")]
    [InlineData("property", "init")]
    [InlineData("indexer", "get")]
    [InlineData("event", "add")]
    [InlineData("event", "remove")]
    public void NewlyParsedModifierOnAccessorWithMissingBody(string declarationKind, string accessorKind)
    {
        foreach (var modifier in new[] { "partial", "scoped" })
        {
            var source = (declarationKind, accessorKind) switch
            {
                ("property", "get") => $"class C {{ int P {{ {modifier} get }} }}",
                ("property", "set") => $"class C {{ int P {{ {modifier} set }} }}",
                ("property", "init") => $"class C {{ int P {{ {modifier} init }} }}",
                ("indexer", "get") => $"class C {{ int this[int i] {{ {modifier} get }} }}",
                ("event", "add") => $"class C {{ event System.Action E {{ {modifier} add }} }}",
                ("event", "remove") => $"class C {{ event System.Action E {{ {modifier} remove }} }}",
                _ => throw ExceptionUtilities.Unreachable(),
            };
            var tree = SyntaxFactory.ParseSyntaxTree(source, TestOptions.RegularPreview);
            var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single(
                accessor => accessor.Keyword.ValueText == accessorKind);

            Assert.Equal(accessorKind, accessor.Keyword.ValueText);
            Assert.Empty(accessor.Modifiers);
            Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(
                CreateCompilation(source, parseOptions: TestOptions.RegularPreview).GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("property")]
    [InlineData("event")]
    public void NewlyParsedModifierBeforeCloseBrace(string declarationKind)
    {
        foreach (var modifier in new[] { "partial", "async", "required", "file", "closed", "scoped" })
        {
            var source = declarationKind switch
            {
                "property" => $"class C {{ int P {{ {modifier} }} }}",
                "event" => $"class C {{ event System.Action E {{ {modifier} }} }}",
                _ => throw ExceptionUtilities.Unreachable(),
            };
            var tree = SyntaxFactory.ParseSyntaxTree(source, TestOptions.Regular10);
            var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();

            Assert.Equal(SyntaxKind.UnknownAccessorDeclaration, accessor.Kind());
            Assert.Equal(modifier, accessor.Keyword.ValueText);
            Assert.Empty(accessor.Modifiers);
            Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(
                CreateCompilation(source, parseOptions: TestOptions.Regular10).GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("=> 0;")]
    [InlineData("unknown;")]
    public void ContextualModifierBeforeNonAccessor(string trailingTokens)
    {
        var source = $"class C {{ int P {{ partial {trailingTokens} }} }}";
        var tree = SyntaxFactory.ParseSyntaxTree(source);

        var accessors = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().ToArray();
        Assert.Equal("partial", accessors[0].Keyword.ValueText);
        Assert.All(accessors, accessor => Assert.Empty(accessor.Modifiers));
        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            CreateCompilation(source).GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ContextualModifierBeforeFollowingMember()
    {
        const string source = "class C { int P { partial int F; } }";
        var tree = SyntaxFactory.ParseSyntaxTree(source);

        var field = Assert.Single(tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>());
        Assert.Equal("F", field.Declaration.Variables.Single().Identifier.ValueText);
        Assert.Empty(field.Modifiers);
        var accessor = Assert.Single(tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>());
        Assert.True(accessor.Keyword.IsMissing);
        Assert.Equal("partial", Assert.Single(accessor.Modifiers).ValueText);
        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            CreateCompilation(source).GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)ErrorCode.ERR_GetOrSetExpected);
    }

    [Fact]
    public void NewlyParsedModifierAtEndOfFile()
    {
        const string source = "class C { int P { partial";
        var tree = SyntaxFactory.ParseSyntaxTree(source);
        var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();

        Assert.Equal("partial", accessor.Keyword.ValueText);
        Assert.Empty(accessor.Modifiers);
        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            CreateCompilation(source).GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void NewlyParsedAttributedModifierWithMissingAccessorName()
    {
        const string source = "class C { int P { [System.Obsolete] partial } }";
        var tree = SyntaxFactory.ParseSyntaxTree(source);
        var accessor = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();

        Assert.Single(accessor.AttributeLists);
        Assert.Equal("partial", accessor.Keyword.ValueText);
        Assert.Empty(accessor.Modifiers);
        Assert.Contains(tree.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            CreateCompilation(source).GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ValidPropertyAndIndexerAccessors()
    {
        const string source = """
            class C
            {
                public int P { get; private set; }
                public int Q { get; private init; }
                public int this[int i] { private get => 0; set { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.IndexerDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ThisKeyword);
                    N(SyntaxKind.BracketedParameterList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp).VerifyDiagnostics();
    }

    [Fact]
    public void RefModifiersRemainOnPropertyAndEventAccessors()
    {
        const string source = """
            class C
            {
                int P { ref get => 0; abstract ref set { } }
                event System.Action E { ref add { } abstract ref remove { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.AbstractKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EventDeclaration);
                {
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "System");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.AddAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.AddKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.RemoveAccessorDeclaration);
                        {
                            N(SyntaxKind.AbstractKeyword);
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.RemoveKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("abstract").WithLocation(3, 40),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("ref").WithLocation(3, 40),
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "ref").WithLocation(4, 29),
            Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "abstract").WithLocation(4, 41));
    }

    [Fact]
    public void RefReturningPropertyWithAccessorKeywordType()
    {
        const string source = """
            #pragma warning disable CS8981
            class get { }
            class C
            {
                get _value;
                ref get A => ref _value;
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "get");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "_value");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "get");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_value");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void IncompleteRefAccessorBeforeCloseBrace()
    {
        const string source = """
            class C
            {
                int P { ref get }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            M(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 17),
            Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "}").WithLocation(3, 21));
    }

    [Fact]
    public void ScopedRefModifiersRemainOnAccessor()
    {
        const string source = """
            class C
            {
                public int P { scoped ref get; set; }
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 20));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 20),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("ref").WithLocation(3, 31));
    }

    [Fact]
    public void SafeModifierOrderingAndAccessorName()
    {
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
                public int Q { get => 0; safe private set { } }
                int R { safe; get => 0; set { } }
            }
            """;

        UsingTree(
            source,
            TestOptions.RegularPreview,
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "safe").WithLocation(5, 13));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.UnknownAccessorDeclaration);
                        {
                            N(SyntaxKind.IdentifierToken, "safe");
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "safe").WithLocation(5, 13));
    }

    [Fact]
    public void ContextualKeywordsAreRecoveredBeforeAccessors()
    {
        const string source = """
            class C
            {
                int P { scoped get; set; }
                int Q { partial get; set; }
                int R { async get; set; }
                public int S { private async get; set; }
                public int T { private partial get; set; }
            }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(source, TestOptions.RegularPreview);
        tree.GetDiagnostics().Verify(
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "scoped").WithLocation(3, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(4, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "async").WithLocation(5, 13),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "async").WithLocation(6, 28),
            Diagnostic(ErrorCode.ERR_GetOrSetExpected, "partial").WithLocation(7, 28));

        var getAccessors = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>()
            .Where(accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));
        Assert.Equal(5, getAccessors.Count());
        Assert.All(getAccessors, accessor => Assert.Empty(accessor.Modifiers));

        Assert.Contains(
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).GetDiagnostics(),
            diagnostic => diagnostic.Code == (int)ErrorCode.ERR_GetOrSetExpected);
    }

    [Fact]
    public void InvalidModifiersRemainOnAccessors()
    {
        const string source = """
            class C
            {
                int P { required get; file set; }
                int Q { closed get; static set; }
                public int R { private private get; set; }
            }
            """;

        UsingTree(source, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.RequiredKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.FileKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Q");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.ClosedKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.StaticKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("required").WithLocation(3, 22),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("file").WithLocation(3, 32),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("closed").WithLocation(4, 20),
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "set").WithArguments("static").WithLocation(4, 32),
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "private").WithArguments("private").WithLocation(5, 28));
    }

    [Fact]
    public void AttributeBeforeReadonlyAccessorModifier()
    {
        const string source = """
            struct S
            {
                public int P { [System.Obsolete] readonly get => 0; set { } }
            }
            """;

        UsingTree(source);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "System");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Obsolete");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void AccessorRecoveryDoesNotConsumeFollowingMember()
    {
        const string source = """
            class C
            {
                int P
                {
                    get { return 0; }
                private int F;
            }
            """;

        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.ReturnStatement);
                                {
                                    N(SyntaxKind.ReturnKeyword);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PrivateKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 26),
            Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(6, 17));
    }

    [Fact]
    public void AccessorModifierBeforeFeature()
    {
        const string source = """
            class C
            {
                public int P { get; private set; }
            }
            """;
        var options = TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp1);

        UsingTree(source, options);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: options).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "P").WithArguments("automatically implemented properties", "3").WithLocation(3, 16),
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "private").WithArguments("access modifiers on properties", "2").WithLocation(3, 25));
    }

    [Fact]
    public void SafeAccessorModifierBeforeFeature()
    {
        const string source = """
            class C
            {
                public int P { private safe get => 0; set { } }
            }
            """;

        UsingTree(source, TestOptions.Regular14);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.PrivateKeyword);
                            N(SyntaxKind.SafeKeyword);
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "safe").WithArguments("updated memory safety rules").WithLocation(3, 28));
    }
}
