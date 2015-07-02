// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editing
{
    public class AddImportsTests
    {
        private readonly AdhocWorkspace _ws = new AdhocWorkspace();
        private readonly Project _emptyProject;

        public AddImportsTests()
        {
            _emptyProject = _ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));
        }

        private Document GetDocument(string code)
        {
            return _emptyProject.AddDocument("test.cs", code);
        }

        private void Test(string initialText, string importsAddedText, string simplifiedText, OptionSet options = null)
        {
            var doc = GetDocument(initialText);
            options = options ?? doc.Project.Solution.Workspace.Options;

            var imported = ImportAdder.AddImportsAsync(doc, options).Result;

            if (importsAddedText != null)
            {
                var formatted = Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options).Result;
                var actualText = formatted.GetTextAsync().Result.ToString();
                Assert.Equal(importsAddedText, actualText);
            }

            if (simplifiedText != null)
            {
                var reduced = Simplifier.ReduceAsync(imported, options).Result;
                var formatted = Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options).Result;

                var actualText = formatted.GetTextAsync().Result.ToString();
                Assert.Equal(simplifiedText, actualText);
            }
        }

        [Fact]
        public void TestAddImport()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestAddSystemImportFirst()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestDontAddSystemImportFirst()
        {
            Test(
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
    _ws.Options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, false)
);
        }

        [Fact]
        public void TestAddImportsInOrder()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestAddMultipleImportsInOrder()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestImportNotRedundantlyAdded()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestUnusedAddedImportIsRemovedBySimplifier()
        {
            Test(
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
}");
        }

        [Fact]
        public void TestImportNotAddedForNamespaceDeclarations()
        {
            Test(
@"namespace N
{
}",

@"namespace N
{
}",

@"namespace N
{
}");
        }

        [Fact]
        public void TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations()
        {
            Test(
@"namespace N
{
    class C
    {
        private N.C c;
    }
}",

@"using N;

namespace N
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
}");
        }

        [Fact]
        public void TestImportAddedAndRemovedForReferencesMatchingNestedImports()
        {
            Test(
@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"using System.Collections.Generic;

namespace N
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
}");
        }

        [Fact]
        public void TestImportRemovedIfItMakesReferenceAmbiguous()
        {
            // this is not really an artifact of the AddImports feature, it is due
            // to Simplifier not reducing the namespace reference because it would 
            // become ambiguous, thus leaving an unused using directive
            Test(
    @"
namespace N { class C { } }

class C 
{
   public N.C F;
}",

    @"using N;

namespace N { class C { } }

class C 
{
   public N.C F;
}",

    @"
namespace N { class C { } }

class C 
{
   public N.C F;
}");
        }
    }
}

