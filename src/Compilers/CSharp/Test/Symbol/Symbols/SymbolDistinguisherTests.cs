// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolDistinguisherTests : CSharpTestBase
    {
        [Fact]
        public void TestSimpleDeclarations()
        {
            var source = @"
public class C
{
    public void M() { }
    public int P { get; set; }
    public int F;
    public event System.Action E { add { } remove { } }
}
";

            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            SymbolDistinguisher distinguisher;

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("C [file.cs(2)]", distinguisher.First.ToString());
            Assert.Equal("C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());

            var sourceMethod = sourceType.GetMember<MethodSymbol>("M");
            var referencedMethod = referencedType.GetMember<MethodSymbol>("M");
            distinguisher = new SymbolDistinguisher(comp, sourceMethod, referencedMethod);
            Assert.Equal("C.M() [file.cs(4)]", distinguisher.First.ToString());
            Assert.Equal("C.M() [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());

            var sourceProperty = sourceType.GetMember<PropertySymbol>("P");
            var referencedProperty = referencedType.GetMember<PropertySymbol>("P");
            distinguisher = new SymbolDistinguisher(comp, sourceProperty, referencedProperty);
            Assert.Equal("C.P [file.cs(5)]", distinguisher.First.ToString());
            Assert.Equal("C.P [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());

            var sourceField = sourceType.GetMember<FieldSymbol>("F");
            var referencedField = referencedType.GetMember<FieldSymbol>("F");
            distinguisher = new SymbolDistinguisher(comp, sourceField, referencedField);
            Assert.Equal("C.F [file.cs(6)]", distinguisher.First.ToString());
            Assert.Equal("C.F [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());

            var sourceEvent = sourceType.GetMember<EventSymbol>("E");
            var referencedEvent = referencedType.GetMember<EventSymbol>("E");
            distinguisher = new SymbolDistinguisher(comp, sourceEvent, referencedEvent);
            Assert.Equal("C.E [file.cs(7)]", distinguisher.First.ToString());
            Assert.Equal("C.E [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestCompilationReferenceLocation()
        {
            var source = @"public class C { }";

            var libRef = new CSharpCompilationReference(CreateCompilationWithMscorlib(Parse(source, "file1.cs"), assemblyName: "Metadata"));
            var comp = CreateCompilationWithMscorlib(Parse(source, "file2.cs"), new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("C [file2.cs(1)]", distinguisher.First.ToString());
            Assert.Equal("C [file1.cs(1)]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestFileReferenceLocation()
        {
            var source = @"public class C { }";

            var tree = Parse(source, "file.cs");

            var libComp = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata");
            var libRef = MetadataReference.CreateFromImage(libComp.EmitToArray(), filePath: "Metadata.dll");
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("C [file.cs(1)]", distinguisher.First.ToString());
            Assert.Equal(string.Format("C [Metadata.dll]"), distinguisher.Second.ToString());
        }

        [Fact]
        public void TestDistinctSymbolsWithSameLocation()
        {
            var source = @"public class C { }";
            var tree = Parse(source, "file.cs");

            var libRef = new CSharpCompilationReference(CreateCompilationWithMscorlib(tree, assemblyName: "Metadata"));
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("C [Source, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.First.ToString());
            Assert.Equal("C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestPathLocationsWithoutCompilation()
        {
            var source = @"public class C { }";

            var tree = Parse(source, @"a\..\file.cs");

            var libComp = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata");
            var libRef = MetadataReference.CreateFromImage(libComp.EmitToArray(), filePath: "Metadata.dll");

            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var distinguisher = new SymbolDistinguisher(null, sourceType, referencedType);
            Assert.Equal(@"C [a\..\file.cs(1)]", distinguisher.First.ToString()); // File path comes out of tree.
            Assert.Equal("C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestSourceLocationWithoutPath()
        {
            var source = @"public class C { }";

            var libRef = CreateCompilationWithMscorlib(source, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("C [Source, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.First.ToString());
            Assert.Equal("C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestParameterLocation()
        {
            var source = @"
public class C 
{ 
    public void M(ref C c) { }
}
";
            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceParameter = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").Parameters.Single();
            var referencedParameter = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").Parameters.Single();
            var distinguisher = new SymbolDistinguisher(comp, sourceParameter, referencedParameter);
            // NOTE: Locations come from parameter *types*.
            // NOTE: RefKind retained.
            Assert.Equal("ref C [file.cs(2)]", distinguisher.First.ToString());
            Assert.Equal("ref C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestArrayLocation()
        {
            var source = @"
public class C 
{ 
    public C[] F;
}
";
            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("F").Type;
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("F").Type;
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            // NOTE: Locations come from element types.
            Assert.Equal("C[] [file.cs(2)]", distinguisher.First.ToString());
            Assert.Equal("C[] [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestPointerLocation()
        {
            var source = @"
unsafe public struct S
{ 
    public S* F;
}
";
            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata", options: TestOptions.UnsafeReleaseDll).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source", options: TestOptions.UnsafeReleaseDll);

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("S").GetMember<FieldSymbol>("F").Type;
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("S").GetMember<FieldSymbol>("F").Type;
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            // NOTE: Locations come from element types.
            Assert.Equal("S* [file.cs(2)]", distinguisher.First.ToString());
            Assert.Equal("S* [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestArrayParameterLocation()
        {
            var source = @"
public class C 
{ 
    public void M(params C[] c) { }
}
";
            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceParameter = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").Parameters.Single();
            var referencedParameter = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").Parameters.Single();
            var distinguisher = new SymbolDistinguisher(comp, sourceParameter, referencedParameter);
            // NOTE: Locations come from parameter element types.
            // NOTE: 'params' retained.
            Assert.Equal("params C[] [file.cs(2)]", distinguisher.First.ToString());
            Assert.Equal("params C[] [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestTypeParameterLocation()
        {
            var source = @"public class C<T> { }";

            var tree = Parse(source, "file.cs");

            var libRef = CreateCompilationWithMscorlib(tree, assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(tree, new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").TypeParameters.Single();
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C").TypeParameters.Single();
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            // NOTE: Locations come from element types.
            Assert.Equal("T [file.cs(1)]", distinguisher.First.ToString());
            Assert.Equal("T [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestDynamicLocation()
        {
            var libRef = CreateCompilationWithMscorlib("public class dynamic { }", assemblyName: "Metadata").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib("", new[] { libRef }, assemblyName: "Source");

            var sourceAssembly = comp.SourceAssembly;
            var referencedAssembly = (AssemblySymbol)comp.GetAssemblyOrModuleSymbol(libRef);

            // I don't see how these types be reported as ambiguous, but we shouldn't blow up.
            var sourceType = DynamicTypeSymbol.Instance;
            var referencedType = referencedAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("dynamic");
            var distinguisher = new SymbolDistinguisher(comp, sourceType, referencedType);
            Assert.Equal("dynamic", distinguisher.First.ToString());
            Assert.Equal("dynamic [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.Second.ToString());
        }

        [Fact]
        public void TestMissingTypeLocation()
        {
            var dummyComp = CreateCompilation("", assemblyName: "Error");
            var errorType = dummyComp.GetSpecialType(SpecialType.System_Int32);
            var validType = CreateCompilationWithMscorlib("").GetSpecialType(SpecialType.System_Int32);

            Assert.NotEqual(TypeKind.Error, validType.TypeKind);
            Assert.Equal(TypeKind.Error, errorType.TypeKind);

            var distinguisher = new SymbolDistinguisher(dummyComp, errorType, validType);
            Assert.Equal("int [Error, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", distinguisher.First.ToString());
            Assert.Equal("int [mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]", distinguisher.Second.ToString());
        }

        [Fact]
        public void ERR_NoImplicitConvCast()
        {
            var libSource = @"
public interface I { }

public static class Lib
{
    public static I M() { return null; }
}
";

            var source = @"
public interface I { }

public class C
{
    public static void Main()
    {
        I i = Lib.M();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(8,9): warning CS0436: The type 'I' in 'file.cs' conflicts with the imported type 'I' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         I i = Lib.M();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "I").WithArguments("file.cs", "I", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "I").WithLocation(8, 9),
                // file.cs(8,15): error CS0266: Cannot implicitly convert type 'I [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]' to 'I [file.cs(2)]'. An explicit conversion exists (are you missing a cast?)
                //         I i = Lib.M();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Lib.M()").WithArguments("I [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "I [file.cs(2)]").WithLocation(8, 15));
        }

        [Fact]
        public void ERR_NoImplicitConv()
        {
            var libSource = @"
public struct S { }

public static class Lib
{
    public static S M() { return default(S); }
}
";

            var source = @"
public struct S { }

public class C
{
    public static void Main()
    {
        S s = Lib.M();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(8,9): warning CS0436: The type 'S' in 'file.cs' conflicts with the imported type 'S' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         S s = Lib.M();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "S").WithArguments("file.cs", "S", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "S").WithLocation(8, 9),
                // file.cs(8,15): error CS0029: Cannot implicitly convert type 'S [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]' to 'S [file.cs(2)]'
                //         S s = Lib.M();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Lib.M()").WithArguments("S [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "S [file.cs(2)]").WithLocation(8, 15));
        }

        [Fact]
        public void ERR_NoExplicitConv()
        {
            var libSource = @"
public struct S { }

public static class Lib
{
    public static S M() { return default(S); }
}
";

            var source = @"
public struct S { }

public class C
{
    public static void Main()
    {
        var s = (S)Lib.M();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(8,18): warning CS0436: The type 'S' in 'file.cs' conflicts with the imported type 'S' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         var s = (S)Lib.M();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "S").WithArguments("file.cs", "S", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "S").WithLocation(8, 18),
                // file.cs(8,17): error CS0030: Cannot convert type 'S [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]' to 'S [file.cs(2)]'
                //         var s = (S)Lib.M();
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S)Lib.M()").WithArguments("S [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "S [file.cs(2)]").WithLocation(8, 17));
        }

        [Fact]
        public void ERR_NoExplicitBuiltinConv()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static C M() { return default(C); }
}
";

            var source = @"
public class C
{
    public static void Main()
    {
        var c = Lib.M() as C;
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,28): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         var c = Lib.M() as C;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(6, 28),
                // file.cs(6,17): error CS0039: Cannot convert type 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]' to 'C [file.cs(2)]' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                //         var c = Lib.M() as C;
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "Lib.M() as C").WithArguments("C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "C [file.cs(2)]").WithLocation(6, 17));
        }

        [Fact]
        public void ERR_InvalidQM()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static C M() { return default(C); }
}
";

            var source = @"
public class C
{
    public static void Main(string[] args)
    {
        var c = args == null ? new C() : Lib.M();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,36): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         var c = args == null ? new C() : Lib.M();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(6, 36),
                // file.cs(6,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'C [file.cs(2)]' and 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'
                //         var c = args == null ? new C() : Lib.M();
                Diagnostic(ErrorCode.ERR_InvalidQM, "args == null ? new C() : Lib.M()").WithArguments("C [file.cs(2)]", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]").WithLocation(6, 17));
        }

        [Fact]
        public void ERR_BadParamType()
        {
            var libSource = @"
public class C { }
public delegate void D(C c);
";

            var source = @"

public class C
{
    public static void Main()
    {
        D d = (C c) => { };
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(7,16): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         D d = (C c) => { };
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(7, 16),
                // file.cs(7,15): error CS1661: Cannot convert lambda expression to delegate type 'D' because the parameter types do not match the delegate parameter types
                //         D d = (C c) => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "(C c) => { }").WithArguments("lambda expression", "D").WithLocation(7, 15),
                // file.cs(7,18): error CS1678: Parameter 1 is declared as type 'C [file.cs(3)]' but should be 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'
                //         D d = (C c) => { };
                Diagnostic(ErrorCode.ERR_BadParamType, "c").WithArguments("1", "", "C [file.cs(3)]", "", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]").WithLocation(7, 18));
        }

        [Fact]
        public void ERR_BadArgType()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static void M(C c) { }
}
";

            var source = @"
public class C
{
    public static void Main()
    {
        Lib.M(new C());
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,19): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         Lib.M(new C());
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(6, 19),
                // file.cs(6,15): error CS1503: Argument 1: cannot convert from 'C [file.cs(2)]' to 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'
                //         Lib.M(new C());
                Diagnostic(ErrorCode.ERR_BadArgType, "new C()").WithArguments("1", "C [file.cs(2)]", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]").WithLocation(6, 15));
        }

        [Fact]
        public void ERR_BadArgType_SameSourceLocation()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static void M(ref C c) { }
}
";

            var source = @"
public class C
{
    public static void Main()
    {
        var c = new C();
        Lib.M(ref c);
    }
}
";

            var libRef = new CSharpCompilationReference(CreateCompilationWithMscorlib(Parse(libSource, "file.cs"), assemblyName: "Metadata"));
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,21): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         var c = new C();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(6, 21),
                // file.cs(7,19): error CS1503: Argument 1: cannot convert from 'ref C [Source, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]' to 'ref C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'
                //         Lib.M(ref c);
                Diagnostic(ErrorCode.ERR_BadArgType, "c").WithArguments("1", "ref C [Source, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "ref C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]").WithLocation(7, 19));
        }

        [Fact]
        public void ERR_GenericConstraintNotSatisfiedRefType()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static void M<T>() where T : C { }
}
";

            var source = @"
public class C
{
    public static void Main()
    {
        Lib.M<C>();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,15): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         Lib.M<C>();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(6, 15),
                // file.cs(6,9): error CS0311: The type 'C [file.cs(2)]' cannot be used as type parameter 'T' in the generic type or method 'Lib.M<T>()'. There is no implicit reference conversion from 'C [file.cs(2)]' to 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'.
                //         Lib.M<C>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Lib.M<C>").WithArguments("Lib.M<T>()", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "T", "C [file.cs(2)]").WithLocation(6, 9));
        }

        // Not relevant: will always have one nullable and one non-nullable.
        //   public void ERR_GenericConstraintNotSatisfiedNullableEnum()
        // Not relevant: will always have one nullable and one interface.
        //   public void ERR_GenericConstraintNotSatisfiedNullableInterface()

        [Fact]
        public void ERR_GenericConstraintNotSatisfiedTyVar()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static void M<T>() where T : C { }
}
";

            var source = @"
public class Test<C> where C : struct
{
    public static void M()
    {
        Lib.M<C>();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(6,9): error CS0314: The type 'C [file.cs(2)]' cannot be used as type parameter 'T' in the generic type or method 'Lib.M<T>()'. There is no boxing conversion or type parameter conversion from 'C [file.cs(2)]' to 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'.
                //         Lib.M<C>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "Lib.M<C>").WithArguments("Lib.M<T>()", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "T", "C [file.cs(2)]").WithLocation(6, 9));
        }

        [Fact]
        public void ERR_GenericConstraintNotSatisfiedValType()
        {
            var libSource = @"
public class C { }

public static class Lib
{
    public static void M<T>() where T : C { }
}
";

            var source = @"
public struct C { } // NOTE: struct, not class

public class Test
{
    public static void Main()
    {
        Lib.M<C>();
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Metadata").EmitToImageReference();
            CreateCompilationWithMscorlib(Parse(source, "file.cs"), new[] { libRef }, assemblyName: "Source").VerifyDiagnostics(
                // file.cs(8,15): warning CS0436: The type 'C' in 'file.cs' conflicts with the imported type 'C' in 'Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in 'file.cs'.
                //         Lib.M<C>();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "C").WithArguments("file.cs", "C", "Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "C").WithLocation(8, 15),
                // file.cs(8,9): error CS0315: The type 'C [file.cs(2)]' cannot be used as type parameter 'T' in the generic type or method 'Lib.M<T>()'. There is no boxing conversion from 'C [file.cs(2)]' to 'C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]'.
                //         Lib.M<C>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "Lib.M<C>").WithArguments("Lib.M<T>()", "C [Metadata, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", "T", "C [file.cs(2)]").WithLocation(8, 9));
        }

        [WorkItem(6262)]
        [Fact]
        public void SymbolDistinguisherEquality()
        {
            var source =
@"class A { }
class B { }
class C { }";
            var compilation = CreateCompilationWithMscorlib(source);
            var sA = compilation.GetMember<NamedTypeSymbol>("A");
            var sB = compilation.GetMember<NamedTypeSymbol>("B");
            var sC = compilation.GetMember<NamedTypeSymbol>("C");
            Assert.True(AreEqual(new SymbolDistinguisher(compilation, sA, sB), new SymbolDistinguisher(compilation, sA, sB)));
            Assert.False(AreEqual(new SymbolDistinguisher(compilation, sA, sB), new SymbolDistinguisher(compilation, sA, sC)));
            Assert.False(AreEqual(new SymbolDistinguisher(compilation, sA, sB), new SymbolDistinguisher(compilation, sC, sB)));
        }

        private static bool AreEqual(SymbolDistinguisher a, SymbolDistinguisher b)
        {
            return a.First.Equals(b.First) && a.Second.Equals(b.Second);
        }
    }
}
