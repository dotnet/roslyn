// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class NoPiaLocalHideAndTypeSubstitutionTests : CSharpTestBase
    {
        [Fact]
        public void NoPiaTypeEquivalenceBetweenPIATypeInExternalAndLocalAssembly()
        {
            // Verify type equivalence between PIA type in external assembly and local assembly

            var localConsumer = CreateCompilation(assemblyName: "Dummy", source: (string[])null,
                         references: new MetadataReference[] {
                                                                TestReferences.SymbolsTests.NoPia.Pia1,
                                                                TestReferences.SymbolsTests.NoPia.LocalTypes1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            Assert.Equal(1, localConsumerRefsAsm.First(arg => arg.Name == "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblies().Length);
            Assert.Equal(1, localConsumerRefsAsm.First(arg => arg.Name == "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblySymbols().Length);
            Assert.Equal(localConsumerRefsAsm.First(arg => arg.Name == "mscorlib"), localConsumerRefsAsm.First(arg => arg.Name == "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblySymbols().ElementAt(0));

            var canonicalType1 = localConsumerRefsAsm.First(arg => arg.Name == "Pia1").GlobalNamespace.GetTypeMembers("I1").Single();
            var canonicalType2 = localConsumerRefsAsm.First(arg => arg.Name == "Pia1").GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().GetTypeMembers("I2").Single();

            NamedTypeSymbol classLocalType = localConsumerRefsAsm.First(arg => arg.Name == "LocalTypes1").GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            MethodSymbol methodSymbol = classLocalType.GetMembers("Test1").OfType<MethodSymbol>().Single();
            ImmutableArray<ParameterSymbol> param = methodSymbol.Parameters;

            Assert.Same(canonicalType1, param.Where(arg => arg.Type.Name == "I1").Select(arg => arg).Single().Type);
            Assert.Same(canonicalType2, param.Where(arg => arg.Type.Name == "I2").Select(arg => arg).Single().Type);
        }

        [Fact]
        public void NoPiaTypeSubstitutionReferToAnotherAssemblyLocalDefOfPIAInterface()
        {
            //Refer to another local assembly's local def of a PIA interface

            var localTypeSource1 = @"public class External1
{
    public const IDuplicates IDup =null;

    public static delegate IDuplicates MyFunc();
 
    public event GeneralEventScenario.EventHandler Event1;
    
    public FooStruct Structure1;
    public FooEnum Property1;
}";

            var localTypeSource2 = @"public static class LocalType1
{
    public void Test1(External1.MyFunc arg)
    {
    }
}";

            var localType1 = CreateEmptyCompilation(assemblyName: "Dummy1", source: new string[] { localTypeSource1 },
                references: new MetadataReference[]
                {
                    TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(true)
                });

            var localType2 = CreateEmptyCompilation(assemblyName: "Dummy2", source: new string[] { localTypeSource2 },
                references: new MetadataReference[]
                {
                    TestReferences.SymbolsTests.NoPia.GeneralPia,
                    new CSharpCompilationReference(localType1)
                });

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy3", source: CSharpTestSource.None,
                references: new MetadataReference[]
                {
                    TestReferences.SymbolsTests.NoPia.GeneralPiaCopy,
                    new CSharpCompilationReference(localType2),
                    new CSharpCompilationReference(localType1)
                });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            Assert.Equal(2, localConsumerRefsAsm.First(arg => arg.Name == "Dummy2").Modules.FirstOrDefault().GetReferencedAssemblies().Length);
            Assert.Equal(2, localConsumerRefsAsm.First(arg => arg.Name == "Dummy2").Modules.FirstOrDefault().GetReferencedAssemblySymbols().Length);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("LocalType1").Single();

            MethodSymbol methodSymbol = classLocalType.GetMembers("Test1").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, methodSymbol.Parameters.Single(arg => arg.Name == "arg").Type.Kind);
        }

        [Fact]
        public void NoPIALocalTypesEquivalentToEachOtherStructAsMethodParameterType()
        {
            //Structure - As method parameter type in external assembly (test this by passing the parameter with a variable which was declared in the current assembly)


            var localTypeSource = @"static class TypeSubstitution
{
    FooStruct myOwnVar = null;
    public static void Main()
    {
        myOwnVar = new FooStruct();
        myOwnVar.Structure = -1;
        ExternalAsm1.Scen1(myOwnVar);
    }
}
";

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: new string[] { localTypeSource },
                                                                references: new MetadataReference[] {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1,
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            Assert.Equal(3, localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").Modules.FirstOrDefault().GetReferencedAssemblies().Length);
            Assert.Equal(3, localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").Modules.FirstOrDefault().GetReferencedAssemblySymbols().Length);

            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.GetTypeMembers("FooStruct").Single();

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").Single();
            FieldSymbol localFieldSymbol = classLocalType.GetMembers("myOwnVar").OfType<FieldSymbol>().Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            MethodSymbol refMethodSymbol = classRefLocalType.GetMembers("Scen1").OfType<MethodSymbol>().Single();
            ImmutableArray<ParameterSymbol> param = refMethodSymbol.Parameters;
            NoPiaMissingCanonicalTypeSymbol missing = (NoPiaMissingCanonicalTypeSymbol)param.First().Type;

            Assert.Same(localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1"), missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.Identifier);
            Assert.Same(canonicalType, localFieldSymbol.Type);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
        }

        [Fact]
        public void NoPIALocalTypesEquivalentToEachOtherInterfaceAsMethodParameterType()
        {
            //Same as previous scenario but with Interface

            var localTypeSource = @"class ICBase : InheritanceConflict.IBase
{
    public int Bar()
    {
        return -2;
    }
    public void ConflictMethod(int x)
    {
    }
    public void Foo()
    {
    }
    public string this[object x]
    {
        get { return null; }
        set { }
    }
}
static class TypeSubstitution
{
    InheritanceConflict.IBase myOwnRef  =null; 
    public static void Main()
    {
        myOwnRef = new ICBase();
        ExternalAsm1.Scen2(myOwnRef);
    }
}";

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: new string[] { localTypeSource },
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.ChildNamespace("InheritanceConflict");
            var canonicalTypeInter = canonicalType.GetTypeMembers("IBase").Single();

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").Single();
            FieldSymbol localFieldSymbol = classLocalType.GetMembers("myOwnRef").OfType<FieldSymbol>().Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            MethodSymbol refMethodSymbol = classRefLocalType.GetMembers("Scen2").OfType<MethodSymbol>().Single();
            ImmutableArray<ParameterSymbol> param = refMethodSymbol.Parameters;

            Assert.Same(canonicalTypeInter, localFieldSymbol.Type);
            Assert.Same(canonicalTypeInter, param.First().Type);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(param.First().Type);
        }

        [Fact]
        public void NoPIALocalTypesEquivalentToEachOtherEnumAsReturnTypeInExternalAssembly()
        {
            //Enum - As return type in external assembly


            var localTypeSource = @"static class TypeSubstitution
{
    FooEnum myLocalType = 0;
    public static void Main()
    {
       FooEnum myLocalType = 0;
       myLocalType = ExternalAsm1.Scen3(5);
    }
}";

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: new string[] { localTypeSource },
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.GetTypeMembers("FooEnum").Single();

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").Single();
            FieldSymbol localFieldSymbol = classLocalType.GetMembers("myLocalType").OfType<FieldSymbol>().Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            MethodSymbol methodSymbol = classRefLocalType.GetMembers("Scen3").OfType<MethodSymbol>().Single();
            NoPiaMissingCanonicalTypeSymbol missing = (NoPiaMissingCanonicalTypeSymbol)methodSymbol.ReturnType;

            Assert.Same(localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1"), missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.Identifier);
            Assert.Same(canonicalType, localFieldSymbol.Type);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(methodSymbol.ReturnType);
        }

        [Fact]
        public void NoPIALocalTypesEquivalentToEachOtherInterfaceAsReturnTypeInExternalAssembly()
        {
            // Interface - As property in external assembly

            var localTypeSource = @"static class TypeSubstitution
{
    ISubFuncProp myLocalType = ExternalAsm1.Scen4;
}";

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: new string[] { localTypeSource },
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.GetTypeMembers("ISubFuncProp").Single();

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").Single();
            FieldSymbol localFieldSymbol = classLocalType.GetMembers("myLocalType").OfType<FieldSymbol>().Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            var methodSymbol = classRefLocalType.GetMembers("Scen4").OfType<PropertySymbol>().Single();
            var missing = methodSymbol.TypeWithAnnotations;

            Assert.Equal(canonicalType.ToTestDisplayString(), missing.Type.Name);
            Assert.Same(canonicalType, localFieldSymbol.Type);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(methodSymbol.Type);
        }

        [Fact]
        public void NoPIALocalTypesEquivalentToEachOtherDelegateAsReturnTypeInExternalAssembly()
        {
            //Same as previous scenario but with Delegate

            var localTypeSource = @"static class TypeSubstitution
{
    GeneralEventScenario.EventHandler myLocalType = ExternalAsm1.Scen5;
}";

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: new string[] { localTypeSource },
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var canonicalType = localConsumerRefsAsm[0].GlobalNamespace.ChildNamespace("GeneralEventScenario");
            var canonicalTypeInter = canonicalType.GetTypeMembers("EventHandler").Single();

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").Single();
            FieldSymbol localFieldSymbol = classLocalType.GetMembers("myLocalType").OfType<FieldSymbol>().Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            var methodSymbol = classRefLocalType.GetMembers("Scen5").OfType<PropertySymbol>().Single();
            var missing = (NamedTypeSymbol)methodSymbol.Type;

            Assert.Equal(SymbolKind.ErrorType, missing.Kind);
            Assert.Same(canonicalTypeInter, localFieldSymbol.Type);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(missing);
        }

        [Fact]
        public void NoPIATypeSubstitutionForClassThatImplementNoPiaInterface()
        {
            //Check type substitution when a class implement a PIA interface

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: CSharpTestSource.None,
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.GetTypeMembers("ISubFuncProp").Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("SubFuncProp").Single();
            MethodSymbol methodSymbol = classRefLocalType.GetMembers("Foo").OfType<MethodSymbol>().Single();
            var interfaceType = classRefLocalType.GetDeclaredInterfaces(null).First();

            Assert.Same(canonicalType, interfaceType);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(interfaceType);
        }

        [Fact]
        public void NoPIATypeSubstitutionForMethodExplicitlyImplementNoPiaInterface()
        {
            //Check type substitution for a method of a class that implement PIA interface

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: CSharpTestSource.None,
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var canonicalType = localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.GetTypeMembers("ISubFuncProp").Single();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("SubFuncProp").Single();
            MethodSymbol methodSymbol = classRefLocalType.GetMembers("Foo").OfType<MethodSymbol>().Single();

            Assert.Equal(0, methodSymbol.ExplicitInterfaceImplementations.Length);
        }

        [Fact]
        public void NoPiaAmbiguousCanonicalTypeSymbolForStruct()
        {
            //NoPiaAmbiguousCanonicalTypeSymbol for the corresponding interface type. 

            var localConsumer = CreateEmptyCompilation(assemblyName: "Dummy", source: CSharpTestSource.None,
                         references: new List<MetadataReference>()  {
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                       TestReferences.SymbolsTests.NoPia.GeneralPiaCopy,
                                                                       TestReferences.SymbolsTests.NoPia.ExternalAsm1
                                                                     });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            NamedTypeSymbol classRefLocalType = localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").Single();
            MethodSymbol refMethodSymbol = classRefLocalType.GetMembers("Scen2").OfType<MethodSymbol>().Single();
            ImmutableArray<ParameterSymbol> param = refMethodSymbol.Parameters;
            NoPiaAmbiguousCanonicalTypeSymbol ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param.First().Type;

            Assert.Equal(SymbolKind.ErrorType, param.First().Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param.First().Type);
            Assert.Same(localConsumerRefsAsm.First(arg => arg.Name == "ExternalAsm1"), ambiguous.EmbeddingAssembly);
            Assert.Same(localConsumerRefsAsm.First(arg => arg.Name == "GeneralPia").GlobalNamespace.ChildNamespace("InheritanceConflict").GetTypeMembers("IBase").Single(), ambiguous.FirstCandidate);
            Assert.Same(localConsumerRefsAsm.First(arg => arg.Name == "GeneralPiaCopy").GlobalNamespace.ChildNamespace("InheritanceConflict").GetTypeMembers("IBase").Single(), ambiguous.SecondCandidate);
        }

        [Fact]
        public void NoPiaLeaveOutAllidentifyingAttributesOnAStructButUseItInAPIAInterface()
        {
            //Leave out all identifying attributes on a Struct, but use it in a PIA interface, and cause the PIA interface to be imported

            var localTypeSource1 = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""NoPIAMixPIANonPIATypesIInterface.dll"")]

public struct Structure1
{
    public double Data;
}
public class Class1
{
}
[Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c6c""), ComImport(), CoClass(typeof(InterfaceImpl)), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IInterface 
{
    Structure1 Prop1 { get; set; }
    void Method1(ref Class1 c1);
}

[Guid(""c9dcf748-b634-4504-a7ce-348cf7c61891"")]
public class InterfaceImpl
{
}";

            var localTypeSource2 = @"public class IdentifyingAttributes
{
    Structure1 s = new IInterface().Prop1[0.0];
    public IInterface Foo()
    {
        return new IInterface();
    }
}";

            var localType1 = CreateCompilation(assemblyName: "Dummy1", source: localTypeSource1, references: null);

            var localType2 = CreateCompilation(assemblyName: "Dummy2", source: localTypeSource2,
            references: new List<MetadataReference>() { new CSharpCompilationReference(localType1, embedInteropTypes: true) });

            Assert.True(localType2.Assembly.GetNoPiaResolutionAssemblies().First(arg => arg.Name == "Dummy1").IsLinked);

            var localConsumer = CreateCompilation(source: "", assemblyName: "Dummy3",
                     references: new List<MetadataReference>() {
                                                                      new CSharpCompilationReference(localType2),
                                                                      new CSharpCompilationReference(localType1)
                                                                    });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var importedType = localConsumerRefsAsm.First(arg => arg.Name == "Dummy2").GlobalNamespace.GetTypeMembers("IdentifyingAttributes").Single();
            var methodSymbol = importedType.GetMembers("Foo").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, methodSymbol.ReturnType.Kind);
            Assert.IsAssignableFrom<SourceNamedTypeSymbol>(methodSymbol.ReturnType);
        }

        [Fact]
        public void NoPiaTypeSubstitutionWithHandAuthoredLocalType()
        {
            //Try to apply attributes to the local type that indicates that the type is intended to be used for type equivalence. 

            var localTypeSource = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class LocalTypes1
{
    public I1 Test1()
    {
        return null;
    }
}
[ComImport, Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), TypeIdentifier, CompilerGenerated, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface I1
{
}";

            var localType = CreateCompilation(assemblyName: "Dummy1", source: localTypeSource, references: null);

            var localConsumer = CreateCompilation(assemblyName: "Dummy2", source: "",
                    references: new List<MetadataReference>()  {
                                                                      TestReferences.SymbolsTests.NoPia.Pia1,
                                                                      new CSharpCompilationReference(localType)
                                                               });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();
            var importedTypeComp2 = localConsumerRefsAsm.First(arg => arg.Name == "Dummy1").GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            var embeddedType = importedTypeComp2.GetMembers("Test1").OfType<MethodSymbol>().Single();
            var importedTypeAsm = localConsumerRefsAsm.First(arg => arg.Name == "Pia1").GlobalNamespace.GetTypeMembers("I1").Single();

            Assert.Same(embeddedType.ReturnType, importedTypeAsm);
            Assert.Equal(SymbolKind.NamedType, embeddedType.ReturnType.Kind);
        }
    }
}

