// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class ParameterWrappingTests : CSharpFormattingTestBase
{
    private readonly OptionsCollection WrapParametersEnabled = new(LanguageNames.CSharp)
    {
        { WrapParameters, true }
    };

    private readonly OptionsCollection WrapParametersDisabled = new(LanguageNames.CSharp)
    {
        { WrapParameters, false }
    };

    private readonly OptionsCollection WrapAndAlignParametersEnabled = new(LanguageNames.CSharp)
    {
        { WrapParameters, true },
        { AlignWrappedParameters, true }
    };

    private readonly OptionsCollection WrapAndAlignAndNewLineParametersEnabled = new(LanguageNames.CSharp)
    {
        { WrapParameters, true },
        { AlignWrappedParameters, true },
        { WrapParametersOnNewLine, true }
    };

    private readonly OptionsCollection WrapAndNewLineParametersEnabled = new(LanguageNames.CSharp)
    {
        { WrapParameters, true },
        { AlignWrappedParameters, false },
        { WrapParametersOnNewLine, true }
    };

    private readonly OptionsCollection AlignWrappedParametersOnly = new(LanguageNames.CSharp)
    {
        { WrapParameters, false },
        { AlignWrappedParameters, true }
    };

    [Fact]
    public async Task TestBasicParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyVerySillyMethod(int param1,
                    int param2,
                    int param3,
                    int param4)
                {
                }
            }
            """, """
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestBasicParameterWrapping_WrapDisabled()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapParametersDisabled);
    }

    [Fact]
    public async Task TestParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyVerySillyMethod(int param1,
                                               int param2,
                                               int param3,
                                               int param4)
                {
                }
            }
            """, """
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestParameterWrappingWithAlignmentAndNewLine_WrapAndAlignAndNewLineEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyVerySillyMethod(
                                                int param1,
                                                int param2,
                                                int param3,
                                                int param4
                )
                {
                }
            }
            """, """
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapAndAlignAndNewLineParametersEnabled);
    }

    [Fact]
    public async Task TestParameterWrappingWithNewLine_WrapAndNewLineEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyVerySillyMethod(
                    int param1,
                    int param2,
                    int param3,
                    int param4
                )
                {
                }
            }
            """, """
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapAndNewLineParametersEnabled);
    }

    [Fact]
    public async Task TestMethodCallWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1,
                        param2,
                        param3,
                        param4);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1, param2, param3, param4);
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestMethodCallWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1,
                                      param2,
                                      param3,
                                      param4);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1, param2, param3, param4);
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestMethodCallWrappingWithAlignmentAndNewLine_WrapAndAlignAndNewLineEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    MyVerySillyMethod(
                                        param1,
                                        param2,
                                        param3,
                                        param4
                    );
                }
            }
            """, """
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1, param2, param3, param4);
                }
            }
            """, WrapAndAlignAndNewLineParametersEnabled);
    }

    [Fact]
    public async Task TestMethodCallWrappingWithNewLine_WrapAndNewLineEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    MyVerySillyMethod(
                        param1,
                        param2,
                        param3,
                        param4
                    );
                }
            }
            """, """
            class C
            {
                void M()
                {
                    MyVerySillyMethod(param1, param2, param3, param4);
                }
            }
            """, WrapAndNewLineParametersEnabled);
    }

    [Fact]
    public async Task TestAlignmentOnlyWithoutWrapping_HasNoEffect()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                private void MyVerySillyMethod(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, AlignWrappedParametersOnly);
    }

    [Fact]
    public async Task TestSingleParameterMethod_NoWrapping()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                private void MySingleParamMethod(int param1)
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestNoParameterMethod_NoWrapping()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                private void MyNoParamMethod()
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestConstructorParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public C(int param1,
                    int param2,
                    int param3,
                    int param4)
                {
                }
            }
            """, """
            class C
            {
                public C(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestConstructorParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public C(int param1,
                         int param2,
                         int param3,
                         int param4)
                {
                }
            }
            """, """
            class C
            {
                public C(int param1, int param2, int param3, int param4)
                {
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestIndexerParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public int this[int param1,
                    int param2,
                    int param3] => 0;
            }
            """, """
            class C
            {
                public int this[int param1, int param2, int param3] => 0;
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestIndexerParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public int this[int param1,
                                int param2,
                                int param3] => 0;
            }
            """, """
            class C
            {
                public int this[int param1, int param2, int param3] => 0;
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestIndexerCallWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = obj[param1,
                        param2,
                        param3];
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = obj[param1, param2, param3];
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestIndexerCallWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = obj[param1,
                                param2,
                                param3];
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = obj[param1, param2, param3];
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestGenericMethodParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public void MyGenericMethod<T>(T param1,
                    T param2,
                    T param3,
                    T param4)
                {
                }
            }
            """, """
            class C
            {
                public void MyGenericMethod<T>(T param1, T param2, T param3, T param4)
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestGenericMethodParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public void MyGenericMethod<T>(T param1,
                                               T param2,
                                               T param3,
                                               T param4)
                {
                }
            }
            """, """
            class C
            {
                public void MyGenericMethod<T>(T param1, T param2, T param3, T param4)
                {
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestDelegateParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public delegate void MyDelegate(int param1,
                    int param2,
                    int param3,
                    int param4);
            }
            """, """
            class C
            {
                public delegate void MyDelegate(int param1, int param2, int param3, int param4);
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestDelegateParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                public delegate void MyDelegate(int param1,
                                                int param2,
                                                int param3,
                                                int param4);
            }
            """, """
            class C
            {
                public delegate void MyDelegate(int param1, int param2, int param3, int param4);
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestLambdaParameterWrapping_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var lambda = (int param1,
                        int param2,
                        int param3,
                        int param4) => param1 + param2 + param3 + param4;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var lambda = (int param1, int param2, int param3, int param4) => param1 + param2 + param3 + param4;
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestLambdaParameterWrappingWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var lambda = (int param1,
                                  int param2,
                                  int param3,
                                  int param4) => param1 + param2 + param3 + param4;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var lambda = (int param1, int param2, int param3, int param4) => param1 + param2 + param3 + param4;
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestComplexParameterTypes_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyComplexMethod(Dictionary<string, List<int>> param1,
                    Func<int, string, bool> param2,
                    Action<string> param3,
                    object param4)
                {
                }
            }
            """, """
            class C
            {
                private void MyComplexMethod(Dictionary<string, List<int>> param1, Func<int, string, bool> param2, Action<string> param3, object param4)
                {
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestComplexParameterTypesWithAlignment_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                private void MyComplexMethod(Dictionary<string, List<int>> param1,
                                             Func<int, string, bool> param2,
                                             Action<string> param3,
                                             object param4)
                {
                }
            }
            """, """
            class C
            {
                private void MyComplexMethod(Dictionary<string, List<int>> param1, Func<int, string, bool> param2, Action<string> param3, object param4)
                {
                }
            }
            """, WrapAndAlignParametersEnabled);
    }

    [Fact]
    public async Task TestParameterWrappingPreservesExistingWrapping_WrapDisabled()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                private void MyVerySillyMethod(int param1,
                    int param2,
                    int param3,
                    int param4)
                {
                }
            }
            """, WrapParametersDisabled);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapOnly()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_parameters = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M(int a,
                    int b,
                    int c)
                {
                    M(1,
                        2,
                        3);
                }
            }
            """, WrapParametersEnabled, editorConfig);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapAndAlign()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_parameters = true
            csharp_align_wrapped_parameters = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M(int a,
                       int b,
                       int c)
                {
                    M(1,
                      2,
                      3);
                }
            }
            """, WrapAndAlignParametersEnabled, editorConfig);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapAndAlignOnNewLine()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_parameters = true
            csharp_align_wrapped_parameters = true
            csharp_wrap_parameters_on_new_line = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M(
                    int a,
                    int b,
                    int c)
                {
                    M(
                        1,
                        2,
                        3);
                }
            }
            """, WrapAndAlignAndNewLineParametersEnabled, editorConfig);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapOnNewLine()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_parameters = true
            csharp_wrap_parameters_on_new_line = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M(
                    int a,
                    int b,
                    int c)
                {
                    M(
                        1,
                        2,
                        3);
                }
            }
            """, WrapAndNewLineParametersEnabled, editorConfig);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_ComplexMethod()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_parameters = true
            csharp_align_wrapped_parameters = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void ComplexMethod(string longParameterName, int anotherParameter, bool yetAnotherParameter, double finalParameter)
                {
                    ComplexMethod("long string value", 42, true, 3.14);
                }
            }
            """, """
            class C
            {
                void ComplexMethod(string longParameterName,
                                   int anotherParameter,
                                   bool yetAnotherParameter,
                                   double finalParameter)
                {
                    ComplexMethod("long string value",
                                  42,
                                  true,
                                  3.14);
                }
            }
            """, WrapAndAlignParametersEnabled, editorConfig);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_DisabledByDefault()
    {
        // Test that parameter wrapping is disabled by default
        var editorConfig = """
            [*.cs]
            # No parameter wrapping options specified
            """;

        await AssertFormatAsync("""
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M(int a, int b, int c)
                {
                    M(1, 2, 3);
                }
            }
            """, new OptionsCollection(LanguageNames.CSharp), editorConfig);
    }

    [Fact]
    public async Task TestRealWorldExample_WrapEnabled()
    {
        await AssertFormatAsync("""
            class FileProcessor
            {
                public void ProcessFile(string filePath,
                    Encoding encoding,
                    CancellationToken cancellationToken,
                    IProgress<int> progress)
                {
                    // Implementation
                }
            }
            """, """
            class FileProcessor
            {
                public void ProcessFile(string filePath, Encoding encoding, CancellationToken cancellationToken, IProgress<int> progress)
                {
                    // Implementation
                }
            }
            """, WrapParametersEnabled);
    }

    [Fact]
    public async Task TestRealWorldExample_WrapAndAlignEnabled()
    {
        await AssertFormatAsync("""
            class FileProcessor
            {
                public void ProcessFile(string filePath,
                                        Encoding encoding,
                                        CancellationToken cancellationToken,
                                        IProgress<int> progress)
                {
                    // Implementation
                }
            }
            """, """
            class FileProcessor
            {
                public void ProcessFile(string filePath, Encoding encoding, CancellationToken cancellationToken, IProgress<int> progress)
                {
                    // Implementation
                }
            }
            """, WrapAndAlignParametersEnabled);
    }
} 