// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Basic.Reference.Assemblies;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataReferenceTests : TestBase
    {
        // Tests require AppDomains
#if NET472
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
        public void CreateFrom_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromImage(null));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromImage(default));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromFile(null));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromFile(null, default(MetadataReferenceProperties)));
            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromStream(null));

            Assert.Throws<ArgumentNullException>(() => MetadataReference.CreateFromAssemblyInternal(null));
            Assert.Throws<ArgumentException>(() => MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly, new MetadataReferenceProperties(MetadataImageKind.Module)));

            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName { Name = "Goo" }, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => MetadataReference.CreateFromAssemblyInternal(dynamicAssembly));
        }
#endif

        [Theory, CombinatorialData]
        public void CreateFromImage_Assembly(bool module, bool immutableArray, bool explicitProperties)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var properties = explicitProperties ? MetadataReferenceProperties.Assembly : default;
            var r = immutableArray
                ? MetadataReference.CreateFromImage(peImage.AsImmutable(), properties)
                : MetadataReference.CreateFromImage(peImage.AsEnumerable(), properties);

            Assert.IsAssignableFrom<AssemblyMetadata>(r.GetMetadata());
            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
        }

        [Theory, CombinatorialData]
        public void CreateFromImage_Module(bool module, bool immutableArray)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var r = immutableArray
                ? MetadataReference.CreateFromImage(peImage.AsImmutable(), MetadataReferenceProperties.Module)
                : MetadataReference.CreateFromImage(peImage.AsEnumerable(), MetadataReferenceProperties.Module);

            Assert.IsAssignableFrom<ModuleMetadata>(r.GetMetadata());
            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryModule, r.Display);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
        }

        [Fact]
        public void CreateFromStream_FileStream()
        {
            var file = Temp.CreateFile().WriteAllBytes(Net461.Resources.mscorlib);
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
            var metadata = (AssemblyMetadata)r.GetMetadataNoCopy();
            Assert.Equal("CommonLanguageRuntimeLibrary", metadata.GetModules()[0].Name);
        }

        [Fact]
        public void CreateFromStream_MemoryStream()
        {
            var r = MetadataReference.CreateFromStream(new MemoryStream(TestResources.General.C1, writable: false));
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);

            Assert.Equal("C, Version=1.0.0.0, Culture=neutral, PublicKeyToken=374d0c2befcd8cc9",
                ((AssemblyMetadata)r.GetMetadataNoCopy()).GetAssembly().Identity.GetDisplayName());
        }

        [Theory, CombinatorialData]
        public void CreateFromStream_Assembly(bool module, bool explicitProperties)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var r = MetadataReference.CreateFromStream(
                new MemoryStream(peImage, writable: false),
                explicitProperties ? MetadataReferenceProperties.Assembly : default);

            Assert.IsAssignableFrom<AssemblyMetadata>(r.GetMetadata());
            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryAssembly, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
        }

        [Theory, CombinatorialData]
        public void CreateFromStream_Module(bool module)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var r = MetadataReference.CreateFromStream(
                new MemoryStream(peImage, writable: false),
                MetadataReferenceProperties.Module);

            Assert.IsAssignableFrom<ModuleMetadata>(r.GetMetadata());
            Assert.Null(r.FilePath);
            Assert.Equal(CodeAnalysisResources.InMemoryModule, r.Display);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);
        }

        [Theory, CombinatorialData]
        public void CreateFromFile_Assembly(bool module, bool explicitProperties)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var file = Temp.CreateFile().WriteAllBytes(peImage);

            var r = MetadataReference.CreateFromFile(file.Path,
                explicitProperties ? MetadataReferenceProperties.Assembly : default);
            Assert.IsAssignableFrom<AssemblyMetadata>(r.GetMetadata());
            Assert.Equal(file.Path, r.FilePath);
            Assert.Equal(file.Path, r.Display);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);

            var props = new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a", "b"), embedInteropTypes: true, hasRecursiveAliases: true);
            Assert.Equal(props, MetadataReference.CreateFromFile(file.Path, props).Properties);

            // check that the metadata is in memory and the file can be deleted:
            File.Delete(file.Path);
            var metadata = (AssemblyMetadata)r.GetMetadataNoCopy();
            Assert.Equal(module ? "ModuleCS00.netmodule" : "CommonLanguageRuntimeLibrary", metadata.GetModules()[0].Name);
        }

        [Theory, CombinatorialData]
        public void CreateFromFile_Module(bool module)
        {
            var peImage = module ? TestResources.MetadataTests.NetModule01.ModuleCS00 : Net461.Resources.mscorlib;
            var file = Temp.CreateFile().WriteAllBytes(peImage);

            var r = MetadataReference.CreateFromFile(file.Path, MetadataReferenceProperties.Module);
            Assert.IsAssignableFrom<ModuleMetadata>(r.GetMetadata());
            Assert.Equal(file.Path, r.FilePath);
            Assert.Equal(file.Path, r.Display);
            Assert.Equal(MetadataImageKind.Module, r.Properties.Kind);
            Assert.False(r.Properties.EmbedInteropTypes);
            Assert.True(r.Properties.Aliases.IsEmpty);

            var props = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.Equal(props, MetadataReference.CreateFromFile(file.Path, props).Properties);

            // check that the metadata is in memory and the file can be deleted:
            File.Delete(file.Path);
            var metadata = (ModuleMetadata)r.GetMetadataNoCopy();
            Assert.Equal(module ? "ModuleCS00.netmodule" : "CommonLanguageRuntimeLibrary", metadata.Name);
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
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
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
            Assert.NotNull(r.GetMetadataNoCopy());
            Assert.False(r.Properties.EmbedInteropTypes);
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
            Assert.NotNull(r.GetMetadataNoCopy());
            Assert.True(r.Properties.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, r.Properties.Kind);
            AssertEx.Equal(new[] { "a" }, r.Properties.Aliases);
            Assert.Equal(@"c:\temp", r.FilePath);

            var r2 = r.WithEmbedInteropTypes(true);
            Assert.Equal(r, r2);
            Assert.Equal(@"c:\temp", r2.FilePath);

            var r3 = r.WithAliases(ImmutableArray.Create("b", "c"));
            Assert.Same(r.DocumentationProvider, r3.DocumentationProvider);
            Assert.Same(r.GetMetadataNoCopy(), r3.GetMetadataNoCopy());
            Assert.Equal(r.Properties.EmbedInteropTypes, r3.Properties.EmbedInteropTypes);
            Assert.Equal(r.Properties.Kind, r3.Properties.Kind);
            AssertEx.Equal(new[] { "b", "c" }, r3.Properties.Aliases);
            Assert.Equal(r.FilePath, r3.FilePath);

            var r4 = r.WithEmbedInteropTypes(false);
            Assert.Same(r.DocumentationProvider, r4.DocumentationProvider);
            Assert.Same(r.GetMetadataNoCopy(), r4.GetMetadataNoCopy());
            Assert.False(r4.Properties.EmbedInteropTypes);
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
                : base(default, fullPath)
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
            public MyReference2(string fullPath)
                : base(properties: default, fullPath)
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

            var i1 = AssemblyMetadata.CreateFromImage(Net461.Resources.mscorlib).GetReference(display: "i1");
            var i2 = AssemblyMetadata.CreateFromImage(Net461.Resources.mscorlib).GetReference(display: "i2");

            var m1a = new MyReference(@"c:\a\goo.dll", display: "m1a");
            Assert.Equal("m1a", m1a.Display);
            var m1b = new MyReference(@"c:\b\..\a\goo.dll", display: "m1b");
            Assert.Equal("m1b", m1b.Display);
            var m2 = new MyReference(@"c:\b\goo.dll", display: "m2");
            Assert.Equal("m2", m2.Display);

            var c1a = CS.CSharpCompilation.Create("goo").ToMetadataReference();
            var c1b = c1a.Compilation.ToMetadataReference();
            var c2 = CS.CSharpCompilation.Create("goo").ToMetadataReference();

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
        public void DocCommentProvider()
        {
            var docProvider = new TestDocumentationProvider();
            var corlib = AssemblyMetadata.CreateFromImage(Net461.Resources.mscorlib).
                GetReference(display: "corlib", documentation: docProvider);

            var comp = (Compilation)CS.CSharpCompilation.Create("goo",
                syntaxTrees: new[] { CSharpTestSource.Parse("class C : System.Collections.ArrayList { }") },
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

            Assert.Throws<BadImageFormatException>((Func<object>)((AssemblyMetadata)r.GetMetadataNoCopy()).GetAssembly);
        }
    }
}
