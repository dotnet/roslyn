// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle;

public abstract partial class BooleanCodeStyleOptionConfigurationTests : AbstractSuppressionDiagnosticTest_NoEditor
{
    protected internal override string GetLanguage() => LanguageNames.CSharp;

    protected override ParseOptions GetScriptOptions() => Options.Script;

    internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
    {
        return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new CSharpUseObjectInitializerDiagnosticAnalyzer(), new ConfigureCodeStyleOptionCodeFixProvider());
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public class TrueConfigurationTests : BooleanCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 0;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_Empty_True()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = true
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/39466")]
        public async Task ConfigureEditorconfig_RuleExists_True()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_style_object_initializer = false:suggestion    ; Comment2
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]    # Comment1
                dotnet_style_object_initializer = true:suggestion    ; Comment2
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidHeader_True()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_style_object_initializer = false:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_style_object_initializer = false:suggestion

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = true
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/39466")]
        public async Task ConfigureEditorconfig_MaintainSeverity_True()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = false:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = true:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidRule_True()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = false:suggestion
                dotnet_style_object_initializerr = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = false:suggestion
                dotnet_style_object_initializerr = false

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = true
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public class FalseConfigurationTests : BooleanCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 1;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_Empty_False()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig"></AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_RuleExists_False()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = false:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_RuleExists_False_NoSeveritySuffix()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = true
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializer = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_RuleExists_DotnetDiagnosticEntry()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = warning

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_RuleExists_ConflitingDotnetDiagnosticEntry()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = error
                dotnet_style_object_initializer = true:warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_diagnostic.IDE0017.severity = error
                dotnet_style_object_initializer = false:warning
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidHeader_False()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_style_object_initializer = true:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.vb]
                dotnet_style_object_initializer = true:suggestion

                [*.{cs,vb}]

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [Fact]
        public async Task ConfigureEditorconfig_MaintainSeverity_False()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = true:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                         <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = new Customer();
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.{vb,cs}]
                dotnet_style_object_initializer = false:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public async Task ConfigureEditorconfig_InvalidRule_False()
        {
            var input = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = false:suggestion
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            var expected = """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        // dotnet_style_object_initializer = true
                        var obj = new Customer() { _age = 21 };

                        // dotnet_style_object_initializer = false
                        Customer obj2 = [|new Customer()|];
                        obj2._age = 21;
                    }

                    internal class Customer
                    {
                        public int _age;

                        public Customer()
                        {

                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath="z:\\.editorconfig">[*.cs]
                dotnet_style_object_initializerr = false:suggestion

                # IDE0017: Simplify object initialization
                dotnet_style_object_initializer = false
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScriptAsync(input, expected, CodeActionIndex);
        }
    }
}
