// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editing
{
    [UseExportProvider]
    public class AddImportsTests
    {
        private static async Task<Document> GetDocument(string code, bool withAnnotations)
        {
            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestMetadata.Net451.mscorlib }));

            var doc = emptyProject.AddDocument("test.cs", code);

            if (withAnnotations)
            {
                var root = await doc.GetSyntaxRootAsync();
                var model = await doc.GetSemanticModelAsync();

                root = root.ReplaceNodes(root.DescendantNodesAndSelf().OfType<TypeSyntax>(),
                    (o, c) =>
                    {
                        var symbol = model.GetSymbolInfo(o).Symbol;
                        return symbol != null
                            ? c.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol), Simplifier.Annotation)
                            : c;
                    });
                doc = doc.WithSyntaxRoot(root);
            }

            return doc;
        }

        private static Task TestNoImportsAddedAsync(
            string initialText,
            bool useSymbolAnnotations)
        {
            return TestAsync(initialText, initialText, initialText, useSymbolAnnotations, performCheck: false);
        }

        private static async Task TestAsync(
            string initialText,
            string importsAddedText,
            string simplifiedText,
            bool useSymbolAnnotations,
            bool placeSystemNamespaceFirst = true,
            bool placeImportsInsideNamespaces = false,
            bool performCheck = true)
        {
            var doc = await GetDocument(initialText, useSymbolAnnotations);

            var addImportOptions = new AddImportPlacementOptions(
                PlaceSystemNamespaceFirst: placeSystemNamespaceFirst,
                PlaceImportsInsideNamespaces: placeImportsInsideNamespaces,
                AllowInHiddenRegions: false);

            var imported = useSymbolAnnotations
                ? await ImportAdder.AddImportsFromSymbolAnnotationAsync(doc, addImportOptions, CancellationToken.None)
                : await ImportAdder.AddImportsFromSyntaxesAsync(doc, addImportOptions, CancellationToken.None);

            if (importsAddedText != null)
            {
                var formatted = await Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation);
                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(importsAddedText, actualText);
            }

            if (simplifiedText != null)
            {
                var reduced = await Simplifier.ReduceAsync(imported);
                var formatted = await Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation);

                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(simplifiedText, actualText);
            }

            if (performCheck)
            {
                if (initialText == importsAddedText && importsAddedText == simplifiedText)
                    throw new Exception($"use {nameof(TestNoImportsAddedAsync)}");
            }
        }

        public static object[][] TestAllData =
        {
            new object[] { false },
            new object[] { true },
        };

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddImport(bool useSymbolAnnotations)
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddSystemImportFirst(bool useSymbolAnnotations)
        {
            await TestAsync(
@"using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;
using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;
using N;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestDontAddSystemImportFirst(bool useSymbolAnnotations)
        {
            await TestAsync(
@"using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using N;
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using N;
using System.Collections.Generic;

class C
{
    public List<int> F;
}",
                useSymbolAnnotations,
                placeSystemNamespaceFirst: false
);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddImportsInOrder(bool useSymbolAnnotations)
        {
            await TestAsync(
@"using System.Collections;
using System.Diagnostics;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddMultipleImportsInOrder(bool useSymbolAnnotations)
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
    public System.EventHandler Handler;
}",

@"using System;
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
    public System.EventHandler Handler;
}",

@"using System;
using System.Collections.Generic;

class C
{
    public List<int> F;
    public EventHandler Handler;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotRedundantlyAdded(bool useSymbolAnnotations)
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Fact]
        public async Task TestBuiltInTypeFromSyntaxes()
        {
            await TestAsync(
@"class C
{
    public System.Int32 F;
}",

@"using System;

class C
{
    public System.Int32 F;
}",

@"class C
{
    public int F;
}", useSymbolAnnotations: false);
        }

        [Fact]
        public async Task TestBuiltInTypeFromSymbols()
        {
            await TestAsync(
@"class C
{
    public System.Int32 F;
}",

@"class C
{
    public System.Int32 F;
}",

@"class C
{
    public int F;
}", useSymbolAnnotations: true);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForNamespaceDeclarations(bool useSymbolAnnotations)
        {
            await TestNoImportsAddedAsync(
@"namespace N
{
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesInsideNamespaceDeclarations(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        private N.C c;
    }
}",

@"namespace N
{
    class C
    {
        private N.C c;
    }
}",

@"namespace N
{
    class C
    {
        private C c;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesInsideParentOfNamespaceDeclarations(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    class C
    {
    }
}

namespace N.N1
{
    class C1
    {
        private N.C c;
    }
}",

@"namespace N
{
    class C
    {
    }
}

namespace N.N1
{
    class C1
    {
        private N.C c;
    }
}",

@"namespace N
{
    class C
    {
    }
}

namespace N.N1
{
    class C1
    {
        private C c;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesMatchingNestedImports(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private List<int> F;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportRemovedIfItMakesReferenceAmbiguous(bool useSymbolAnnotations)
        {
            // this is not really an artifact of the AddImports feature, it is due
            // to Simplifier not reducing the namespace reference because it would 
            // become ambiguous, thus leaving an unused using directive
            await TestAsync(
@"namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}",

@"using N;

namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}",

@"namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithoutExistingImports(bool useSymbolAnnotations)
        {
            await TestAsync(
@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithExistingImports(bool useSymbolAnnotations)
        {
            await TestAsync(
@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using ZZZ;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using ZZZ;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using ZZZ;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestLeadingWhitespaceLinesArePreserved(bool useSymbolAnnotations)
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportAddedToNestedImports(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    using System;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System;
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System;
    using System.Collections.Generic;

    class C
    {
        private List<int> F;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNameNotSimplfied(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace System
{
    using System.Threading;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace System
{
    using System.Collections.Generic;
    using System.Threading;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace System
{
    using System.Collections.Generic;
    using System.Threading;

    class C
    {
        private List<int> F;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestUnnecessaryImportAddedAndRemoved(bool useSymbolAnnotations)
        {
            await TestAsync(
@"using List = System.Collections.Generic.List<int>;

namespace System
{
    class C
    {
        private List F;
    }
}",

@"using System.Collections.Generic;
using List = System.Collections.Generic.List<int>;

namespace System
{
    class C
    {
        private List F;
    }
}",

@"using List = System.Collections.Generic.List<int>;

namespace System
{
    class C
    {
        private List F;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportAddedToStartOfDocumentIfNoNestedImports(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"using System.Collections.Generic;

namespace N
{
    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"using System.Collections.Generic;

namespace N
{
    class C
    {
        private List<int> F;
    }
}", useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(9228, "https://github.com/dotnet/roslyn/issues/9228")]
        public async Task TestDoNotAddDuplicateImportIfNamespaceIsDefinedInSourceAndExternalAssembly(bool useSymbolAnnotations)
        {
            var externalCode =
@"namespace N.M { public class A : System.Attribute { } }";

            var code =
@"using System;
using N.M;

class C
{
    public void M1(String p1) { }

    public void M2([A] String p2) { }
}";

            var otherAssemblyReference = GetInMemoryAssemblyReferenceForCode(externalCode);

            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestMetadata.Net451.mscorlib }));

            var project = emptyProject
                .AddMetadataReferences(new[] { otherAssemblyReference })
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            project = project.AddDocument("duplicate.cs", externalCode).Project;
            var document = project.AddDocument("test.cs", code);

            var options = document.Project.Solution.Workspace.Options;

            var compilation = await document.Project.GetCompilationAsync(CancellationToken.None);
            var compilerDiagnostics = compilation.GetDiagnostics(CancellationToken.None);
            Assert.Empty(compilerDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var attribute = compilation.GetTypeByMetadataName("N.M.A");

            var syntaxRoot = await document.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
            SyntaxNode p1SyntaxNode = syntaxRoot.DescendantNodes().OfType<ParameterSyntax>().FirstOrDefault();

            // Add N.M.A attribute to p1.
            var editor = await DocumentEditor.CreateAsync(document, CancellationToken.None).ConfigureAwait(false);
            var attributeSyntax = editor.Generator.Attribute(editor.Generator.TypeExpression(attribute));

            editor.AddAttribute(p1SyntaxNode, attributeSyntax);
            var documentWithAttribute = editor.GetChangedDocument();

            var addImportOptions = new AddImportPlacementOptions();

            // Add namespace import.
            var imported = useSymbolAnnotations
                ? await ImportAdder.AddImportsFromSymbolAnnotationAsync(documentWithAttribute, addImportOptions, CancellationToken.None).ConfigureAwait(false)
                : await ImportAdder.AddImportsFromSyntaxesAsync(documentWithAttribute, addImportOptions, CancellationToken.None).ConfigureAwait(false);

            var formatted = await Formatter.FormatAsync(imported, options);
            var actualText = (await formatted.GetTextAsync()).ToString();

            Assert.Equal(@"using System;
using N.M;

class C
{
    public void M1([global::N.M.A] String p1) { }

    public void M2([A] String p2) { }
}", actualText);
        }

        private static MetadataReference GetInMemoryAssemblyReferenceForCode(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);

            var compilation = CSharpCompilation
                .Create("test.dll", new[] { tree })
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(TestMetadata.Net451.mscorlib);

            return compilation.ToMetadataReference();
        }

        #region AddImports Safe Tests

        [Fact]
        public async Task TestSafeWithMatchingSimpleName()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    class C1 {}
    class C2 {}
}

namespace B
{
    class C1 {}
}

class C
{
    C1 M(A.C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingGenericName()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    class C1<T> {}
    class C2 {}
}

namespace B
{
    class C1<T> {}
}

class C
{
    C1<int> M(A.C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingQualifiedName()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    class O {}
    class C2 {}
}

namespace B
{
	class O
	{
    	public class C1 {}
	}
}

class C
{
    O.C1 M(A.C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingAliasedIdentifierName()
        {
            await TestNoImportsAddedAsync(
@"using C1 = B.C1;

namespace A
{
    class C1 {}
    class C2 {}
}

namespace B
{
    class C1 {}
}

namespace Inner
{
    class C
    {
        C1 M(A.C2 c2) => default;
    }
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingGenericNameAndTypeArguments()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    class C1<T> {}
    class C2 {}
    class C3 {}
}

namespace B
{
    class C1<T> {}
    class C3 {}
}

class C
{
    C1<C3> M(A.C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingGenericNameAndTypeArguments_DifferentArity()
        {
            await TestAsync(
@"using B;

namespace A
{
    class C1<T, X> {}
    class C2 {}
}

namespace B
{
    class C1<T> {}
    class C3 {}
}

class C
{
    C1<C3> M(A.C2 c2) => default;
}",
@"using A;
using B;

namespace A
{
    class C1<T, X> {}
    class C2 {}
}

namespace B
{
    class C1<T> {}
    class C3 {}
}

class C
{
    C1<C3> M(A.C2 c2) => default;
}",
@"using A;
using B;

namespace A
{
    class C1<T, X> {}
    class C2 {}
}

namespace B
{
    class C1<T> {}
    class C3 {}
}

class C
{
    C1<C3> M(C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingQualifiedNameAndTypeArguments()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    class O {}
    class C2 {}
    class C3 {}
}

namespace B
{
    class C3 {}
	class O
	{
    	public class C1<T> {}
	}
}

class C
{
    O.C1<C3> M(A.C2 c2) => default;
}", useSymbolAnnotations: true);
        }

        [Fact, WorkItem(39641, "https://github.com/dotnet/roslyn/issues/39641")]
        public async Task TestSafeWithMatchingSimpleNameInAllLocations()
        {
            await TestNoImportsAddedAsync(
@"using B;
using System.Collections.Generic;

namespace A
{
	class C1 { }
	class C2 { }
}

namespace B
{
	class C1
	{
		public static C1 P { get; }
	}
}

#nullable enable
#pragma warning disable

class C
{
	/// <summary>
	/// <see cref=""C1""/>
	/// </summary>
	C1 M(C1 c1, A.C2 c2)

    {
        C1 result = (C1)c1 ?? new C1() ?? C1.P ?? new C1[0] { }[0] ?? new List<C1>()[0] ?? (C1?)null;
        (C1 a, int b) = (default, default);
        return result;
    }
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingExtensionMethod()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    static class AExtensions
    {
        public static void M(this int a){}
    }
    public class C1 {}
}

namespace B
{
    static class BExtensions
    {
        public static void M(this int a){}
    }
}

class C
{
    void M(A.C1 c1) => 42.M();
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingExtensionMethodAndArguments()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    static class AExtensions
    {
        public static void M(this int a, C2 c2){}
    }
    public class C1 {}
    public class C2 {}
}

namespace B
{
    static class BExtensions
    {
        public static void M(this int a, C2 c2){}
    }
    public class C2 {}
}

class C
{
    void M(A.C1 c1) => 42.M(default(C2));
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithMatchingExtensionMethodAndTypeArguments()
        {
            await TestNoImportsAddedAsync(
@"using B;

namespace A
{
    static class AExtensions
    {
        public static void M<T>(this int a){}
    }
    public class C1 {}
    public class C2 {}
}

namespace B
{
    static class BExtensions
    {
        public static void M<T>(this int a){}
    }
    public class C2 {}
}

class C
{
    void M(A.C1 c1) => 42.M<C2>();
}", useSymbolAnnotations: true);
        }

        [Fact]
        public async Task TestSafeWithLambdaExtensionMethodAmbiguity()
        {
            await TestNoImportsAddedAsync(
@"using System;

class C
{
    // Don't add a using for N even though it is used here.
    public N.Other x;

    public static void Main()
    {
        M(x => x.M1());
    }

    public static void M(Action<C> a){}
    public static void M(Action<int> a){}

    public void M1(){}
}

namespace N
{
    public class Other { }

    public static class Extensions
    {
        public static void M1(this int a){}
    }
}", useSymbolAnnotations: true);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestAddImport_InsideNamespace(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        public System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        public System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        public List<int> F;
    }
}", useSymbolAnnotations, placeImportsInsideNamespaces: true);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestAddImport_InsideNamespace_NoNamespace(bool useSymbolAnnotations)
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}", useSymbolAnnotations, placeImportsInsideNamespaces: true);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestAddImport_InsideNamespace_MultipleNamespaces(bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N1
{
    namespace N2
    {
        class C
        {
            public System.Collections.Generic.List<int> F;
        }
    }
}",

@"namespace N1
{
    namespace N2
    {
        using System.Collections.Generic;

        class C
        {
            public System.Collections.Generic.List<int> F;
        }
    }
}",

@"namespace N1
{
    namespace N2
    {
        using System.Collections.Generic;

        class C
        {
            public List<int> F;
        }
    }
}", useSymbolAnnotations, placeImportsInsideNamespaces: true);
        }

        #endregion
    }
}
