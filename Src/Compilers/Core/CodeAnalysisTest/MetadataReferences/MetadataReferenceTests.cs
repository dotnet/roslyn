// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataReferenceTests : TestBase
    {
        private char SystemDrive = Environment.GetFolderPath(Environment.SpecialFolder.Windows)[0];

        [Fact]
        public void MetadataFileReference_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => { new MetadataFileReference(null); });
            Assert.Throws<ArgumentNullException>(() => { new MetadataFileReference(null, default(MetadataReferenceProperties)); });
        }

        [Fact]
        public void MetadataImageReference_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => { new MetadataImageReference(default(ImmutableArray<byte>)); });
            Assert.Throws<ArgumentNullException>(() => { new MetadataImageReference((ModuleMetadata)null); });
            Assert.Throws<ArgumentNullException>(() => { new MetadataImageReference((AssemblyMetadata)null); });
            Assert.Throws<ArgumentNullException>(() => { new MetadataImageReference((System.IO.Stream)null); });
            Assert.Throws<ArgumentNullException>(() => { new MetadataImageReference((IEnumerable<byte>)null); });
        }

        private class TestDocumentationProvider : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            {
                return string.Format("<member name='{0}'><summary>{0}</summary></member>", documentationMemberID);
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void MetadataImageReference_Module_WithXxx()
        {
            var doc = new TestDocumentationProvider();
            var module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1);
            var r = new MetadataImageReference(module, filePath: @"c:\temp", display: "hello", documentation: doc);
            Assert.Same(doc, r.DocumentationProvider);
            Assert.Same(doc, r.DocumentationProvider);
            Assert.NotNull(r.GetMetadata());
            Assert.Equal(false, r.Properties.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.True(r.Properties.Aliases.IsDefault);
            Assert.Equal(@"c:\temp", r.FilePath);

            var r1 = r.WithAliases(default(ImmutableArray<string>));
            Assert.Same(r, r1);

            var r2 = r.WithEmbedInteropTypes(false);
            Assert.Same(r, r2);

            var r3 = r.WithDocumentationProvider(doc);
            Assert.Same(r, r3);
            
            Assert.Throws<ArgumentException>(() => r.WithAliases(new[] { "bar" }));
            Assert.Throws<ArgumentException>(() => r.WithEmbedInteropTypes(true));
        }

        [Fact]
        public void MetadataImageReference_Assembly_WithXxx()
        {
            var doc = new TestDocumentationProvider();
            var assembly = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1);
         
            var r = new MetadataImageReference(
                assembly,
                documentation: doc, 
                aliases: ImmutableArray.Create("a"), 
                embedInteropTypes: true, 
                filePath: @"c:\temp", 
                display: "hello");

            Assert.Same(doc, r.DocumentationProvider);
            Assert.Same(doc, r.DocumentationProvider);
            Assert.NotNull(r.GetMetadata());
            Assert.Equal(true, r.Properties.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            AssertEx.Equal(new[] { "a" }, r.Properties.Aliases);
            Assert.Equal(@"c:\temp", r.FilePath);

            var r2 = r.WithEmbedInteropTypes(true);
            Assert.Equal(r, r2);

            var r3 = r.WithAliases(ImmutableArray.Create("b", "c"));
            Assert.Same(r.DocumentationProvider, r3.DocumentationProvider);
            Assert.Same(r.GetMetadata(), r3.GetMetadata());
            Assert.Equal(r.Properties.EmbedInteropTypes, r3.Properties.EmbedInteropTypes);
            Assert.Equal(r.Properties.Kind, r3.Properties.Kind);
            AssertEx.Equal(new[] { "b", "c" }, r3.Properties.Aliases);
            Assert.Equal(r.FilePath, r3.FilePath);

            var r4 = r.WithEmbedInteropTypes(false);
            Assert.Same(r.DocumentationProvider, r4.DocumentationProvider);
            Assert.Same(r.GetMetadata(), r4.GetMetadata());
            Assert.Equal(false, r4.Properties.EmbedInteropTypes);
            Assert.Equal(r.Properties.Kind, r4.Properties.Kind);
            AssertEx.Equal(r.Properties.Aliases, r4.Properties.Aliases);
            Assert.Equal(r.FilePath, r4.FilePath);

            Assert.Throws<ArgumentNullException>(() => r.WithDocumentationProvider(null));
            Assert.Same(r, r.WithDocumentationProvider(r.DocumentationProvider));

            var doc2 = new TestDocumentationProvider();
            var r5 = r.WithDocumentationProvider(doc2);
            Assert.Same(doc2, r5.DocumentationProvider);

            Assert.Same(r.GetMetadata(), r5.GetMetadata());
            Assert.Equal(r.Properties.EmbedInteropTypes, r5.Properties.EmbedInteropTypes);
            Assert.Equal(r.Properties.Kind, r5.Properties.Kind);
            AssertEx.Equal(r.Properties.Aliases, r5.Properties.Aliases);
            Assert.Equal(r.FilePath, r5.FilePath);
        }

        [Fact]
        public void MetadataImageReference_Module_Path()
        {
            var module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1);

            // no path specified
            var mmr1 = new MetadataImageReference(module);
            Assert.Null(mmr1.FilePath);

            // path specified
            const string path = @"c:\some path that doesn't need to exist";
            var r = new MetadataImageReference(module, filePath: path);
            Assert.Equal(path, r.FilePath);
        }

        [Fact]
        public void MetadataImageReference_Assembly_Path()
        {
            var assembly = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1);

            // no path specified
            var mmr1 = new MetadataImageReference(assembly);
            Assert.Null(mmr1.FilePath);

            // path specified
            const string path = @"c:\some path that doesn't need to exist";
            var r = new MetadataImageReference(assembly, filePath: path);
            Assert.Equal(path, r.FilePath);
        }

        [Fact]
        public void MetadataReference_Display()
        {
            MetadataReference r;

            r = new MetadataImageReference(AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1));
            Assert.Equal("<in-memory assembly>".NeedsLocalization(), r.Display);

            r = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1));
            Assert.Equal("<in-memory module>".NeedsLocalization(), r.Display);

            r = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1), filePath: @"c:\blah");
            Assert.Equal(@"c:\blah", r.Display);

            r = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1), display: @"dddd");
            Assert.Equal(@"dddd", r.Display);

            r = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.C1), filePath: @"c:\blah", display: @"dddd");
            Assert.Equal(@"dddd", r.Display);

            r = new MetadataFileReference(@"c:\some path");
            Assert.Equal(@"c:\some path", r.Display);

            r = CS.CSharpCompilation.Create("compilation name").ToMetadataReference();
            Assert.Equal(@"compilation name", r.Display);

            r = VisualBasic.VisualBasicCompilation.Create("compilation name").ToMetadataReference();
            Assert.Equal(@"compilation name", r.Display);
        }

        private static readonly AssemblyIdentity MscorlibIdentity = new AssemblyIdentity(
                name: "mscorlib", 
                version: new Version(4, 0, 0, 0), 
                cultureName: "",
                publicKeyOrToken: new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }.AsImmutableOrNull(),
                hasPublicKey: true);

        private class MyReference : PortableExecutableReference
        {
            private readonly string display;

            public MyReference(string fullPath, string display)
                : base(default(MetadataReferenceProperties), fullPath)
            {
                this.display = display;
            }

            public override string Display
            {
                get { return display; }
            }

            protected override Metadata GetMetadataImpl()
            {
                throw new NotImplementedException();
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                throw new NotImplementedException();
            }
        }

        private class MyReference2 : PortableExecutableReference
        {            
            public MyReference2(string fullPath, string display)
                : base(default(MetadataReferenceProperties), fullPath)
            {
            }

            public override string Display
            {
                get { return base.Display; }
            }

            protected override Metadata GetMetadataImpl()
            {
                throw new NotImplementedException();
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Equivalence()
        {
            var comparer = CommonReferenceManager<CS.CSharpCompilation, IAssemblySymbol>.MetadataReferenceEqualityComparer.Instance;

            var f1 = MscorlibRef;
            var f2 = SystemCoreRef;

            var i1 = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib, display: "i1");
            var i2 = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib, display: "i2");

            var m1a = new MyReference(@"c:\a\foo.dll", display: "m1a");
            Assert.Equal("m1a", m1a.Display);
            var m1b = new MyReference(@"c:\b\..\a\foo.dll", display: "m1b");
            Assert.Equal("m1b", m1b.Display);
            var m2 = new MyReference(@"c:\b\foo.dll", display: "m2");
            Assert.Equal("m2", m2.Display);
            var m3 = new MyReference(null, display: "m3");
            var m4 = new MyReference(null, display: "m4");

            var c1a = CS.CSharpCompilation.Create("foo").ToMetadataReference();
            var c1b = c1a.Compilation.ToMetadataReference();
            var c2 = CS.CSharpCompilation.Create("foo").ToMetadataReference();
            
            var all = new MetadataReference[] { f1, f2, i1, i2, m1a, m1b, m2, c1a, c1b, c2 };
            foreach (var r in all)
            {
                foreach (var s in all)
                {
                    var eq = comparer.Equals(r, s);

                    if (ReferenceEquals(r, s) || 
                        ReferenceEquals(r, c1a) && ReferenceEquals(s, c1b) ||
                        ReferenceEquals(s, c1a) && ReferenceEquals(r, c1b))
                    {
                        Assert.True(eq, string.Format("expected '{0}' == '{1}'", r.Display, s.Display));
                    }
                    else
                    {
                        Assert.False(eq, string.Format("expected '{0}' != '{1}'", r.Display, s.Display));
                    }
                }
            }
        }

        [Fact]
        public void PortableReference_Display()
        {
            var comparer = CommonReferenceManager<CS.CSharpCompilation, IAssemblySymbol>.MetadataReferenceEqualityComparer.Instance;

            var f1 = MscorlibRef;
            var f2 = SystemCoreRef;
            
            var m1a = new MyReference2(@"c:\a\foo.dll", display: "m1a");
            Assert.Equal(@"c:\a\foo.dll", m1a.Display);
            Assert.Equal(@"c:\a\foo.dll", m1a.FilePath);            
        }

        [Fact]
        public void DocCommentProvider()
        {
            var docProvider = new TestDocumentationProvider();
            var corlib = new MetadataImageReference(
                ProprietaryTestResources.NetFX.v4_0_30319.mscorlib.AsImmutableOrNull(), 
                display: "corlib",
                documentation: docProvider);

            var comp = CS.CSharpCompilation.Create("foo", 
                syntaxTrees: new[] { CS.SyntaxFactory.ParseSyntaxTree("class C : System.Collections.ArrayList { }") },
                references: new[] { corlib });

            var c = (ITypeSymbol)comp.GlobalNamespace.GetMembers("C").Single();
            var list = c.BaseType;
            var summary = list.GetDocumentationCommentXml();
            Assert.Equal("<member name='T:System.Collections.ArrayList'><summary>T:System.Collections.ArrayList</summary></member>", summary);
        }

        [Fact]
        public void MetadataImageReferenceFromStream()
        {
            MetadataImageReference r;

            r = new MetadataImageReference(new MemoryStream(TestResources.SymbolsTests.General.C1, writable: false));
            Assert.Equal("<in-memory assembly>".NeedsLocalization(), r.Display);

            Assert.Equal("C, Version=1.0.0.0, Culture=neutral, PublicKeyToken=374d0c2befcd8cc9", ((AssemblyMetadata)r.GetMetadata()).GetAssembly().Identity.GetDisplayName());
        }

    }
}
