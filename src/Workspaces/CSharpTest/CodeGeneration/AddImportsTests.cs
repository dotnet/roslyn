// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editing
{
    [UseExportProvider]
    public class AddImportsTests
    {
        private async Task<Document> GetDocument(string code, bool withAnnotations)
        {
            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));

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

        private async Task TestAsync(string initialText, string importsAddedText, string simplifiedText, bool safe, bool useSymbolAnnotations, Func<OptionSet, OptionSet> optionsTransform = null)
        {
            var doc = await GetDocument(initialText, useSymbolAnnotations);
            OptionSet options = await doc.GetOptionsAsync();
            if (optionsTransform != null)
            {
                options = optionsTransform(options);
            }

            var imported = useSymbolAnnotations
                ? await ImportAdder.AddImportsFromSymbolAnnotationAsync(doc, safe, options)
                : await ImportAdder.AddImportsFromSyntaxesAsync(doc, safe, options);

            if (importsAddedText != null)
            {
                var formatted = await Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options);
                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(importsAddedText, actualText);
            }

            if (simplifiedText != null)
            {
                var reduced = await Simplifier.ReduceAsync(imported, options);
                var formatted = await Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options);

                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(simplifiedText, actualText);
            }
        }

        public static object[][] TestAllData =
        {
            new object[] { false, false },
            new object[] { false, true },
            new object[] { true, false },
            new object[] { true, true },
        };

        public static object[][] TestSyntaxesData =
        {
            new object[] { false, false },
            new object[] { true, false },
        };

        public static object[][] TestSymbolsData =
        {
            new object[] { false, true },
            new object[] { true, true },
        };

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddImport(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddSystemImportFirst(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestDontAddSystemImportFirst(bool safe, bool useSymbolAnnotations)
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
                safe,
                useSymbolAnnotations,
                options => options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, false)
);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddImportsInOrder(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestAddMultipleImportsInOrder(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotRedundantlyAdded(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestSyntaxesData))]
        public async Task TestBuiltInTypeFromSyntaxes(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestSymbolsData))]
        public async Task TestBuiltInTypeFromSymbols(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForNamespaceDeclarations(bool safe, bool useSymbolAnnotations)
        {
            await TestAsync(
@"namespace N
{
}",

@"namespace N
{
}",

@"namespace N
{
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesInsideNamespaceDeclarations(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesInsideParentOfNamespaceDeclarations(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNotAddedForReferencesMatchingNestedImports(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportRemovedIfItMakesReferenceAmbiguous(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithoutExistingImports(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithExistingImports(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestLeadingWhitespaceLinesArePreserved(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportAddedToNestedImports(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportNameNotSimplfied(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, InlineData(false, true)]
        public async Task TestUnnecessaryImportAddedAndRemoved(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        public async Task TestImportAddedToStartOfDocumentIfNoNestedImports(bool safe, bool useSymbolAnnotations)
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
}", safe, useSymbolAnnotations);
        }

        [Theory, MemberData(nameof(TestAllData))]
        [WorkItem(9228, "https://github.com/dotnet/roslyn/issues/9228")]
        public async Task TestDoNotAddDuplicateImportIfNamespaceIsDefinedInSourceAndExternalAssembly(bool safe, bool useSymbolAnnotations)
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
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));

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

            // Add namespace import.
            var imported =
                useSymbolAnnotations
                    ? await ImportAdder.AddImportsFromSymbolAnnotationAsync(documentWithAttribute, safe, null,
                        CancellationToken.None).ConfigureAwait(false)
                    : await ImportAdder.AddImportsFromSyntaxesAsync(documentWithAttribute, safe, null,
                        CancellationToken.None).ConfigureAwait(false);

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
                .AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib);

            return compilation.ToMetadataReference();
        }

        #region AddImports Safe Tests

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingSimpleName(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    global::B.C1 M(A.C2 c2) => default;
}",

@"using A;
using B;

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
    B.C1 M(C2 c2) => default;
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingGenericName(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    global::B.C1<int> M(A.C2 c2) => default;
}",

@"using A;
using B;

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
    B.C1<int> M(C2 c2) => default;
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingQualifiedName(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    global::B.O.C1 M(A.C2 c2) => default;
}",

@"using A;
using B;

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
    B.O.C1 M(C2 c2) => default;
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingAliasedIdentifierName(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;
using C1 = B.C1;

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
        global::B.C1 M(A.C2 c2) => default;
    }
}",

@"using A;
using C1 = B.C1;

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
        C1 M(C2 c2) => default;
    }
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingGenericNameAndTypeArguments(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    global::B.C1<global::B.C3> M(A.C2 c2) => default;
}",

@"using A;
using B;

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
    B.C1<B.C3> M(C2 c2) => default;
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingQualifiedNameAndTypeArguments(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    global::B.O.C1<global::B.C3> M(A.C2 c2) => default;
}",

@"using A;
using B;

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
    B.O.C1<B.C3> M(C2 c2) => default;
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingSimpleNameInAllLocations(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;
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
		public static global::B.C1 P { get; }
	}
}

#nullable enable
#pragma warning disable

class C
{
    /// <summary>
    /// <see cref=""global::B.C1""/>
    /// </summary>
    global::B.C1 M(global::B.C1 c1, A.C2 c2)

    {
        global::B.C1 result = (global::B.C1)c1 ?? new global::B.C1() ?? global::B.C1.P ?? new global::B.C1[0] { }[0] ?? new List<global::B.C1>()[0] ?? (global::B.C1?)null;
        (global::B.C1 a, int b) = (default, default);
        return result;
    }
}",

@"using A;
using B;
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
    /// <see cref=""global::B.C1""/>
    /// </summary>
    B.C1 M(B.C1 c1, C2 c2)

    {
        B.C1 result = (B.C1)c1 ?? new B.C1() ?? B.C1.P ?? new B.C1[0] { }[0] ?? new List<B.C1>()[0] ?? (B.C1?)null;
        (B.C1 a, int b) = (default, default);
        return result;
    }
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingExtensionMethod(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    void M(A.C1 c1) => global::B.BExtensions.M(42);
}",

@"using A;
using B;

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
    void M(C1 c1) => BExtensions.M(42);
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingExtensionMethodAndArguments(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

namespace A
{
    static class AExtensions
    {
        public static void M(this int a, global::A.C2 c2){}
    }
    public class C1 {}
    public class C2 {}
}

namespace B
{
    static class BExtensions
    {
        public static void M(this int a, global::B.C2 c2){}
    }
    public class C2 {}
}

class C
{
    void M(A.C1 c1) => global::B.BExtensions.M(42, (global::B.C2)(default(global::B.C2)));
}",

@"using A;
using B;

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
    void M(C1 c1) => 42.M(default(B.C2));
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestSafeWithMatchingExtensionMethodAndTypeArguments(bool useSymbolAnnotations)
        {
            await TestAsync(
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
}",

@"using A;
using B;

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
    void M(A.C1 c1) => global::B.BExtensions.M<global::B.C2>(42);
}",

@"using A;
using B;

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
    void M(C1 c1) => BExtensions.M<B.C2>(42);
}", safe: true, useSymbolAnnotations);
        }

        [Theory, InlineData(true), InlineData(false)]
        public async Task TestWarnsWithMatchingExtensionMethodUsedAsDelegate(bool useSymbolAnnotations)
        {
            var source = @"using System;
using B;

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
        public static void M(this object a){}
    }
}

class C
{
    Action M(A.C1 c1) => 42.M;
}";

            await TestAsync(
                source,
@"using System;
using A;
using B;

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
        public static void M(this object a){}
    }
}

class C
{
    Action M(A.C1 c1) => 42.M;
}",

@"using System;
using A;
using B;

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
        public static void M(this object a){}
    }
}

class C
{
    Action M(C1 c1) => 42.M;
}", safe: true, useSymbolAnnotations);

            var doc = await GetDocument(source, useSymbolAnnotations);
            OptionSet options = await doc.GetOptionsAsync();

            var imported = await ImportAdder.AddImportsFromSyntaxesAsync(doc, true, options);
            var root = await imported.GetSyntaxRootAsync();
            var nodeWithWarning = root.GetAnnotatedNodes(WarningAnnotation.Kind).Single();

            Assert.Equal("42.M", nodeWithWarning.ToFullString());

            var warning = nodeWithWarning.GetAnnotations(WarningAnnotation.Kind).Single();
            var expectedWarningMessage = WorkspacesResources.Warning_adding_imports_will_bring_an_extension_method_into_scope_with_the_same_name_as_member_access.Replace("{0}", "M");

            Assert.Equal(expectedWarningMessage, WarningAnnotation.GetDescription(warning));
        }
        #endregion
    }
}
