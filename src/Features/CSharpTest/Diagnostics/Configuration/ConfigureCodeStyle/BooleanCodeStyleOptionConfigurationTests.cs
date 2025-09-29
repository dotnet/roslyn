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
    public sealed class TrueConfigurationTests : BooleanCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 0;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_True()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/39466")]
        public Task ConfigureEditorconfig_RuleExists_True()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_True()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/39466")]
        public Task ConfigureEditorconfig_MaintainSeverity_True()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_True()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);
    }

    [Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public sealed class FalseConfigurationTests : BooleanCodeStyleOptionConfigurationTests
    {
        protected override int CodeActionIndex => 1;

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_Empty_False()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_False()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_False_NoSeveritySuffix()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_RuleExists_DotnetDiagnosticEntry()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_RuleExists_ConflitingDotnetDiagnosticEntry()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidHeader_False()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [Fact]
        public Task ConfigureEditorconfig_MaintainSeverity_False()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);

        [ConditionalFact(typeof(IsEnglishLocal))]
        public Task ConfigureEditorconfig_InvalidRule_False()
            => TestInRegularAndScriptAsync("""
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
                """, """
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
                """, CodeActionIndex);
    }
}
