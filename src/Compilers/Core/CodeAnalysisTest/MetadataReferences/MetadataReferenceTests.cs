// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataReferenceTests : TestBase
    {
        [Fact]
        public void CreateFrom_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromImage(null));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromImage(default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromFile(null));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromFile(null, default(MetadataReferenceProperties)));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromStream(null));

            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromAssemblyInternal(null));
            Assert.Throws<ArgumentException>(() => MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly, new MetadataReferenceProperties(MetadataImageKind.Module)));

            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName { Name = "Foo" }, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => MetadataReference.CreateFromAssemblyInternal(dynamicAssembly));
        }

        [Fact]
        public void CreateFromImage()
        {
            var r = MetadataReference.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib);

            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
        }

        [Fact]
        public void CreateFromStream_FileStream()
        {
            var file = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.mscorlib);
            var stream = File.OpenRead(file.Path);

            var r = MetadataReference.CreateFromStream(stream);

            // stream is closed:
            Assert.False(stream.CanRead);

            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);

            // check that the metadata is in memory and the file can be deleted:
            File.Delete(file.Path);
            var metadata = (AssemblyMetadata)r.GetMetadata();
            Assert.Equal("CommonLanguageRuntimeLibrary", metadata.GetModules()[0].Name);
        }

        [Fact]
        public void CreateFromStream_MemoryStream()
        {
            var r = MetadataReference.CreateFromStream(new MemoryStream(TestResources.General.C1, writable: false));
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);

            Assert.Equal("C, Version=1.0.0.0, Culture=neutral, PublicKeyToken=374d0c2befcd8cc9",
                ((AssemblyMetadata)r.GetMetadata()).GetAssembly().Identity.GetDisplayName());
        }

        [Fact]
        public void CreateFromFile_Assembly()
        {
            var file = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.mscorlib);

            var r = MetadataReference.CreateFromFile(file.Path);
            Assert.Equal(file.Path, r.FilePath);
            Assert.Equal(file.Path, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);

            var props = new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a", "b"), embedInteropTypes: true, hasRecursiveAliases: true);
            Assert.Equal(props, MetadataReference.CreateFromFile(file.Path, props).Properties);

            // check that the metadata is in memory and the file can be deleted:
            File.Delete(file.Path);
            var metadata = (AssemblyMetadata)r.GetMetadata();
            Assert.Equal("CommonLanguageRuntimeLibrary", metadata.GetModules()[0].Name);
        }

        [Fact]
        public void CreateFromFile_Module()
        {
            var file = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.NetModule01.ModuleCS00);

            var r = MetadataReference.CreateFromFile(file.Path, MetadataReferenceProperties.Module);
            Assert.Equal(file.Path, r.FilePath);
            Assert.Equal(file.Path, r.Display);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);

            var props = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.Equal(props, MetadataReference.CreateFromFile(file.Path, props).Properties);

            // check that the metadata is in memory and the file can be deleted:
            File.Delete(file.Path);
            var metadata = (ModuleMetadata)r.GetMetadata();
            Assert.Equal("ModuleCS00.netmodule", metadata.Name);
        }

        [Fact]
        public void CreateFromAssembly()
        {
            var assembly = typeof(object).Assembly;
            var r = (PortableExecutableReference)MetadataReference.CreateFromAssemblyInternal(assembly);
            Assert.Equal(assembly.Location, r.FilePath);
            Assert.Equal(assembly.Location, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
            Assert.Same(DocumentationProvider.Default, r.DocumentationProvider);

            var props = new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a", "b"), embedInteropTypes: true, hasRecursiveAliases: true);
            Assert.Equal(props, MetadataReference.CreateFromAssemblyInternal(assembly, props).Properties);
        }

        [Fact]
        public void CreateFromAssembly_NoMetadata()
        {
            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName { Name = "A" }, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => MetadataReference.CreateFromAssemblyInternal(dynamicAssembly));

            var inMemoryAssembly = Assembly.Load(TestResources.General.C1);
            Assert.Equal("", inMemoryAssembly.Location);
            Assert.Throws<NotSupportedException>(() => MetadataReference.CreateFromAssemblyInternal(inMemoryAssembly));
        }

        [Fact]
        public void CreateFromAssembly_WithPropertiesAndDocumentation()
        {
            var doc = new TestDocumentationProvider();
            var assembly = typeof(object).Assembly;
            var r = (PortableExecutableReference)MetadataReference.CreateFromAssemblyInternal(assembly, new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a", "b"), embedInteropTypes: true), documentation: doc);
            Assert.Equal(assembly.Location, r.FilePath);
            Assert.Equal(assembly.Location, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.True(r.Properties.EmbedInteropTypes);
            AssertEx.Equal(ImmutableArray.Create("a", "b"), r.Properties.Aliases);
            Assert.Same(doc, r.DocumentationProvider);
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
        public void Module_WithXxx()
        {
            var doc = new TestDocumentationProvider();
            var module = ModuleMetadata.CreateFromImage(TestResources.General.C1);
            var r = module.GetReference(filePath: @"c:\temp", display: "hello", documentation: doc);
            Assert.Same(doc, r.DocumentationProvider);
            Assert.Same(doc, r.DocumentationProvider);
            Assert.NotNull(r.GetMetadata());
            Assert.Equal(false, r.Properties.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.True(r.Properties.Aliases.IsEmpty);
            Assert.Equal(@"c:\temp", r.FilePath);

            var r1 = r.WithAliases(default(ImmutableArray<string>));
            Assert.Same(r, r1);
            Assert.Equal(@"c:\temp", r1.FilePath);

            var r2 = r.WithEmbedInteropTypes(false);
            Assert.Same(r, r2);
            Assert.Equal(@"c:\temp", r2.FilePath);

            var r3 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Module));
            Assert.Same(r, r3);

            var r4 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Assembly));
            Assert.Equal(MetadataImageKind.Assembly, r4.Properties.Kind);

            Assert.Throws<ArgumentException>(() => r.WithAliases(new[] { "bar" }));
            Assert.Throws<ArgumentException>(() => r.WithEmbedInteropTypes(true));
        }

        [Fact]
        public void Assembly_WithXxx()
        {
            var doc = new TestDocumentationProvider();
            var assembly = AssemblyMetadata.CreateFromImage(TestResources.General.C1);

            var r = assembly.GetReference(
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
            Assert.Equal(@"c:\temp", r2.FilePath);

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

            var r5 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Module));
            Assert.Equal(MetadataImageKind.Module, r5.Properties.Kind);
            Assert.True(r5.Properties.Aliases.IsEmpty);
            Assert.False(r5.Properties.EmbedInteropTypes);

            var r6 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("x"), embedInteropTypes: true));
            Assert.Equal(MetadataImageKind.Assembly, r6.Properties.Kind);
            AssertEx.Equal(new[] { "x" }, r6.Properties.Aliases);
            Assert.True(r6.Properties.EmbedInteropTypes);
        }

        [Fact]
        public void CompilationReference_CSharp_WithXxx()
        {
            var c = CSharpCompilation.Create("cs");

            var r = c.ToMetadataReference();
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);

            var r1 = r.WithAliases(new[] { "a", "b" });
            Assert.Same(c, r1.Compilation);
            Assert.False(r1.Properties.EmbedInteropTypes);
            AssertEx.Equal(new[] { "a", "b" }, r1.Properties.Aliases);
            Assert.Equal(MetadataImageKind.Assembly, r1.Properties.Kind);

            var r2 = r.WithEmbedInteropTypes(true);
            Assert.Same(c, r2.Compilation);
            Assert.True(r2.Properties.EmbedInteropTypes);
            Assert.True(r2.Properties.Aliases.IsEmpty);
            Assert.Equal(MetadataImageKind.Assembly, r2.Properties.Kind);

            var r3 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("x"), embedInteropTypes: true));
            Assert.Same(c, r3.Compilation);
            Assert.True(r3.Properties.EmbedInteropTypes);
            AssertEx.Equal(new[] { "x" }, r3.Properties.Aliases);
            Assert.Equal(MetadataImageKind.Assembly, r3.Properties.Kind);

            Assert.Throws<ArgumentException>(() => r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Module)));
        }

        [Fact]
        public void CompilationReference_VB_WithXxx()
        {
            var c = VisualBasicCompilation.Create("vb");

            var r = c.ToMetadataReference();
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);

            var r1 = r.WithAliases(new[] { "a", "b" });
            Assert.Same(c, r1.Compilation);
            Assert.False(r1.Properties.EmbedInteropTypes);
            AssertEx.Equal(new[] { "a", "b" }, r1.Properties.Aliases);
            Assert.Equal(MetadataImageKind.Assembly, r1.Properties.Kind);

            var r2 = r.WithEmbedInteropTypes(true);
            Assert.Same(c, r2.Compilation);
            Assert.True(r2.Properties.EmbedInteropTypes);
            Assert.True(r2.Properties.Aliases.IsEmpty);
            Assert.Equal(MetadataImageKind.Assembly, r2.Properties.Kind);

            var r3 = r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("x"), embedInteropTypes: true));
            Assert.Same(c, r3.Compilation);
            Assert.True(r3.Properties.EmbedInteropTypes);
            AssertEx.Equal(new[] { "x" }, r3.Properties.Aliases);
            Assert.Equal(MetadataImageKind.Assembly, r3.Properties.Kind);

            Assert.Throws<ArgumentException>(() => r.WithProperties(new MetadataReferenceProperties(MetadataImageKind.Module)));
        }

        [Fact]
        public void Module_Path()
        {
            var module = ModuleMetadata.CreateFromImage(TestResources.General.C1);

            // no path specified
            var mmr1 = module.GetReference();
            Assert.Null(mmr1.FilePath);

            // path specified
            const string path = @"c:\some path that doesn't need to exist";
            var r = module.GetReference(filePath: path);
            Assert.Equal(path, r.FilePath);
        }

        [Fact]
        public void Assembly_Path()
        {
            var assembly = AssemblyMetadata.CreateFromImage(TestResources.General.C1);

            // no path specified
            var mmr1 = assembly.GetReference();
            Assert.Null(mmr1.FilePath);

            // path specified
            const string path = @"c:\some path that doesn't need to exist";
            var r = assembly.GetReference(filePath: path);
            Assert.Equal(path, r.FilePath);
        }

        [Fact]
        public void Display()
        {
            MetadataReference r;

            r = MetadataReference.CreateFromImage(TestResources.General.C1);
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);

            r = ModuleMetadata.CreateFromImage(TestResources.General.C1).GetReference();
            Assert.Equal(CodeAnalysisResources.InMemoryModule, r.Display);

            r = MetadataReference.CreateFromImage(TestResources.General.C1, filePath: @"c:\blah");
            Assert.Equal(@"c:\blah", r.Display);

            r = AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(display: @"dddd");
            Assert.Equal(@"dddd", r.Display);

            r = AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(filePath: @"c:\blah", display: @"dddd");
            Assert.Equal(@"dddd", r.Display);

            r = CS.CSharpCompilation.Create("compilation name").ToMetadataReference();
            Assert.Equal(@"compilation name", r.Display);

            r = VisualBasic.VisualBasicCompilation.Create("compilation name").ToMetadataReference();
            Assert.Equal(@"compilation name", r.Display);
        }

        private static readonly AssemblyIdentity s_mscorlibIdentity = new AssemblyIdentity(
                name: "mscorlib",
                version: new Version(4, 0, 0, 0),
                cultureName: "",
                publicKeyOrToken: new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }.AsImmutableOrNull(),
                hasPublicKey: true);

        private class MyReference : PortableExecutableReference
        {
            private readonly string _display;

            public MyReference(string fullPath, string display)
                : base(default(MetadataReferenceProperties), fullPath)
            {
                _display = display;
            }

            public override string Display
            {
                get { return _display; }
            }

            protected override Metadata GetMetadataImpl()
            {
                throw new NotImplementedException();
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                throw new NotImplementedException();
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
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

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Equivalence()
        {
            var comparer = CommonReferenceManager<CS.CSharpCompilation, IAssemblySymbolInternal>.MetadataReferenceEqualityComparer.Instance;

            var f1 = MscorlibRef;
            var f2 = SystemCoreRef;

            var i1 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).GetReference(display: "i1");
            var i2 = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).GetReference(display: "i2");

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
            var comparer = CommonReferenceManager<CS.CSharpCompilation, IAssemblySymbolInternal>.MetadataReferenceEqualityComparer.Instance;

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
            var corlib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).
                GetReference(display: "corlib", documentation: docProvider);

            var comp = CS.CSharpCompilation.Create("foo",
                syntaxTrees: new[] { CS.SyntaxFactory.ParseSyntaxTree("class C : System.Collections.ArrayList { }") },
                references: new[] { corlib });

            var c = (ITypeSymbol)comp.GlobalNamespace.GetMembers("C").Single();
            var list = c.BaseType;
            var summary = list.GetDocumentationCommentXml();
            Assert.Equal("<member name='T:System.Collections.ArrayList'><summary>T:System.Collections.ArrayList</summary></member>", summary);
        }

        [Fact]
        public void InvalidPublicKey()
        {
            var r = MetadataReference.CreateFromStream(new MemoryStream(TestResources.SymbolsTests.Metadata.InvalidPublicKey, writable: false));
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);

            Assert.Throws<BadImageFormatException>((Func<object>)((AssemblyMetadata)r.GetMetadata()).GetAssembly);
        }
    }
}
