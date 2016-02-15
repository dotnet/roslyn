// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.CorLibrary
{
    public class CorTypes : CSharpTestBase
    {
        private static readonly SymbolDisplayFormat s_languageNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        [Fact]
        public void MissingCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { TestReferences.SymbolsTests.CorLibrary.NoMsCorLibRef });

            var noMsCorLibRef = assemblies[0];

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                var t = noMsCorLibRef.GetSpecialType((SpecialType)i);
                Assert.Equal((SpecialType)i, t.SpecialType);
                Assert.Equal(TypeKind.Error, t.TypeKind);
                Assert.NotNull(t.ContainingAssembly);
                Assert.Equal("<Missing Core Assembly>", t.ContainingAssembly.Identity.Name);
            }

            var p = noMsCorLibRef.GlobalNamespace.GetTypeMembers("I1").Single().
                GetMembers("M1").OfType<MethodSymbol>().Single().
                Parameters[0].Type;

            Assert.Equal(TypeKind.Error, p.TypeKind);
            Assert.Equal(SpecialType.System_Int32, p.SpecialType);
        }

        [Fact]
        public void PresentCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { TestReferences.NetFx.v4_0_21006.mscorlib });

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                var t = msCorLibRef.GetSpecialType((SpecialType)i);
                Assert.Equal((SpecialType)i, t.SpecialType);
                Assert.Same(msCorLibRef, t.ContainingAssembly);
            }

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[] { MetadataReference.CreateFromImage(TestResources.NetFX.v4_0_30316_17626.mscorlib.AsImmutableOrNull()) });

            msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];
            Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            Queue<NamespaceSymbol> namespaces = new Queue<NamespaceSymbol>();

            namespaces.Enqueue(msCorLibRef.Modules[0].GlobalNamespace);
            int count = 0;

            while (namespaces.Count > 0)
            {
                foreach (var m in namespaces.Dequeue().GetMembers())
                {
                    NamespaceSymbol ns = m as NamespaceSymbol;

                    if (ns != null)
                    {
                        namespaces.Enqueue(ns);
                    }
                    else if (((NamedTypeSymbol)m).SpecialType != SpecialType.None)
                    {
                        count++;
                    }

                    if (count >= (int)SpecialType.Count)
                    {
                        Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                    }
                }
            }

            Assert.Equal(count, (int)SpecialType.Count);
            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
        }

        [Fact]
        public void FakeCorLib()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[] { TestReferences.SymbolsTests.CorLibrary.FakeMsCorLib.dll });

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)assemblies[0];

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                var t = msCorLibRef.GetSpecialType((SpecialType)i);
                Assert.Equal((SpecialType)i, t.SpecialType);

                if (t.SpecialType == SpecialType.System_Object)
                {
                    Assert.NotEqual(TypeKind.Error, t.TypeKind);
                }
                else
                {
                    Assert.Equal(TypeKind.Error, t.TypeKind);
                    Assert.Same(msCorLibRef, t.ContainingAssembly);
                }

                Assert.Same(msCorLibRef, t.ContainingAssembly);
            }

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
        }

        [Fact]
        public void SourceCorLib()
        {
            string source = @"
namespace System
{
    public class Object
    {
    }
}
";

            var c1 = CSharpCompilation.Create("CorLib", syntaxTrees: new[] { Parse(source) });

            Assert.Same(c1.Assembly, c1.Assembly.CorLibrary);

            MetadataOrSourceAssemblySymbol msCorLibRef = (MetadataOrSourceAssemblySymbol)c1.Assembly;

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                if (i != (int)SpecialType.System_Object)
                {
                    Assert.True(msCorLibRef.KeepLookingForDeclaredSpecialTypes);
                    var t = c1.GetSpecialType((SpecialType)i);
                    Assert.Equal((SpecialType)i, t.SpecialType);

                    Assert.Equal(TypeKind.Error, t.TypeKind);
                    Assert.Same(msCorLibRef, t.ContainingAssembly);
                }
            }

            var system_object = msCorLibRef.Modules[0].GlobalNamespace.GetMembers("System").
                Select(m => (NamespaceSymbol)m).Single().GetTypeMembers("Object").Single();

            Assert.Equal(SpecialType.System_Object, system_object.SpecialType);

            Assert.False(msCorLibRef.KeepLookingForDeclaredSpecialTypes);

            Assert.Same(system_object, c1.GetSpecialType(SpecialType.System_Object));

            Assert.Throws<ArgumentOutOfRangeException>(() => c1.GetSpecialType(SpecialType.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => c1.GetSpecialType(SpecialType.Count + 1));
        }

        [WorkItem(697521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697521")]
        [Fact]
        public void SubclassSystemArray()
        {
            var source1 = @"
namespace System
{
    public class Object
    {
    }

    public class Array : Object
    {
    }
}
";

            var source2 = @"
namespace System
{
    internal class ArrayContract : Array
    {
    }
}
";

            // Fine in corlib.
            CreateCompilation(source1 + source2).VerifyDiagnostics();

            // Error elsewhere.
            CreateCompilationWithMscorlib(source2).VerifyDiagnostics(
                // (4,20): error CS0644: 'System.ArrayContract' cannot derive from special class 'System.Array'
                //     internal class ArrayContract : Array
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "ArrayContract").WithArguments("System.ArrayContract", "System.Array"));
        }
    }
}
