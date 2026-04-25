// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Razor.Compiler.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Analyzers.Tests
{
    public class ComponentParameterNullableWarningSuppressorTests
    {
        [Fact]
        public async Task ParameterEditorRequiredNoWarning()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        [Fact]
        public async Task NoEditorRequiredStillReports()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task NoParameterRequiredStillReports()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [EditorRequired]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task NotComponentStillReports()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task AliasedAttributes()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;
                using MyParameter = Microsoft.AspNetCore.Components.ParameterAttribute;
                using MyRequired = Microsoft.AspNetCore.Components.EditorRequiredAttribute;

                public class MyComponent : ComponentBase
                {
                    [MyParameter, MyRequired]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(10, 19, 10, 30).WithSpan(10, 19, 10, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        [Fact]
        public async Task LocallyDefinedAttributes()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;
                
                public class MyComponent : IComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }

                namespace Microsoft.AspNetCore.Components
                {
                    public class ParameterAttribute : Attribute { }
                    public class EditorRequiredAttribute : Attribute { }
                    public interface IComponent { }
                }

                """;

            await VerifyAnalyzerAsync(testCode, extraReferences: [],
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        [Fact]
        public async Task LocallyDefinedAttributesDifferentNamespace()
        {
            var testCode = """
                #nullable enable
                using System;
                using MyNamespace;
                
                public class MyComponent : IComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }

                namespace MyNamespace
                {
                    public class ParameterAttribute : Attribute { }
                    public class EditorRequiredAttribute : Attribute { }
                    public interface IComponent { }
                }

                """;

            await VerifyAnalyzerAsync(testCode, extraReferences: [],
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task LocallyDefinedAttributesAndSdkAttributes()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;
                
                public class MyComponent : IComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }

                namespace Microsoft.AspNetCore.Components
                {
                    public class ParameterAttribute : Attribute { }
                    public class EditorRequiredAttribute : Attribute { }
                    public interface IComponent { }
                }

                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(8,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true),
                // /0/Test0.cs(7,6): warning CS0436: The type 'ParameterAttribute' in '/0/Test0.cs' conflicts with the imported type 'ParameterAttribute' in 'Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. Using the type defined in '/0/Test0.cs'.
                DiagnosticResult.CompilerWarning("CS0436").WithSpan(7, 6, 7, 15).WithArguments("/0/Test0.cs", "Microsoft.AspNetCore.Components.ParameterAttribute", "Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "Microsoft.AspNetCore.Components.ParameterAttribute"),
                // /0/Test0.cs(7,17): warning CS0436: The type 'EditorRequiredAttribute' in '/0/Test0.cs' conflicts with the imported type 'EditorRequiredAttribute' in 'Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. Using the type defined in '/0/Test0.cs'.
                DiagnosticResult.CompilerWarning("CS0436").WithSpan(7, 17, 7, 31).WithArguments("/0/Test0.cs", "Microsoft.AspNetCore.Components.EditorRequiredAttribute", "Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "Microsoft.AspNetCore.Components.EditorRequiredAttribute"),
                // /0/Test0.cs(5,28): warning CS0436: The type 'IComponent' in '/0/Test0.cs' conflicts with the imported type 'IComponent' in 'Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. Using the type defined in '/0/Test0.cs'.
                DiagnosticResult.CompilerWarning("CS0436").WithSpan(5, 28, 5, 38).WithArguments("/0/Test0.cs", "Microsoft.AspNetCore.Components.IComponent", "Microsoft.AspNetCore.Components, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "Microsoft.AspNetCore.Components.IComponent")
                );
        }

        [Theory]
        [InlineData("internal")]
        [InlineData("private")]
        [InlineData("protected internal")]
        [InlineData("protected")]
        [InlineData("public static")]
        public async Task IncorrectModifiersStillReport(string modifiers)
        {
            var testCode = $$"""
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    {{modifiers}}
                    string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(9, 12, 9, 23).WithSpan(9, 12, 9, 23).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("private set;")]
        [InlineData("private init;")]
        public async Task IncorrectSetterStillReport(string setter)
        {
            var testCode = $$"""
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; {{setter}} }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task RequiredPropertyDoesNotReport()
        {
            var testCode = $$"""
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public required string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task DerivedBaseType()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public abstract class BaseComponent : ComponentBase
                {
                }

                public class MyComponent : BaseComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(12,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(12, 19, 12, 30).WithSpan(12, 19, 12, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        [Fact]
        public async Task DerivedBaseTypeWithBaseParameter()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public abstract class BaseComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }

                public class MyComponent : BaseComponent
                {
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(8,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        [Fact]
        public async Task DerivedBaseTypeNotComponentWithBaseParameter()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public abstract class BaseComponent
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; set; }
                }

                public class MyComponent : BaseComponent, IComponent
                {
                    public void Attach(RenderHandle renderHandle) => throw null!;
                    public System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters) => throw null!;
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(8,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(false)
                );
        }

        [Fact]
        public async Task NullableReturnType()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public string? MyParameter { get; set; }
                }
                """;

            await VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ParameterWithInit()
        {
            var testCode = """
                #nullable enable
                using System;
                using Microsoft.AspNetCore.Components;

                public class MyComponent : ComponentBase
                {
                    [Parameter, EditorRequired]
                    public string MyParameter { get; init; }
                }
                """;

            await VerifyAnalyzerAsync(testCode,
                // /0/Test0.cs(9,19): warning CS8618: Non-nullable property 'MyParameter' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                DiagnosticResult.CompilerWarning("CS8618").WithSpan(8, 19, 8, 30).WithSpan(8, 19, 8, 30).WithArguments("property", "MyParameter").WithIsSuppressed(true)
                );
        }

        private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
            => VerifyAnalyzerAsync(source,
                                   Basic.Reference.Assemblies.AspNet80.References.All,
                                   expected);

        private static async Task VerifyAnalyzerAsync(string source, ImmutableArray<PortableExecutableReference> extraReferences, params DiagnosticResult[] expected)
        {
            var test = new CSharpAnalyzerTest<ComponentParameterNullableWarningSuppressor, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.Warnings,
                DisabledDiagnostics = { "CS1591" }, // Missing XML comment for publicly visible type or member
            };

            test.TestState.AdditionalReferences.AddRange(extraReferences);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
