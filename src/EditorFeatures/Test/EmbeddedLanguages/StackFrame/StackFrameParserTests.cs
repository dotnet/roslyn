// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame.StackFrameSyntaxFactory;
using static Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame.StackFrameExtensions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame;

public sealed partial class StackFrameParserTests
{
    [Fact]
    public void TestNoParams()
        => Verify(
            @"at ConsoleApp4.MyClass.M()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                argumentList: EmptyParams)
            );

    [Fact]
    public void TestCtor()
        => Verify(
            @"at ConsoleApp4.MyClass..ctor()",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Constructor),
                argumentList: EmptyParams)
            );

    [Fact]
    public void TestStaticCtor()
        => Verify(
            @"at ConsoleApp4.MyClass..cctor()",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    StaticConstructor),
                argumentList: EmptyParams)
            );

    [Theory]
    [InlineData("C", 1)]
    [InlineData("C", 100)]
    [InlineData("a‿", 5)] // Unicode character with connection
    [InlineData("abcdefg", 99999)]
    public void TestArity(string typeName, int arity)
        => Verify(
            $"at ConsoleApp4.{typeName}`{arity}.M()",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        GenericType(typeName, arity)),
                    Identifier("M")),

                argumentList: EmptyParams)
            );

    [Fact]
    public void TestTrailingTrivia()
        => Verify(
            @"at ConsoleApp4.MyClass.M() some other text",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                argumentList: EmptyParams),

            eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(" some other text"))
            );

    [Fact]
    public void TestTrailingTrivia_InTriviaNoSpace()
        => Verify(
            @"at ConsoleApp4.MyClass.M() inC:\My\Path\C.cs:line 26",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                argumentList: EmptyParams),

            eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@" inC:\My\Path\C.cs:line 26"))
            );

    [Fact]
    public void TestTrailingTrivia_InTriviaNoSpace2()
        => Verify(
            @"at ConsoleApp4.MyClass.M()in C:\My\Path\C.cs:line 26",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: AtTrivia),
                argumentList: EmptyParams),

            eolTokenOpt: EOLToken.With(leadingTrivia: CreateTriviaArray(@"in C:\My\Path\C.cs:line 26"))
            );

    [Fact]
    public void TestNoParams_NoAtTrivia()
        => Verify(
            @"ConsoleApp4.MyClass.M()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M"),
                argumentList: EmptyParams)
            );

    [Fact]
    public void TestNoParams_SpaceInParams_NoAtTrivia()
        => Verify(
            @"ConsoleApp4.MyClass.M(  )",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M"),
                argumentList: ParameterList(
                    OpenParenToken.With(trailingTrivia: ImmutableArray.Create(SpaceTrivia(2))),
                    CloseParenToken))
            );

    [Fact]
    public void TestNoParams_SpaceTrivia()
        => Verify(
            @" ConsoleApp4.MyClass.M()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: SpaceTrivia()),
                argumentList: EmptyParams)
            );

    [Fact]
    public void TestNoParams_SpaceTrivia2()
        => Verify(
            @"  ConsoleApp4.MyClass.M()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("ConsoleApp4.MyClass.M", leadingTrivia: SpaceTrivia(2)),
                argumentList: EmptyParams)
            );

    [Fact]
    public void TestMethodOneParam()
        => Verify(
            @"at ConsoleApp4.MyClass.M(string s)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),

                argumentList: ParameterList(
                    Parameter(
                        Identifier("string"),
                        IdentifierToken("s", leadingTrivia: SpaceTrivia())))
                )
            );

    [Fact]
    public void TestMethodOneParamSpacing()
        => Verify(
            @"at ConsoleApp4.MyClass.M( string s )",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),

                argumentList: ParameterList(
                    OpenParenToken.With(trailingTrivia: SpaceTrivia().ToImmutableArray()),
                    CloseParenToken,
                    Parameter(
                        Identifier("string"),
                        IdentifierToken("s", leadingTrivia: SpaceTrivia(), trailingTrivia: SpaceTrivia())))
                )
            );

    [Fact]
    public void TestMethodTwoParam()
        => Verify(
            @"at ConsoleApp4.MyClass.M(string s, string t)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),

                argumentList: ParameterList(
                    Parameter(
                        Identifier("string"),
                        IdentifierToken("s", leadingTrivia: SpaceTrivia())),
                    Parameter(
                        Identifier("string", leadingTrivia: SpaceTrivia()),
                        IdentifierToken("t", leadingTrivia: SpaceTrivia())))
                )
            );

    [Fact]
    public void TestMethodArrayParam()
        => Verify(
            @"at ConsoleApp4.MyClass.M(string[] s)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),

                argumentList: ParameterList(
                        Parameter(ArrayType(Identifier("string"), ArrayRankSpecifier(trailingTrivia: SpaceTrivia())),
                        IdentifierToken("s")))
            )
        );

    [Fact]
    public void TestMethodArrayParamWithSpace()
        => Verify(
            "M.N(string[ , , ] s)",
            methodDeclaration: MethodDeclaration(
                QualifiedName("M.N"),
                argumentList: ParameterList(
                    Parameter(
                        ArrayType(Identifier("string"),
                            ArrayRankSpecifier(
                                OpenBracketToken.With(trailingTrivia: SpaceTrivia().ToImmutableArray()),
                                CloseBracketToken.With(trailingTrivia: SpaceTrivia().ToImmutableArray()),
                                CommaToken.With(trailingTrivia: SpaceTrivia().ToImmutableArray()),
                                CommaToken.With(trailingTrivia: SpaceTrivia().ToImmutableArray()))),
                        IdentifierToken("s")
                    )
                ))
        );

    [Fact]
    public void TestCommaArrayParam()
        => Verify(
            @"at ConsoleApp4.MyClass.M(string[,] s)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),

                argumentList: ParameterList(
                    Parameter(
                        ArrayType(Identifier("string"), ArrayRankSpecifier(1, trailingTrivia: SpaceTrivia())),
                        IdentifierToken("s")))
            )
        );

    [Fact]
    public void TestInvalidParameterIdentifier_MemberAccess()
        => Verify("at ConsoleApp4.MyClass(string my.string.name)", expectFailure: true);

    [Fact]
    public void TestInvalidParameterIdentifier_TypeArity()
        => Verify("at ConsoleApp4.MyClass(string s`1)", expectFailure: true);

    [Fact]
    public void TestGenericMethod_Brackets()
        => Verify(
            @"at ConsoleApp4.MyClass.M[T](T t)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),
                typeArguments: TypeArgumentList(TypeArgument("T")),
                argumentList: ParameterList(
                    Parameter(
                        Identifier("T"),
                        IdentifierToken("t", leadingTrivia: SpaceTrivia())))
            )
        );

    [Fact]
    public void TestGenericMethod()
        => Verify(
            @"at ConsoleApp4.MyClass.M<T>(T t)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    QualifiedName(
                        Identifier("ConsoleApp4", leadingTrivia: AtTrivia),
                        Identifier("MyClass")),
                    Identifier("M")),
                typeArguments: TypeArgumentList(useBrackets: false, TypeArgument("T")),
                argumentList: ParameterList(
                    Parameter(
                        Identifier("T"),
                        IdentifierToken("t", leadingTrivia: SpaceTrivia())))
            )
        );

    [Theory]
    [InlineData("_")]
    [InlineData("_s")]
    [InlineData("S0m3th1ng")]
    [InlineData("ü")] // Unicode character
    [InlineData("uʶ")] // character and modifier character
    [InlineData("a\u00AD")] // Soft hyphen formatting character
    [InlineData("a‿")] // Connecting punctuation (combining character)
    [InlineData("at")]
    [InlineData("line")]
    [InlineData("in")]
    public void TestIdentifierNames(string identifierName)
        => Verify(
            @$"at {identifierName}.{identifierName}[{identifierName}]({identifierName} {identifierName})",
            methodDeclaration: MethodDeclaration(
                QualifiedName($"{identifierName}.{identifierName}", leadingTrivia: AtTrivia),
                typeArguments: TypeArgumentList(TypeArgument(identifierName)),
                argumentList: ParameterList(
                    Parameter(
                        Identifier(identifierName),
                        IdentifierToken(identifierName, leadingTrivia: SpaceTrivia())))
            )
        );

    [Fact]
    public void TestInvalidSpacingBeforeQualifiedName()
        => Verify(
            @"at MyNamespace. MyClass.MyMethod()", expectFailure: true);

    [Fact]
    public void TestInvalidSpacingAfterQualifiedName2()
        => Verify(
            @"at MyNamespace.MyClass .MyMethod()", expectFailure: true);

    [Fact]
    public void TestWhitespaceAroundBrackets()
        => Verify(
            @"at MyNamespace.MyClass.MyMethod[ T ]()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("MyNamespace.MyClass.MyMethod", leadingTrivia: AtTrivia),
                typeArguments: TypeArgumentList(
                    TypeArgument(IdentifierToken("T", leadingTrivia: SpaceTrivia(), trailingTrivia: SpaceTrivia()))))
            );

    [Fact]
    public void TestAnonymousMethod()
        => Verify(
            @"Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0()",
            methodDeclaration: MethodDeclaration(
                QualifiedName("Microsoft.VisualStudio.DesignTools.SurfaceDesigner.Tools.EventRouter.ScopeElement_MouseUp.AnonymousMethod__0"),
                argumentList: EmptyParams)
        );

    [Fact]
    public void TestFileInformation()
        => Verify(
            @"M.M() in C:\folder\m.cs:line 1",
            methodDeclaration: MethodDeclaration(
                QualifiedName("M.M"),
                argumentList: EmptyParams),

            fileInformation: FileInformation(
                Path(@"C:\folder\m.cs"),
                ColonToken,
                Line(1))
        );

    [Fact]
    public void TestFileInformation_PartialPath()
        => Verify(@"M.M() in C:\folder\m.cs:line", expectFailure: true);

    [Fact]
    public void TestFileInformation_PartialPath2()
        => Verify(@"M.M() in C:\folder\m.cs:", expectFailure: true);

    [Fact]
    public void TestFileInformation_PartialPath3()
        => Verify(@"M.M() in C:\folder\m.cs:[trailingtrivia]", expectFailure: true);

    [Theory]
    [InlineData(@"C:\folder\m.cs", 1)]
    [InlineData(@"m.cs", 1)]
    [InlineData(@"C:\folder\m.cs", 123456789)]
    [InlineData(@"..\m.cs", 1)]
    [InlineData(@".\m.cs", 1)]
    public void TestFilePaths(string path, int line)
        => Verify(
            $"M.M() in {path}:line {line}",
            methodDeclaration: MethodDeclaration(
                QualifiedName("M.M"),
                argumentList: EmptyParams),

            fileInformation: FileInformation(
                Path(path),
                ColonToken,
                Line(line))
        );

    [Fact]
    public void TestFileInformation_TrailingTrivia()
        => Verify(
            @"M.M() in C:\folder\m.cs:line 1[trailingtrivia]",
            methodDeclaration: MethodDeclaration(
                QualifiedName("M.M"),
                argumentList: EmptyParams),

            fileInformation: FileInformation(
                Path(@"C:\folder\m.cs"),
                ColonToken,
                Line(1).With(trailingTrivia: CreateTriviaArray("[trailingtrivia]"))),

            eolTokenOpt: EOLToken
        );

    [Fact]
    public void TestFileInformation_InvalidDirectory()
        => Verify(@"M.M() in <\m.cs", expectFailure: true);

    [Theory]
    [InlineData("")]
    [InlineData("lkasjdlkfjalskdfj")]
    [InlineData("\n")]
    [InlineData("at ")]
    [InlineData(@"at M()")] // Method with no class is invalid
    [InlineData(@"at M.1c()")] // Invalid start character for identifier
    [InlineData(@"at 1M.C()")]
    [InlineData(@"at M.C(string& s)")] // "string&" represents a reference (ref, out) and is not supported yet
    [InlineData(@"at StreamJsonRpc.JsonRpc.<InvokeCoreAsync>d__139`1.MoveNext()")] // Generated/Inline methods are not supported yet
    [InlineData(@"at M(")] // Missing closing paren
    [InlineData(@"at M)")] // MIssing open paren
    [InlineData(@"at M.M[T>(T t)")] // Mismatched generic opening/close
    [InlineData(@"at M.M<T](T t)")] // Mismatched generic opening/close
    [InlineData(@"at M.M<T, U<V>>(T t)")] // Invalid nested generics
    [InlineData("at M.M(T<U> t)")] // Invalid generic in parameter
    [InlineData(@"at M.M(string[ s)")] // Opening array bracket no close
    [InlineData(@"at M.M(string] s)")] // Close only array bracket
    [InlineData(@"at M.M(string[][][ s)")]
    [InlineData(@"at M.M(string[[]] s)")]
    [InlineData("at M.M(string s, string t,")] // Trailing comma in parameters
    [InlineData(@"at M.N`.P()")] // Missing numeric for arity 
    [InlineData(@"at M.N`9N.P()")] // Invalid character after arity
    [InlineData("M.N.P.()")] // Trailing . with no identifier before arguments
    [InlineData("M.N(X.Y. x)")] // Trailing . in argument type
    [InlineData("M.N[T.Y]()")] // Generic type arguments should not be qualified types
    [InlineData("M.N(X.Y x.y)")] // argument names should not be qualified
    [InlineData("M.N(params)")] // argument with type but no name
    [InlineData("M.N [T]()")] // Space between identifier and bracket
    [InlineData("M.N(string [] s)")] // Space between type and array brackets
    [InlineData("M.N ()")] // Space between method declaration and parameters
    [InlineData("M.N .O.P(string s)")] // Space in type qualified name
    [InlineData("\r\nM.N()")]
    [InlineData("\nM.N()")]
    [InlineData("\rM.N()")]
    [InlineData("M.N(\r\n)")]
    [InlineData("M.N(\r)")]
    [InlineData("M.N(\n)")]
    [InlineData("at M..ctor.N()")] // Constructor on lhs of qualified name
    public void TestInvalidInputs(string input)
        => Verify(input, expectFailure: true);

    [Theory]
    [InlineData("at ")]
    [InlineData("in ")]
    [InlineData("line ")]
    public void TestKeywordsAsIdentifiers(string keyword)
        => Verify(@$"MyNamespace.MyType.MyMethod[{keyword}]({keyword} {keyword})",
            methodDeclaration: MethodDeclaration(
                QualifiedName("MyNamespace.MyType.MyMethod"),
                typeArguments: TypeArgumentList(TypeArgument(IdentifierToken(keyword.Trim(), trailingTrivia: SpaceTrivia()))),
                argumentList: ParameterList(
                    Parameter(
                        Identifier(keyword.Trim()),
                        IdentifierToken(keyword.Trim(), leadingTrivia: SpaceTrivia(2), trailingTrivia: SpaceTrivia()))))
            );

    [Fact]
    public void TestGeneratedMain()
        => Verify(@"Program.<Main>$(String[] args)",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    Identifier("Program"),
                    GeneratedName("Main")),
                argumentList: ParameterList(
                        Parameter(ArrayType(Identifier("String"), ArrayRankSpecifier(trailingTrivia: SpaceTrivia())),
                        IdentifierToken("args")))
                )
            );

    [Fact]
    public void TestLocalMethod()
        => Verify(@"C.<M>g__Local|0_0()",
            methodDeclaration: MethodDeclaration(
                QualifiedName(
                    Identifier("C"),
                    LocalMethod(
                        GeneratedName("M", endWithDollar: false),
                        "Local",
                        "0_0"))
                )
            );

    [Theory]
    [InlineData("v", "v", "řádek")] // Czech
    [InlineData("bei", "in", "Zeile")] // German
    [InlineData("en", "en", "línea")] // Spanish
    [InlineData("à", "dans", "ligne")] // French
    [InlineData("in", "in", "riga")] // Italian
    [InlineData("場所", "場所", "行")] // Japanese
    [InlineData("위치:", "파일", "줄")] // Korean
    [InlineData("w", "w", "wiersz")] // Polish
    [InlineData("em", "na", "linha")] // Portuguese (Brazil)
    [InlineData("в", "в", "строка")] // Russian
    [InlineData("在", "位置", "行号")] // Chinese (Simplified)
    [InlineData("於", "於", " 行")] // Chinese (Traditional)
    public void TestLanguages(string at, string @in, string line)
        => Verify(@$"{at} Program.Main() {@in} C:\repos\languages\Program.cs:{line} 16",
            methodDeclaration: MethodDeclaration(
                QualifiedName("Program.Main",
                    leadingTrivia: CreateTrivia(StackFrameKind.AtTrivia, $"{at} "))),

            fileInformation: FileInformation(
                Path(@"C:\repos\languages\Program.cs"),
                ColonToken,
                line: CreateToken(StackFrameKind.NumberToken, "16", leadingTrivia: [CreateTrivia(StackFrameKind.LineTrivia, $"{line} ")]),
                inTrivia: CreateTrivia(StackFrameKind.InTrivia, $" {@in} "))
                );
}
