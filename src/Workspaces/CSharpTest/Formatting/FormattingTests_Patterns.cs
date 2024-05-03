// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingTests_Patterns : CSharpFormattingTestBase
    {
        [Theory, CombinatorialData]
        public async Task FormatRelationalPatterns1(
            [CombinatorialValues("<", "<=", ">", ">=")] string operatorText,
            BinaryOperatorSpacingOptions spacing)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  {operatorText}  3  or  {operatorText}  5;
    }}
}}
";

            var expectedSingle = $@"
class A
{{
    bool Method(int value)
    {{
        return value is {operatorText} 3 or {operatorText} 5;
    }}
}}
";
            var expectedIgnore = $@"
class A
{{
    bool Method(int value)
    {{
        return value is  {operatorText}  3  or  {operatorText}  5;
    }}
}}
";
            var expectedRemove = $@"
class A
{{
    bool Method(int value)
    {{
        return value is {operatorText}3 or {operatorText}5;
    }}
}}
";

            var expected = spacing switch
            {
                BinaryOperatorSpacingOptions.Single => expectedSingle,
                BinaryOperatorSpacingOptions.Ignore => expectedIgnore,
                BinaryOperatorSpacingOptions.Remove => expectedRemove,
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, CombinatorialData]
        public async Task FormatRelationalPatterns2(
            [CombinatorialValues("<", "<=", ">", ">=")] string operatorText,
            BinaryOperatorSpacingOptions spacing,
            bool spaceWithinExpressionParentheses)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  (  {operatorText}  3  )  or  (  {operatorText}  5  )  ;
    }}
}}
";

            var expectedSingleFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText} 3) or ({operatorText} 5);
    }}
}}
";
            var expectedIgnoreFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText}  3)  or  ({operatorText}  5);
    }}
}}
";
            var expectedRemoveFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText}3) or ({operatorText}5);
    }}
}}
";
            var expectedSingleTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText} 3 ) or ( {operatorText} 5 );
    }}
}}
";
            var expectedIgnoreTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText}  3 )  or  ( {operatorText}  5 );
    }}
}}
";
            var expectedRemoveTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText}3 ) or ( {operatorText}5 );
    }}
}}
";

            var expected = (spacing, spaceWithinExpressionParentheses) switch
            {
                (BinaryOperatorSpacingOptions.Single, false) => expectedSingleFalse,
                (BinaryOperatorSpacingOptions.Ignore, false) => expectedIgnoreFalse,
                (BinaryOperatorSpacingOptions.Remove, false) => expectedRemoveFalse,
                (BinaryOperatorSpacingOptions.Single, true) => expectedSingleTrue,
                (BinaryOperatorSpacingOptions.Ignore, true) => expectedIgnoreTrue,
                (BinaryOperatorSpacingOptions.Remove, true) => expectedRemoveTrue,
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
                { CSharpFormattingOptions2.SpaceBetweenParentheses, CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue.WithFlagValue(SpacePlacementWithinParentheses.Expressions, spaceWithinExpressionParentheses) },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, CombinatorialData]
        public async Task FormatNotPatterns1(BinaryOperatorSpacingOptions spacing)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  not  3  or  not  5;
    }}
}}
";

            var expectedSingle = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3 or not 5;
    }}
}}
";
            var expectedIgnore = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3  or  not 5;
    }}
}}
";
            var expectedRemove = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3 or not 5;
    }}
}}
";

            var expected = spacing switch
            {
                BinaryOperatorSpacingOptions.Single => expectedSingle,
                BinaryOperatorSpacingOptions.Ignore => expectedIgnore,
                BinaryOperatorSpacingOptions.Remove => expectedRemove,
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, CombinatorialData]
        public async Task FormatNotPatterns2(
            BinaryOperatorSpacingOptions spacing,
            bool spaceWithinExpressionParentheses)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  (  not  3  )  or  (  not  5  );
    }}
}}
";

            var expectedSingleFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3) or (not 5);
    }}
}}
";
            var expectedIgnoreFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3)  or  (not 5);
    }}
}}
";
            var expectedRemoveFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3) or (not 5);
    }}
}}
";
            var expectedSingleTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 ) or ( not 5 );
    }}
}}
";
            var expectedIgnoreTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 )  or  ( not 5 );
    }}
}}
";
            var expectedRemoveTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 ) or ( not 5 );
    }}
}}
";

            var expected = (spacing, spaceWithinExpressionParentheses) switch
            {
                (BinaryOperatorSpacingOptions.Single, false) => expectedSingleFalse,
                (BinaryOperatorSpacingOptions.Ignore, false) => expectedIgnoreFalse,
                (BinaryOperatorSpacingOptions.Remove, false) => expectedRemoveFalse,
                (BinaryOperatorSpacingOptions.Single, true) => expectedSingleTrue,
                (BinaryOperatorSpacingOptions.Ignore, true) => expectedIgnoreTrue,
                (BinaryOperatorSpacingOptions.Remove, true) => expectedRemoveTrue,
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
                { CSharpFormattingOptions2.SpaceBetweenParentheses, CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue.WithFlagValue(SpacePlacementWithinParentheses.Expressions, spaceWithinExpressionParentheses) },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46284")]
        public async Task FormatMultiLinePattern1()
        {
            var content = @"
class TypeName
{
    bool MethodName(string value)
    {
        return value is object
               && value is
                 {
                     Length: 2,
                 };
    }
}
";
            var expected = @"
class TypeName
{
    bool MethodName(string value)
    {
        return value is object
               && value is
               {
                   Length: 2,
               };
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46284")]
        public async Task FormatMultiLinePattern2()
        {
            var content = @"
class TypeName
{
    private static bool IsCallingConventionModifier(CustomModifier modifier)
    {
        var modifierType = ((CSharpCustomModifier)modifier).ModifierSymbol;
        return (object)modifierType.ContainingAssembly == modifierType.ContainingAssembly.CorLibrary
               && modifierType.Name != ""CallConv""
               && modifierType.Arity == 0
               && modifierType.Name.StartsWith(""CallConv"", StringComparison.Ordinal)
               && modifierType.ContainingNamespace is
                  {
                      Name: ""CompilerServices"",
                      ContainingNamespace:
                      {
                          Name: ""Runtime"",
                          ContainingNamespace:
                          {
                              Name: ""System"",
                              ContainingNamespace: { IsGlobalNamespace: true }
                          }
                      }
                  };
    }
}
";
            var expected = @"
class TypeName
{
    private static bool IsCallingConventionModifier(CustomModifier modifier)
    {
        var modifierType = ((CSharpCustomModifier)modifier).ModifierSymbol;
        return (object)modifierType.ContainingAssembly == modifierType.ContainingAssembly.CorLibrary
               && modifierType.Name != ""CallConv""
               && modifierType.Arity == 0
               && modifierType.Name.StartsWith(""CallConv"", StringComparison.Ordinal)
               && modifierType.ContainingNamespace is
               {
                   Name: ""CompilerServices"",
                   ContainingNamespace:
                   {
                       Name: ""Runtime"",
                       ContainingNamespace:
                       {
                           Name: ""System"",
                           ContainingNamespace: { IsGlobalNamespace: true }
                       }
                   }
               };
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46284")]
        public async Task FormatMultiLinePattern3()
        {
            var content = @"
class TypeName
{
    private static bool IsCallingConventionModifier(CustomModifier modifier)
    {
        var modifierType = ((CSharpCustomModifier)modifier).ModifierSymbol;
        return (object)modifierType.ContainingAssembly == modifierType.ContainingAssembly.CorLibrary
               && modifierType.Name != ""CallConv""
               && modifierType.Arity == 0
               && modifierType.Name.StartsWith(""CallConv"", StringComparison.Ordinal)
               && modifierType.ContainingNamespace is
{
Name: ""CompilerServices"",
ContainingNamespace:
{
Name: ""Runtime"",
ContainingNamespace:
{
Name: ""System"",
ContainingNamespace: { IsGlobalNamespace: true }
}
}
};
    }
}
";
            var expected = @"
class TypeName
{
    private static bool IsCallingConventionModifier(CustomModifier modifier)
    {
        var modifierType = ((CSharpCustomModifier)modifier).ModifierSymbol;
        return (object)modifierType.ContainingAssembly == modifierType.ContainingAssembly.CorLibrary
               && modifierType.Name != ""CallConv""
               && modifierType.Arity == 0
               && modifierType.Name.StartsWith(""CallConv"", StringComparison.Ordinal)
               && modifierType.ContainingNamespace is
               {
                   Name: ""CompilerServices"",
                   ContainingNamespace:
                   {
                       Name: ""Runtime"",
                       ContainingNamespace:
                       {
                           Name: ""System"",
                           ContainingNamespace: { IsGlobalNamespace: true }
                       }
                   }
               };
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatMultiLinePattern4()
        {
            var content = @"
class TypeName
{
    void MethodName(string value)
    {
        if (value is
                 {
                     Length: 2,
                 })
{
}
    }
}
";
            var expected = @"
class TypeName
{
    void MethodName(string value)
    {
        if (value is
            {
                Length: 2,
            })
        {
        }
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatMultiLinePattern5()
        {
            var content = @"
class TypeName
{
    void MethodName(string value)
    {
        while (value is
                 {
                     Length: 2,
                 })
{
}
    }
}
";
            var expected = @"
class TypeName
{
    void MethodName(string value)
    {
        while (value is
            {
                Length: 2,
            })
        {
        }
    }
}
";

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatNestedListPattern1()
        {
            var content = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [ [  ]  ])
                        {

                        }
                    }
                }
                """;

            var expected = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [[]])
                        {
                
                        }
                    }
                }
                """;

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatNestedListPattern2()
        {
            var content = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [ [  ],[ ]     ])
                        {

                        }
                    }
                }
                """;

            var expected = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [[], []])
                        {
                
                        }
                    }
                }
                """;

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatNestedListPattern3()
        {
            var content = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [    [  ],[ ]     , [   ]  ] )
                        {

                        }
                    }
                }
                """;

            var expected = """
                class C
                {
                    void M(string[] ss)
                    {
                        if (ss is [[], [], []])
                        {
                
                        }
                    }
                }
                """;

            await AssertFormatAsync(expected, content);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42861")]
        public async Task FormatNestedListPattern4()
        {
            var content = """
                class C
                {
                    void M(string[][] ss)
                    {
                        if (ss is [    [ [ ] ] ] )
                        {

                        }
                    }
                }
                """;

            var expected = """
                class C
                {
                    void M(string[][] ss)
                    {
                        if (ss is [[[]]])
                        {
                
                        }
                    }
                }
                """;

            await AssertFormatAsync(expected, content);
        }
    }
}
