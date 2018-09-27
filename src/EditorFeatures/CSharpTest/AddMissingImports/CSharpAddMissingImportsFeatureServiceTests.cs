// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.AddMissingImports;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.AddMissingImports)]
    public class CSharpAddMissingImportsFeatureServiceTests : AbstractAddMissingImportsFeatureServiceTest
    {
        private static readonly Lazy<IExportProviderFactory> _exportProviderFactory = new Lazy<IExportProviderFactory>(() =>
        {
            // When running tests we need to get compiler diagnostics so we can find the missing imports
            var catalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                .WithoutPartsOfType(typeof(IWorkspaceDiagnosticAnalyzerProviderService))
                .WithPart(typeof(CSharpCompilerDiagnosticAnalyzerProviderService));

            return ExportProviderCache.GetOrCreateExportProviderFactory(catalog);
        });

        public CSharpAddMissingImportsFeatureServiceTests()
            : base(LanguageNames.CSharp)
        {
        }

        protected override ExportProvider CreateExportProvider()
        {
            return _exportProviderFactory.Value.CreateExportProvider();
        }

        [Fact]
        public async Task AddMissingImports_NoChange_SpanIsNotMissingImports()
        {
            var code = @"
class [|C|]
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await AssertDocumentUnchangedAsync(code);
        }

        [Fact]
        public async Task AddMissingImports_AddedImport_SpanContainsMissingImport()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}
";

            var expected = @"
using A;

class C
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await AssertDocumentChangedAsync(code, expected);
        }

        [Fact]
        public async Task AddMissingImports_AddedMultipleImports_SpanContainsMissingImports()
        {
            var code = @"
using System;

class C
{
    [|public D Foo { get; }
    public E Bar { get; }|]
}

namespace A
{
    public class D { }
}

namespace B
{
    public class E { }
}
";

            var expected = @"
using System;
using A;
using B;

class C
{
    public D Foo { get; }
    public E Bar { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class E { }
}
";

            await AssertDocumentChangedAsync(code, expected);
        }

        [Fact]
        public async Task AddMissingImports_NoChange_SpanContainsAmbiguousMissingImport()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
}
";

            await AssertDocumentUnchangedAsync(code);
        }

        [Fact]
        public async Task AddMissingImports_PartialFix_SpanContainsFixableAndAmbiguousMissingImports()
        {
            var code = @"
class C
{
    [|public D Foo { get; }
    public E Bar { get; }|]
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
    public class E { }
}
";

            var expected = @"
using B;

class C
{
    public D Foo { get; }
    public E Bar { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
    public class E { }
}
";

            await AssertDocumentChangedAsync(code, expected);
        }
    }
}
