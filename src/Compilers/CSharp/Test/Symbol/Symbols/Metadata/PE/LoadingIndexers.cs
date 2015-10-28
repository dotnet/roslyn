// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    // CONSIDER: it might be worthwhile to promote some of these sample types to a test resource DLL
    public class LoadingIndexers : CSharpTestBase
    {
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadReadWriteIndexer()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          set_Item(int32 x,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 C::get_Item(int32)
    .set instance void C::set_Item(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("Item", @class.DefaultMemberName);

                var indexer = @class.GetIndexer<PEPropertySymbol>("Item");
                CheckIndexer(indexer, true, true, "System.Int32 C.this[System.Int32 x] { get; set; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadWriteOnlyIndexer()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig specialname instance void 
          set_Item(int32 x,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .set instance void C::set_Item(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("Item", @class.DefaultMemberName);

                var indexer = @class.GetIndexer<PEPropertySymbol>("Item");
                CheckIndexer(indexer, false, true, "System.Int32 C.this[System.Int32 x] { set; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadReadOnlyIndexer()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 C::get_Item(int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("Item", @class.DefaultMemberName);

                var indexer = @class.GetIndexer<PEPropertySymbol>("Item");
                CheckIndexer(indexer, true, false, "System.Int32 C.this[System.Int32 x] { get; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadIndexerWithAlternateName()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('NotItem')}

  .method public hidebysig specialname instance int32 
          get_NotItem(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          set_NotItem(int32 x,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 NotItem(int32)
  {
    .get instance int32 C::get_NotItem(int32)
    .set instance void C::set_NotItem(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("NotItem", @class.DefaultMemberName);

                var indexer = @class.GetIndexer<PEPropertySymbol>("NotItem");
                CheckIndexer(indexer, true, true, "System.Int32 C.this[System.Int32 x] { get; set; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadIndexerWithAccessorAsDefaultMember()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_NotItem')}

  .method public hidebysig specialname instance int32 
          get_NotItem(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          set_NotItem(int32 x,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 NotItem(int32)
  {
    .get instance int32 C::get_NotItem(int32)
    .set instance void C::set_NotItem(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("get_NotItem", @class.DefaultMemberName);

                var indexer = @class.GetIndexer<PEPropertySymbol>("NotItem");
                CheckIndexer(indexer, true, true, "System.Int32 C.this[System.Int32 x] { get; set; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadComplexIndexers()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Accessor1')}

  .method public hidebysig specialname instance int32 
          Accessor1(int32 x, int64 y) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          Accessor2(int32 x, int64 y,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname instance void 
          Accessor3(int32 x, int64 y,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Indexer1(int32, int64)
  {
    .get instance int32 C::Accessor1(int32, int64)
    .set instance void C::Accessor2(int32, int64, int32)
  }

  .property instance int32 Indexer2(int32, int64)
  {
    .get instance int32 C::Accessor1(int32, int64)
    .set instance void C::Accessor3(int32, int64, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("Accessor1", @class.DefaultMemberName);

                var indexer1 = @class.GetIndexer<PEPropertySymbol>("Indexer1");
                CheckIndexer(indexer1, true, true, "System.Int32 C.this[System.Int32 x, System.Int64 y] { get; set; }", suppressAssociatedPropertyCheck: true);

                var indexer2 = @class.GetIndexer<PEPropertySymbol>("Indexer2");
                CheckIndexer(indexer2, true, true, "System.Int32 C.this[System.Int32 x, System.Int64 y] { get; set; }", suppressAssociatedPropertyCheck: true);
            });
        }

        [ClrOnlyFact]
        public void LoadNonIndexer_NoDefaultMember()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 C::get_Item(int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("", @class.DefaultMemberName); //placeholder value to avoid refetching

                var property = @class.GetMember<PEPropertySymbol>("Item");
                CheckNonIndexer(property, true, false, "System.Int32 C.Item[System.Int32 x] { get; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadNonIndexer_NotDefaultMember()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('NotItem')}

  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 C::get_Item(int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                Assert.Equal("NotItem", @class.DefaultMemberName);

                var property = @class.GetMember<PEPropertySymbol>("Item");
                CheckNonIndexer(property, true, false, "System.Int32 C.Item[System.Int32 x] { get; }");
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadNonGenericIndexers()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit NonGeneric
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_Item')}

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int64 x) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(int64 x, int32 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static int32  get_Item(int64 x) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static void  set_Item(int64 x, int32 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldnull
	throw
  }

  .property instance int32 Instance(int64)
  {
    .get instance int32 NonGeneric::get_Item(int64)
    .set instance void NonGeneric::set_Item(int64, int32)
  }

  .property int32 Static(int64)
  {
    .get int32 NonGeneric::get_Item(int64)
    .set void NonGeneric::set_Item(int64, int32)
  }
} // end of class NonGeneric
";

            CompileWithCustomILSource("", ilSource, compilation =>
                CheckInstanceAndStaticIndexers(compilation, "NonGeneric", "System.Int32 NonGeneric.this[System.Int64 x] { get; set; }"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadGenericIndexers()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit Generic`2<T,U>
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_Item')}

  .method public hidebysig newslot specialname virtual 
          instance !T  get_Item(!U u) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(!U u, !T 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static !T  get_Item(!U u) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static void  set_Item(!U u, !T 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldnull
	throw
  }

  .property instance !T Instance(!U)
  {
    .get instance !T Generic`2::get_Item(!U)
    .set instance void Generic`2::set_Item(!U, !T)
  }

  .property !T Static(!U)
  {
    .get !T Generic`2::get_Item(!U)
    .set void Generic`2::set_Item(!U, !T)
  }
} // end of class Generic`2
";

            CompileWithCustomILSource("", ilSource, compilation =>
                CheckInstanceAndStaticIndexers(compilation, "Generic", "T Generic<T, U>.this[U u] { get; set; }"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadClosedGenericIndexers()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit ClosedGeneric
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_Item')}

  .method public hidebysig newslot specialname virtual 
          instance class [mscorlib]System.Collections.Generic.List`1<int32> 
          get_Item(class [mscorlib]System.Action`1<int16> u) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(class [mscorlib]System.Action`1<int16> u,
                                  class [mscorlib]System.Collections.Generic.List`1<int32> 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static class [mscorlib]System.Collections.Generic.List`1<int32> 
          get_Item(class [mscorlib]System.Action`1<int16> u) cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig newslot specialname virtual 
          static void  set_Item(class [mscorlib]System.Action`1<int16> u,
                                  class [mscorlib]System.Collections.Generic.List`1<int32> 'value') cil managed
  {
    ldnull
	throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldnull
	throw
  }

  .property instance class [mscorlib]System.Collections.Generic.List`1<int32>
          Instance(class [mscorlib]System.Action`1<int16>)
  {
    .get instance class [mscorlib]System.Collections.Generic.List`1<int32> ClosedGeneric::get_Item(class [mscorlib]System.Action`1<int16>)
    .set instance void ClosedGeneric::set_Item(class [mscorlib]System.Action`1<int16>,
                                               class [mscorlib]System.Collections.Generic.List`1<int32>)
  }

  .property class [mscorlib]System.Collections.Generic.List`1<int32>
          Static(class [mscorlib]System.Action`1<int16>)
  {
    .get class [mscorlib]System.Collections.Generic.List`1<int32> ClosedGeneric::get_Item(class [mscorlib]System.Action`1<int16>)
    .set void ClosedGeneric::set_Item(class [mscorlib]System.Action`1<int16>,
                                               class [mscorlib]System.Collections.Generic.List`1<int32>)
  }
} // end of class ClosedGeneric
";

            CompileWithCustomILSource("", ilSource, compilation =>
                CheckInstanceAndStaticIndexers(compilation, "ClosedGeneric", "System.Collections.Generic.List<System.Int32> ClosedGeneric.this[System.Action<System.Int16> u] { get; set; }"));
        }

        [Fact]
        public void LoadIndexerWithRefParam()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.Indexers);
            var @class = assembly.GlobalNamespace.GetMember<NamedTypeSymbol>("RefIndexer");
            var indexer = (PropertySymbol)@class.GetMembers().Where(m => m.Kind == SymbolKind.Property).Single();
            Assert.Equal(RefKind.Ref, indexer.Parameters.Single().RefKind);
            Assert.True(indexer.MustCallMethodsDirectly);
        }

        private static void CheckInstanceAndStaticIndexers(CSharpCompilation compilation, string className, string indexerDisplayString)
        {
            var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>(className);

            var instanceIndexer = @class.GetIndexer<PEPropertySymbol>("Instance");
            Assert.False(instanceIndexer.IsStatic);
            CheckIndexer(instanceIndexer, true, true, indexerDisplayString);

            var staticIndexer = @class.GetIndexer<PEPropertySymbol>("Static"); //not allowed in C#
            Assert.True(staticIndexer.IsStatic);
            CheckIndexer(staticIndexer, true, true, indexerDisplayString);
        }

        /// <summary>
        /// The accessor and the property have signatures.
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadAccessorPropertySignatureMismatch()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('get_Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig specialname instance int32 
          get_Item(string s) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 ParameterCount(string, char)
  {
    .get instance int32 C::get_Item(string)
  }

  .method public hidebysig specialname instance int32 
          get_Item(string s, string c) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 ParameterTypes(string, char)
  {
    .get instance int32 C::get_Item(string, string)
  }

  .method public hidebysig specialname instance int32 
          get_Item(string s, char modopt(int32) c) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 ReturnType(string, char)
  {
    .get instance char C::get_Item(string, string)
  }

  .property instance int32 ParameterModopt(string, char)
  {
    .get instance int32 C::get_Item(string, char modopt(int32))
  }

  .method public hidebysig specialname instance char 
          get_Item(string s, string c) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance int32 modopt(int32) 
          get_Item(string s, char c) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 ReturnTypeModopt(string, char)
  {
    .get instance int32 modopt(int32) C::get_Item(string, char)
  }
} // end of class C
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

                var parameterCountIndexer = @class.GetIndexer<PEPropertySymbol>("ParameterCount");
                Assert.True(parameterCountIndexer.IsIndexer);
                Assert.True(parameterCountIndexer.MustCallMethodsDirectly);
                Assert.NotEqual(parameterCountIndexer.ParameterCount, parameterCountIndexer.GetMethod.ParameterCount);

                var parameterTypesIndexer = @class.GetIndexer<PEPropertySymbol>("ParameterTypes");
                Assert.True(parameterTypesIndexer.IsIndexer);
                Assert.True(parameterTypesIndexer.MustCallMethodsDirectly);
                Assert.NotEqual(parameterTypesIndexer.Parameters.Last().Type.TypeSymbol, parameterTypesIndexer.GetMethod.Parameters.Last().Type.TypeSymbol);

                var returnTypeIndexer = @class.GetIndexer<PEPropertySymbol>("ReturnType");
                Assert.True(returnTypeIndexer.IsIndexer);
                Assert.True(returnTypeIndexer.MustCallMethodsDirectly);
                Assert.NotEqual(returnTypeIndexer.Type.TypeSymbol, returnTypeIndexer.GetMethod.ReturnType.TypeSymbol);

                var parameterModoptIndexer = @class.GetIndexer<PEPropertySymbol>("ParameterModopt");
                Assert.True(parameterModoptIndexer.IsIndexer);
                Assert.False(parameterModoptIndexer.MustCallMethodsDirectly); //NB: we allow this amount of variation (modopt is on, rather than in parameter type)
                Assert.NotEqual(parameterModoptIndexer.Parameters.Last().Type.CustomModifiers.Length, parameterModoptIndexer.GetMethod.Parameters.Last().Type.CustomModifiers.Length);

                var returnTypeModoptIndexer = @class.GetIndexer<PEPropertySymbol>("ReturnTypeModopt");
                Assert.True(returnTypeModoptIndexer.IsIndexer);
                Assert.False(returnTypeModoptIndexer.MustCallMethodsDirectly); //NB: we allow this amount of variation (modopt is on, rather than in return type)
                Assert.NotEqual(returnTypeModoptIndexer.Type.CustomModifiers.Length, returnTypeModoptIndexer.GetMethod.ReturnType.CustomModifiers.Length);
            });
        }

        [ClrOnlyFact]
        public void LoadParameterNames()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 
          get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

// NB: getter and setter have different parameter names
  .method public hidebysig specialname instance void 
          set_Item(int32 y,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 ReadWrite(int32)
  {
    .get instance int32 C::get_Item(int32)
    .set instance void C::set_Item(int32, int32)
  }

  .property instance int32 ReadOnly(int32)
  {
    .get instance int32 C::get_Item(int32)
  }

  .property instance int32 WriteOnly(int32)
  {
    .set instance void C::set_Item(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

                var property1 = @class.GetMember<PEPropertySymbol>("ReadWrite");
                var property1ParamName = property1.Parameters.Single().Name;

                // NOTE: prefer setter
                Assert.NotEqual(property1ParamName, property1.GetMethod.Parameters.Single().Name);
                Assert.Equal(property1ParamName, property1.SetMethod.Parameters.First().Name);

                var property2 = @class.GetMember<PEPropertySymbol>("ReadOnly");
                var property2ParamName = property2.Parameters.Single().Name;

                Assert.Equal(property2ParamName, property2.GetMethod.Parameters.Single().Name);

                var property3 = @class.GetMember<PEPropertySymbol>("WriteOnly");
                var property3ParamName = property3.Parameters.Single().Name;

                Assert.Equal(property3ParamName, property3.SetMethod.Parameters.First().Name);
            });
        }

        /// <remarks>
        /// Only testing parameter count mismatch.  There isn't specific handling for other
        /// types of bogus properties - just setter param name if setter available and getter
        /// param name if getter available (i.e. same as success case).
        /// </remarks>
        [ClrOnlyFact]
        public void LoadBogusParameterNames()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 
          get_Item(int32 x, int32 y) cil managed
  {
    ldc.i4.0
    ret
  }

  // accessor has too many parameters
  .property instance int32 TooMany(int32)
  {
    .get instance int32 C::get_Item(int32, int32)
  }

  // accessor has too few parameters
  .property instance int32 TooFew(int32, int32, int32)
  {
    .get instance int32 C::get_Item(int32, int32)
  }
}
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

                var accessor = @class.GetMember<MethodSymbol>("get_Item");
                var accessParam0Name = accessor.Parameters[0].Name;
                var accessParam1Name = accessor.Parameters[1].Name;

                var property1 = @class.GetMember<PEPropertySymbol>("TooMany");
                Assert.Equal(accessParam0Name, property1.Parameters[0].Name);

                var property2 = @class.GetMember<PEPropertySymbol>("TooFew");
                var property2Params = property2.Parameters;
                Assert.Equal(accessParam0Name, property2Params[0].Name);
                Assert.Equal(accessParam1Name, property2Params[1].Name);
                Assert.Equal("value", property2Params[2].Name); //filler name
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadParamArrayAttribute()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('accessor')}

  .method public hidebysig specialname instance int32 
          accessor(int32[] a) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          accessor(int32[] a,
                   int32 'value') cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 ReadWrite(int32[])
  {
    .get instance int32 C::accessor(int32[])
    .set instance void C::accessor(int32[], int32)
  }

  .property instance int32 ReadOnly(int32[])
  {
    .get instance int32 C::accessor(int32[])
  }

  .property instance int32 WriteOnly(int32[])
  {
    .set instance void C::accessor(int32[], int32)
  }
} // end of class C
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

                var readWrite = @class.GetIndexer<PEPropertySymbol>("ReadWrite");
                Assert.True(readWrite.IsIndexer);
                Assert.False(readWrite.MustCallMethodsDirectly);
                Assert.True(readWrite.Parameters.Last().IsParams);

                var readOnly = @class.GetIndexer<PEPropertySymbol>("ReadOnly");
                Assert.True(readOnly.IsIndexer);
                Assert.False(readOnly.MustCallMethodsDirectly);
                Assert.True(readOnly.Parameters.Last().IsParams);

                var writeOnly = @class.GetIndexer<PEPropertySymbol>("WriteOnly");
                Assert.True(writeOnly.IsIndexer);
                Assert.False(writeOnly.MustCallMethodsDirectly);
                Assert.True(writeOnly.Parameters.Last().IsParams);
            });
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadBogusParamArrayAttribute()
        {
            string ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('params')}

  .method public hidebysig specialname instance int32 
          params(int32[] a) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ldc.i4.0
    ret
  }
  .method public hidebysig specialname instance int32 
          noParams(int32[] a) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void 
          params(int32[] a,
                   int32 'value') cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ret
  }

  .method public hidebysig specialname instance void 
          noParams(int32[] a,
                   int32 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 OnlyGetter(int32[])
  {
    .get instance int32 C::params(int32[])
    .set instance void C::noParams(int32[], int32)
  }

  .property instance int32 OnlySetter(int32[])
  {
    .get instance int32 C::noParams(int32[])
    .set instance void C::params(int32[], int32)
  }
} // end of class C
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");

                var readWrite = @class.GetIndexer<PEPropertySymbol>("OnlyGetter");
                Assert.True(readWrite.IsIndexer);
                Assert.True(readWrite.MustCallMethodsDirectly);
                Assert.False(readWrite.Parameters.Last().IsParams); //favour setter

                var readOnly = @class.GetIndexer<PEPropertySymbol>("OnlySetter");
                Assert.True(readWrite.IsIndexer);
                Assert.True(readOnly.MustCallMethodsDirectly);
                Assert.True(readOnly.Parameters.Last().IsParams); //favour setter
            });
        }

        private static void CheckIndexer(PropertySymbol indexer, bool expectGetter, bool expectSetter, string indexerDisplayString, bool suppressAssociatedPropertyCheck = false)
        {
            CheckParameterizedProperty(indexer, expectGetter, expectSetter, indexerDisplayString, true, suppressAssociatedPropertyCheck);
        }

        private static void CheckNonIndexer(PropertySymbol property, bool expectGetter, bool expectSetter, string propertyDisplayString)
        {
            CheckParameterizedProperty(property, expectGetter, expectSetter, propertyDisplayString, false, true);
        }

        private static void CheckParameterizedProperty(PropertySymbol property, bool expectGetter, bool expectSetter, string propertyDisplayString, bool expectIndexer, bool suppressAssociatedPropertyCheck)
        {
            Assert.Equal(SymbolKind.Property, property.Kind);
            Assert.Equal(expectIndexer, property.IsIndexer);
            Assert.NotEqual(expectIndexer, property.MustCallMethodsDirectly);
            Assert.Equal(propertyDisplayString, property.ToTestDisplayString());

            if (expectGetter)
            {
                CheckAccessorShape(property.GetMethod, true, property, expectIndexer, suppressAssociatedPropertyCheck);
            }
            else
            {
                Assert.Null(property.GetMethod);
            }

            if (expectSetter)
            {
                CheckAccessorShape(property.SetMethod, false, property, expectIndexer, suppressAssociatedPropertyCheck);
            }
            else
            {
                Assert.Null(property.SetMethod);
            }
        }

        private static void CheckAccessorShape(MethodSymbol accessor, bool accessorIsGetMethod, PropertySymbol property, bool propertyIsIndexer, bool suppressAssociatedPropertyCheck)
        {
            Assert.NotNull(accessor);
            if (propertyIsIndexer)
            {
                if (!suppressAssociatedPropertyCheck)
                {
                    Assert.Same(property, accessor.AssociatedSymbol);
                }
            }
            else
            {
                Assert.Null(accessor.AssociatedSymbol);
                Assert.Equal(MethodKind.Ordinary, accessor.MethodKind);
            }

            if (accessorIsGetMethod)
            {
                Assert.Equal(propertyIsIndexer ? MethodKind.PropertyGet : MethodKind.Ordinary, accessor.MethodKind);

                Assert.Equal(property.Type.TypeSymbol, accessor.ReturnType.TypeSymbol);
                Assert.Equal(property.ParameterCount, accessor.ParameterCount);
            }
            else
            {
                Assert.Equal(propertyIsIndexer ? MethodKind.PropertySet : MethodKind.Ordinary, accessor.MethodKind);

                Assert.Equal(SpecialType.System_Void, accessor.ReturnType.SpecialType);
                Assert.Equal(property.Type.TypeSymbol, accessor.Parameters.Last().Type.TypeSymbol);
                Assert.Equal(property.ParameterCount + 1, accessor.ParameterCount);
            }

            // NOTE: won't check last param of setter - that was handled above.
            for (int i = 0; i < property.ParameterCount; i++)
            {
                Assert.Equal(property.Parameters[i].Type.TypeSymbol, accessor.Parameters[i].Type.TypeSymbol);
            }

            Assert.Equal(property.IsAbstract, accessor.IsAbstract);
            Assert.Equal(property.IsOverride, @accessor.IsOverride);
            Assert.Equal(property.IsVirtual, @accessor.IsVirtual);
            Assert.Equal(property.IsSealed, @accessor.IsSealed);
            Assert.Equal(property.IsExtern, @accessor.IsExtern);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void LoadExplicitImplementation()
        {
            string ilSource = @"
.class interface public abstract auto ansi I
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_Item(int32 x) cil managed
  {
  } // end of method I::get_Item

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Item(int32 x,
                                  int32 'value') cil managed
  {
  } // end of method I::set_Item

  .property instance int32 Item(int32)
  {
    .get instance int32 I::get_Item(int32)
    .set instance void I::set_Item(int32,
                                   int32)
  } // end of property I::Item
} // end of class I

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements I
{
  .method private hidebysig newslot specialname virtual final 
          instance int32  I.get_Item(int32 x) cil managed
  {
    .override I::get_Item
    ldnull
    throw
  }

  .method private hidebysig newslot specialname virtual final 
          instance void  I.set_Item(int32 x,
                                    int32 'value') cil managed
  {
    .override I::set_Item
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 I.Item(int32)
  {
    .get instance int32 C::I.get_Item(int32)
    .set instance void C::I.set_Item(int32,
                                     int32)
  }
} // end of class C
";

            CompileWithCustomILSource("", ilSource, compilation =>
            {
                var @interface = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("I");
                var interfaceIndexer = @interface.Indexers.Single();
                Assert.True(interfaceIndexer.IsIndexer);

                var @class = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("C");
                var classIndexer = (PropertySymbol)@class.GetMembers().Single(s => s.Kind == SymbolKind.Property);
                Assert.False(classIndexer.IsIndexer);

                Assert.Equal(classIndexer, @class.FindImplementationForInterfaceMember(interfaceIndexer));
                Assert.Equal(interfaceIndexer, classIndexer.ExplicitInterfaceImplementations.Single());
            });
        }

        [Fact]
        public void LoadImplicitImplementation()
        {
        }

        [Fact]
        public void LoadOverriding()
        {
        }

        [Fact]
        public void LoadHiding()
        {
        }
    }
}
