// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class GetUnusedImportDirectivesTests : SemanticModelTestBase
    {
        [Fact]
        public void UnusedUsing1()
        {
            var text = @"
using System;

class C
{
    void Goo()
    {
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);

            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
        }

        [WorkItem(865627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865627")]
        [Fact]
        public void TestUnusedExtensionMarksImportsAsUsed()
        {
            string class1Source = @"using System;

namespace ClassLibrary1
{
    public class Class1
    {
        public void Method1(string arg1)
        {
            Console.WriteLine(arg1);
        }
    }
} 
";
            var classLib1 = CreateCompilation(source: class1Source, assemblyName: "ClassLibrary1");

            string class2Source = @"using System;
using ClassLibrary1;

namespace ClassLibrary2
{
    public static class Class2
    {
        public static void Method1(this Class1 arg0, string arg1)
        {
            Console.Write(""Erroneous: "" + arg1);
        }
    }
}";
            var classLib2 = CreateCompilation(source: class2Source, assemblyName: "ClassLibrary2", references: new[] { classLib1.ToMetadataReference() });

            string consoleApplicationSource = @"using ClassLibrary2;
using ClassLibrary1;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var instance1 = new Class1();
            instance1.Method1(""Argument1"");
        }
    }
}";
            var tree = Parse(consoleApplicationSource);
            var comp = CreateCompilation(tree, new[] { classLib1.ToMetadataReference(), classLib2.ToMetadataReference() }, assemblyName: "ConsoleApplication");
            var model = comp.GetSemanticModel(tree) as CSharpSemanticModel;

            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;

            //This is the crux of the test.
            //Without this line, with or without the fix, the model never gets pushed to evaluate extension method candidates
            //and therefore never marked ClassLibrary2 as a used import in consoleApplication.
            //Without the fix, this call used to result in ClassLibrary2 getting marked as used, after the fix, this call does not
            //result in changing ClassLibrary2's used status.
            model.GetMemberGroup(syntax);

            model.GetDiagnostics().Verify(Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using ClassLibrary2;"));
        }

        [WorkItem(747219, "DevDiv2/DevDiv")]
        [Fact]
        public void UnusedUsing747219()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
 
class Program
{
    static void Main(string[] args)
    {
        Enumerable.Repeat(1, 1);
    }
}";
            var comp = CreateEmptyCompilation(text, new[] { MscorlibRef });
            //all unused because system.core was not included and Enumerable didn't bind
            comp.VerifyDiagnostics(
                // (4,14): error CS0234: The type or namespace name 'Linq' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // using System.Linq;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Linq").WithArguments("Linq", "System"),
                // (11,9): error CS0103: The name 'Enumerable' does not exist in the current context
                //         Enumerable.Repeat(1, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Enumerable").WithArguments("Enumerable"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using System.Linq;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using System.Threading.Tasks;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading.Tasks;")
                );

            comp = comp.WithReferences(comp.References.Concat(SystemCoreRef));
            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using System.Threading.Tasks;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading.Tasks;")
                );
        }

        [Fact]
        public void UsedUsing1()
        {
            var text = @"
using System;

class C
{
    void Goo()
    {
        Console.WriteLine();
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SpeculativeBindingDoesNotAffectResult()
        {
            var text = @"
using System;

class C
{
    void Goo()
    {
        /*here*/
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var position = text.IndexOf("/*here*/", StringComparison.Ordinal);
            var info = model.GetSpeculativeSymbolInfo(position, SyntaxFactory.IdentifierName("Console"), SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.NotNull(info.Symbol);
            Assert.Equal(SymbolKind.NamedType, info.Symbol.Kind);
            Assert.Equal("Console", info.Symbol.Name);
            Assert.Equal(SymbolKind.Namespace, info.Symbol.ContainingSymbol.Kind);
            Assert.Equal("System", info.Symbol.ContainingSymbol.Name);

            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;")
                );
        }

        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void AllAssemblyLevelAttributesMustBeBound()
        {
            var snkPath = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey).Path;

            var signing = Parse(@"
using System.Reflection;

[assembly: AssemblyVersion(""1.2.3.4"")]
[assembly: AssemblyKeyFile(@""" + snkPath + @""")]
");

            var ivtCompilation = CreateCompilation(
                assemblyName: "IVT",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(new DesktopStrongNameProvider()),
                source: new[]
                {
                    Parse(@"
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""Lib, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]

namespace NamespaceContainingInternalsOnly
{
    internal static class Extensions
    {
        internal static void Goo(this int x) {}
    }
}
"),
                    signing
                });

            var libCompilation = CreateCompilation(
                assemblyName: "Lib",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(new DesktopStrongNameProvider()),
                references: new[] { ivtCompilation.ToMetadataReference() },
                source: new[]
                {
                    Parse(@"
using NamespaceContainingInternalsOnly;

public class C
{
    internal static void F(int x)
    {
        x.Goo();
    }
}
"),
                    signing
                });

            libCompilation.VerifyDiagnostics();
        }

        [WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")]
        [Fact]
        public void SemanticModelCallDoesNotCountsAsUse()
        {
            var source = @"
using System.Collections;
using System.Collections.Generic;

class C 
{
    void M()
    {
        return;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;")
                );
        }

        [WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")]
        [Fact]
        public void INF_UnusedUsingDirective()
        {
            var source = @"
using System.Collections;
using C = System.Console;
";

            CreateCompilation(source).VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Collections;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using C = System.Console;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using C = System.Console;"));
        }

        [WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")]
        [Fact]
        public void INF_UnusedExternAlias()
        {
            var source = @"
extern alias A;
";
            var lib = CreateEmptyCompilation("", assemblyName: "lib");
            var comp = CreateCompilation(source, new[] { new CSharpCompilationReference(lib, aliases: ImmutableArray.Create("A")) });

            comp.VerifyDiagnostics(
                // (2,1): info CS8020: Unused extern alias.
                // extern alias A;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias A;"));
        }

        [WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")]
        [Fact]
        public void CrefCountsAsUse()
        {
            var source = @"
using System;

/// <see cref='Console'/>
public class C { }
";
            // Not reporting doc comment diagnostics. It is still a use.
            CreateCompilation(source).VerifyDiagnostics();

            // Binding doc comments.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics();
        }

        [WorkItem(770147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770147")]
        [Fact]
        public void InfoAndWarnAsError()
        {
            var source = @"
using System;
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
            comp.VerifyEmitDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithWarningAsError(false));
        }

        [Fact]
        public void UnusedUsingInteractive()
        {
            var tree = Parse("using System;", options: TestOptions.Script);
            var comp = CSharpCompilation.CreateScriptCompilation("sub1", tree, new[] { MscorlibRef_v4_0_30316_17626 });

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void UnusedUsingScript()
        {
            var tree = Parse("using System;", options: TestOptions.Script);
            var comp = CreateCompilationWithMscorlib45(new[] { tree });

            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));
        }

        [Fact, WorkItem(18348, "https://github.com/dotnet/roslyn/issues/18348")]
        public void IncorrectUnusedUsingWhenAttributeOnParameter_01()
        {
            var source1 =
@"using System.Runtime.InteropServices;

partial class Program
{
    partial void M([Out] [In] ref int x) { }
}";
            var source2 =
@"partial class Program
{
    partial void M(ref int x);
}";
            var comp = CreateCompilation(new[] { source1, source2 });
            var tree = comp.SyntaxTrees[0];
            //comp.VerifyDiagnostics(); // doing this first hides the symptoms of the bug
            var model = comp.GetSemanticModel(tree);

            // There should be no diagnostics.
            model.GetDiagnostics().Verify(
                //// (1,1): hidden CS8019: Unnecessary using directive.
                //// using System.Runtime.InteropServices;
                //Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Runtime.InteropServices;").WithLocation(1, 1)
                );
        }

        [Fact, WorkItem(18348, "https://github.com/dotnet/roslyn/issues/18348")]
        public void IncorrectUnusedUsingWhenAttributeOnParameter_02()
        {
            var source1 =
@"using System.Runtime.InteropServices;

partial class Program
{
    partial void M([Out] [In] ref int x);
}";
            var source2 =
@"partial class Program
{
    partial void M(ref int x) { }
}";
            var comp = CreateCompilation(new[] { source1, source2 });
            var tree = comp.SyntaxTrees[0];
            //comp.VerifyDiagnostics(); // doing this first hides the symptoms of the bug
            var model = comp.GetSemanticModel(tree);

            // There should be no diagnostics.
            model.GetDiagnostics().Verify(
                //// (1,1): hidden CS8019: Unnecessary using directive.
                //// using System.Runtime.InteropServices;
                //Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Runtime.InteropServices;").WithLocation(1, 1)
                );
        }

        [Fact, WorkItem(2773, "https://github.com/dotnet/roslyn/issues/2773")]
        public void UsageInDocComment()
        {
            var source = @"using X;

/// <summary/>
public class Program
{
    /// <summary>
    /// <see cref=""Q""/>
    /// </summary>
    static void Main(string[] args)
    {
    }
}

namespace X
{
    /// <summary/>
    public class Q { }
}
";
            foreach (DocumentationMode documentationMode in Enum.GetValues(typeof(DocumentationMode)))
            {
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithDocumentationMode(documentationMode));
                compilation.VerifyDiagnostics();
            }
        }
    }
}
