// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.ModuleInitializers
{
    [CompilerTrait(CompilerFeature.ModuleInitializers)]
    public sealed class IgnoredTests : CSharpTestBase
    {
        [Fact]
        public void IgnoredOnReturnValue()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [return: ModuleInitializer]
    internal static void M()
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnMethodParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M([ModuleInitializer] int p)
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnGenericParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    internal static void M<[ModuleInitializer] T>()
    {
    }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnClass()
        {
            string source = @"
using System.Runtime.CompilerServices;

[ModuleInitializer]
class C
{
    internal static void M() { }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnEvent()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public event System.Action E;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnProperty()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int P { get; set; }
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnIndexer()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int this[int p] => p;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnField()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer]
    public int F;
}

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnModule()
        {
            string source = @"
using System.Runtime.CompilerServices;

[module: ModuleInitializer]

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredOnAssembly()
        {
            string source = @"
using System.Runtime.CompilerServices;

[assembly: ModuleInitializer]

namespace System.Runtime.CompilerServices { class ModuleInitializerAttribute : System.Attribute { } }
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }

        [Fact]
        public void IgnoredWhenConstructorArgumentIsSpecified()
        {
            string source = @"
using System.Runtime.CompilerServices;

class C
{
    [ModuleInitializer(42)]
    internal static void M()
    {
    }
}

namespace System.Runtime.CompilerServices
{
    class ModuleInitializerAttribute : System.Attribute
    {
        public ModuleInitializerAttribute(int p) { }
    }
}
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal(MetadataImportOptions.All, ((PEModuleSymbol)module).ImportOptions);
                    var rootModuleType = (TypeSymbol)module.GlobalNamespace.GetMember("<Module>");
                    Assert.Null(rootModuleType.GetMember(".cctor"));
                });
        }
    }
}
