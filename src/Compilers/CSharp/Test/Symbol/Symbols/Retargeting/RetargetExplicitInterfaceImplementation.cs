// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RetargetExplicitInterfaceImplementation : CSharpTestBase
    {
        [Fact]
        public void ExplicitInterfaceImplementationRetargeting()
        {
            var comp1 = CreateCompilation(
                new AssemblyIdentity("Assembly1"),
                new string[]
                { @"
public class C : Interface1
{
    void Interface1.Method1() { }
    void Interface1.Method2() { }
    void Interface1.Method3(bool b) { }
    void Interface1.Method4(Class1 c) { }
    string Interface1.Property1 { get; set; } 
    string Interface1.Property2 { get; set; } 
    string Interface1.Property3 { get; set; } 
    Class1 Interface1.Property4 { get; set; } 
    string Interface1.this[string x] { get { return null; } set { } }
    string Interface1.this[string x, string y] { get { return null; } set { } }
    string Interface1.this[string x, string y, string z] { get { return null; } set { } }
    Class1 Interface1.this[Class1 x, Class1 y, Class1 z, Class1 w] { get { return null; } set { } }
    event System.Action Interface1.Event1 { add { } remove { } } 
    event System.Action Interface1.Event2 { add { } remove { } } 
    event System.Action Interface1.Event3 { add { } remove { } } 
    event Delegate1 Interface1.Event4 { add { } remove { } } 
}
"
                },
                new[]
                    {
                        NetFramework.mscorlib,
                        TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                    });

            comp1.VerifyDiagnostics();

            var globalNamespace1 = comp1.GlobalNamespace;
            var classC = globalNamespace1.GetTypeMembers("C").Single();

            var interfaceV1 = globalNamespace1.GetTypeMembers("Interface1").Single();

            var interfaceV1Method1 = (MethodSymbol)interfaceV1.GetMembers("Method1").Single();
            var interfaceV1Method2 = (MethodSymbol)interfaceV1.GetMembers("Method2").Single();
            var interfaceV1Method3 = (MethodSymbol)interfaceV1.GetMembers("Method3").Single();
            var interfaceV1Method4 = (MethodSymbol)interfaceV1.GetMembers("Method4").Single();

            var interfaceV1Property1 = (PropertySymbol)interfaceV1.GetMembers("Property1").Single();
            var interfaceV1Property2 = (PropertySymbol)interfaceV1.GetMembers("Property2").Single();
            var interfaceV1Property3 = (PropertySymbol)interfaceV1.GetMembers("Property3").Single();
            var interfaceV1Property4 = (PropertySymbol)interfaceV1.GetMembers("Property4").Single();

            var interfaceV1Indexer1 = FindIndexerWithParameterCount(interfaceV1, 1);
            var interfaceV1Indexer2 = FindIndexerWithParameterCount(interfaceV1, 2);
            var interfaceV1Indexer3 = FindIndexerWithParameterCount(interfaceV1, 3);
            var interfaceV1Indexer4 = FindIndexerWithParameterCount(interfaceV1, 4);

            var interfaceV1Event1 = (EventSymbol)interfaceV1.GetMembers("Event1").Single();
            var interfaceV1Event2 = (EventSymbol)interfaceV1.GetMembers("Event2").Single();
            var interfaceV1Event3 = (EventSymbol)interfaceV1.GetMembers("Event3").Single();
            var interfaceV1Event4 = (EventSymbol)interfaceV1.GetMembers("Event4").Single();

            foreach (var member in classC.GetMembers())
            {
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                        var method = (MethodSymbol)member;
                        if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                        {
                            Assert.Equal(interfaceV1, method.ExplicitInterfaceImplementations.Single().ContainingType);
                        }
                        break;
                    case SymbolKind.Property:
                        Assert.Equal(interfaceV1, ((PropertySymbol)member).ExplicitInterfaceImplementations.Single().ContainingType);
                        break;
                    case SymbolKind.Event:
                        Assert.Equal(interfaceV1, ((EventSymbol)member).ExplicitInterfaceImplementations.Single().ContainingType);
                        break;
                    case SymbolKind.ErrorType:
                        Assert.True(false);
                        break;
                }
            }

            var comp2 = CreateCompilation(
                new AssemblyIdentity("Assembly2"),
                new string[]
                {@"
public  class D : C
{
}
"
                },
                new MetadataReference[]
                {
                        NetFramework.mscorlib,
                        TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                        new CSharpCompilationReference(comp1)
                });

            Assert.False(comp2.GetDiagnostics().Any());

            var globalNamespace2 = comp2.GlobalNamespace;

            var interfaceV2 = globalNamespace2.GetTypeMembers("Interface1").Single();
            Assert.NotSame(interfaceV1, interfaceV2);

            var interfaceV2Method1 = (MethodSymbol)interfaceV2.GetMembers("Method1").Single();
            //Method2 deleted
            var interfaceV2Method3 = (MethodSymbol)interfaceV2.GetMembers("Method3").Single();
            var interfaceV2Method4 = (MethodSymbol)interfaceV2.GetMembers("Method4").Single();

            var interfaceV2Property1 = (PropertySymbol)interfaceV2.GetMembers("Property1").Single();
            //Property2 deleted
            var interfaceV2Property3 = (PropertySymbol)interfaceV2.GetMembers("Property3").Single();
            var interfaceV2Property4 = (PropertySymbol)interfaceV2.GetMembers("Property4").Single();

            var interfaceV2Indexer1 = FindIndexerWithParameterCount(interfaceV2, 1);
            //Two-parameter indexer deleted
            var interfaceV2Indexer3 = FindIndexerWithParameterCount(interfaceV2, 3);
            var interfaceV2Indexer4 = FindIndexerWithParameterCount(interfaceV2, 4);

            var interfaceV2Event1 = (EventSymbol)interfaceV2.GetMembers("Event1").Single();
            //Event2 deleted
            var interfaceV2Event3 = (EventSymbol)interfaceV2.GetMembers("Event3").Single();
            var interfaceV2Event4 = (EventSymbol)interfaceV2.GetMembers("Event4").Single();

            var classD = globalNamespace2.GetTypeMembers("D").Single();
            var retargetedClassC = classD.BaseType();

            Assert.IsType<RetargetingNamedTypeSymbol>(retargetedClassC);

            Assert.Equal(interfaceV2, retargetedClassC.Interfaces().Single());

            var retargetedClassCMethod1 = (MethodSymbol)retargetedClassC.GetMembers("Interface1.Method1").Single();
            {
                Assert.IsType<RetargetingMethodSymbol>(retargetedClassCMethod1);
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, retargetedClassCMethod1.MethodKind);

                var retargetedClassCMethod1Impl = retargetedClassCMethod1.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Method1, retargetedClassCMethod1Impl);
                Assert.NotSame(interfaceV1Method1, retargetedClassCMethod1Impl);
                Assert.Equal(retargetedClassCMethod1Impl.ToTestDisplayString(), interfaceV1Method1.ToTestDisplayString());
            }

            var retargetedClassCMethod2 = (MethodSymbol)retargetedClassC.GetMembers("Interface1.Method2").Single();
            {
                Assert.IsType<RetargetingMethodSymbol>(retargetedClassCMethod2);
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, retargetedClassCMethod2.MethodKind);

                //since the method is missing from V2 of the interface
                Assert.False(retargetedClassCMethod2.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCMethod3 = (MethodSymbol)retargetedClassC.GetMembers("Interface1.Method3").Single();
            {
                Assert.IsType<RetargetingMethodSymbol>(retargetedClassCMethod3);
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, retargetedClassCMethod3.MethodKind);

                //since the method has a different signature in V2 of the interface
                Assert.False(retargetedClassCMethod3.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCMethod4 = (MethodSymbol)retargetedClassC.GetMembers("Interface1.Method4").Single();
            {
                Assert.IsType<RetargetingMethodSymbol>(retargetedClassCMethod4);
                Assert.Equal(MethodKind.ExplicitInterfaceImplementation, retargetedClassCMethod4.MethodKind);

                var retargetedClassCMethod4Impl = retargetedClassCMethod4.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Method4, retargetedClassCMethod4Impl);
                Assert.NotSame(interfaceV1Method4, retargetedClassCMethod4Impl);
                Assert.Equal(retargetedClassCMethod4Impl.ToTestDisplayString(), interfaceV1Method4.ToTestDisplayString());
            }

            var retargetedClassCProperty1 = (PropertySymbol)retargetedClassC.GetMembers("Interface1.Property1").Single();
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCProperty1);

                var retargetedClassCProperty1Impl = retargetedClassCProperty1.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Property1, retargetedClassCProperty1Impl);
                Assert.NotSame(interfaceV1Property1, retargetedClassCProperty1Impl);
                Assert.Equal(retargetedClassCProperty1Impl.Name, interfaceV1Property1.Name);
                Assert.Equal(retargetedClassCProperty1Impl.Type.ToTestDisplayString(), interfaceV1Property1.Type.ToTestDisplayString());
            }

            var retargetedClassCProperty2 = (PropertySymbol)retargetedClassC.GetMembers("Interface1.Property2").Single();
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCProperty2);

                //since the property is missing from V2 of the interface
                Assert.False(retargetedClassCProperty2.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCProperty3 = (PropertySymbol)retargetedClassC.GetMembers("Interface1.Property3").Single();
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCProperty3);

                //since the property has a different type in V2 of the interface
                Assert.False(retargetedClassCProperty3.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCProperty4 = (PropertySymbol)retargetedClassC.GetMembers("Interface1.Property4").Single();
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCProperty4);

                var retargetedClassCProperty4Impl = retargetedClassCProperty4.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Property4, retargetedClassCProperty4Impl);
                Assert.NotSame(interfaceV1Property4, retargetedClassCProperty4Impl);
                Assert.Equal(retargetedClassCProperty4Impl.Name, interfaceV1Property4.Name);
                Assert.Equal(retargetedClassCProperty4Impl.Type.ToTestDisplayString(), interfaceV1Property4.Type.ToTestDisplayString());
            }

            var retargetedClassCIndexer1 = FindIndexerWithParameterCount(retargetedClassC, 1);
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCIndexer1);

                var retargetedClassCIndexer1Impl = retargetedClassCIndexer1.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Indexer1, retargetedClassCIndexer1Impl);
                Assert.NotSame(interfaceV1Indexer1, retargetedClassCIndexer1Impl);
                Assert.Equal(retargetedClassCIndexer1Impl.Name, interfaceV1Indexer1.Name);
                Assert.Equal(retargetedClassCIndexer1Impl.Type.ToTestDisplayString(), interfaceV1Indexer1.Type.ToTestDisplayString());
            }

            var retargetedClassCIndexer2 = FindIndexerWithParameterCount(retargetedClassC, 2);
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCIndexer2);

                //since the property is missing from V2 of the interface
                Assert.False(retargetedClassCIndexer2.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCIndexer3 = FindIndexerWithParameterCount(retargetedClassC, 3);
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCIndexer3);

                //since the property has a different type in V2 of the interface
                Assert.False(retargetedClassCIndexer3.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCIndexer4 = FindIndexerWithParameterCount(retargetedClassC, 4);
            {
                Assert.IsType<RetargetingPropertySymbol>(retargetedClassCIndexer4);

                var retargetedClassCIndexer4Impl = retargetedClassCIndexer4.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Indexer4, retargetedClassCIndexer4Impl);
                Assert.NotSame(interfaceV1Indexer4, retargetedClassCIndexer4Impl);
                Assert.Equal(retargetedClassCIndexer4Impl.Name, interfaceV1Indexer4.Name);
                Assert.Equal(retargetedClassCIndexer4Impl.Type.ToTestDisplayString(), interfaceV1Indexer4.Type.ToTestDisplayString());
            }

            var retargetedClassCEvent1 = (EventSymbol)retargetedClassC.GetMembers("Interface1.Event1").Single();
            {
                Assert.IsType<RetargetingEventSymbol>(retargetedClassCEvent1);

                var retargetedClassCEvent1Impl = retargetedClassCEvent1.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Event1, retargetedClassCEvent1Impl);
                Assert.NotSame(interfaceV1Event1, retargetedClassCEvent1Impl);
                Assert.Equal(retargetedClassCEvent1Impl.Name, interfaceV1Event1.Name);
                Assert.Equal(retargetedClassCEvent1Impl.Type.ToTestDisplayString(), interfaceV1Event1.Type.ToTestDisplayString());
            }

            var retargetedClassCEvent2 = (EventSymbol)retargetedClassC.GetMembers("Interface1.Event2").Single();
            {
                Assert.IsType<RetargetingEventSymbol>(retargetedClassCEvent2);

                //since the event is missing from V2 of the interface
                Assert.False(retargetedClassCEvent2.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCEvent3 = (EventSymbol)retargetedClassC.GetMembers("Interface1.Event3").Single();
            {
                Assert.IsType<RetargetingEventSymbol>(retargetedClassCEvent3);

                //since the event has a different type in V2 of the interface
                Assert.False(retargetedClassCEvent3.ExplicitInterfaceImplementations.Any());
            }

            var retargetedClassCEvent4 = (EventSymbol)retargetedClassC.GetMembers("Interface1.Event4").Single();
            {
                Assert.IsType<RetargetingEventSymbol>(retargetedClassCEvent4);

                var retargetedClassCEvent4Impl = retargetedClassCEvent4.ExplicitInterfaceImplementations.Single();
                Assert.Same(interfaceV2Event4, retargetedClassCEvent4Impl);
                Assert.NotSame(interfaceV1Event4, retargetedClassCEvent4Impl);
                Assert.Equal(retargetedClassCEvent4Impl.Name, interfaceV1Event4.Name);
                Assert.Equal(retargetedClassCEvent4Impl.Type.ToTestDisplayString(), interfaceV1Event4.Type.ToTestDisplayString());
            }
        }

        private static PropertySymbol FindIndexerWithParameterCount(NamedTypeSymbol type, int parameterCount)
        {
            return type.GetMembers().Where(s => s.Kind == SymbolKind.Property).Cast<PropertySymbol>().Single(p => p.Parameters.Length == parameterCount);
        }

        [Fact]
        public void ExplicitInterfaceImplementationRetargetingGeneric()
        {
            var comp1 = CreateCompilation(
                new AssemblyIdentity("Assembly1"),
                new string[]
                { @"
public class C1<S> : Interface2<S>
{
    void Interface2<S>.Method1(S s) { }
    S Interface2<S>.Property1 { get; set; } 
    event System.Action<S> Interface2<S>.Event1 { add { } remove { } }
}

public class C2 : Interface2<int>
{
    void Interface2<int>.Method1(int i) { }
    int Interface2<int>.Property1 { get; set; } 
    event System.Action<int> Interface2<int>.Event1 { add { } remove { } }
}

public class C3 : Interface2<Class1>
{
    void Interface2<Class1>.Method1(Class1 c) { }
    Class1 Interface2<Class1>.Property1 { get; set; } 
    event System.Action<Class1> Interface2<Class1>.Event1 { add { } remove { } }
}
"
                },
                new[]
                    {
                        NetFramework.mscorlib,
                        TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                    });

            var d = comp1.GetDiagnostics();
            Assert.False(comp1.GetDiagnostics().Any());

            var globalNamespace1 = comp1.GlobalNamespace;
            var classC1 = globalNamespace1.GetTypeMembers("C1").Single();
            var classC2 = globalNamespace1.GetTypeMembers("C2").Single();
            var classC3 = globalNamespace1.GetTypeMembers("C3").Single();

            foreach (var diag in comp1.GetDiagnostics())
            {
                Console.WriteLine(diag);
            }

            var comp2 = CreateCompilation(
                new AssemblyIdentity("Assembly2"),
                new string[]
                {@"
public  class D1<R> : C1<R>
{
}
public  class D2 : C2
{
}
public  class D3 : C3
{
}
"
                },
                new MetadataReference[]
                {
                        NetFramework.mscorlib,
                        TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                        new CSharpCompilationReference(comp1)
                });

            Assert.False(comp2.GetDiagnostics().Any());

            var globalNamespace2 = comp2.GlobalNamespace;

            var interfaceV2 = globalNamespace2.GetTypeMembers("Interface2").Single();
            var interfaceV2Method1 = interfaceV2.GetMembers("Method1").Single();
            var interfaceV2Property1 = interfaceV2.GetMembers("Property1").Single();
            var interfaceV2Event1 = interfaceV2.GetMembers("Event1").Single();

            var classD1 = globalNamespace2.GetTypeMembers("D1").Single();
            var classD2 = globalNamespace2.GetTypeMembers("D2").Single();
            var classD3 = globalNamespace2.GetTypeMembers("D3").Single();

            var retargetedClassC1 = classD1.BaseType();
            var retargetedClassC2 = classD2.BaseType();
            var retargetedClassC3 = classD3.BaseType();

            var retargetedClassC1Method1 = (MethodSymbol)retargetedClassC1.GetMembers("Interface2<S>.Method1").Single();
            var retargetedClassC1Method1Impl = retargetedClassC1Method1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Method1, retargetedClassC1Method1Impl.OriginalDefinition);

            var retargetedClassC2Method1 = (MethodSymbol)retargetedClassC2.GetMembers("Interface2<System.Int32>.Method1").Single();
            var retargetedClassC2Method1Impl = retargetedClassC2Method1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Method1, retargetedClassC2Method1Impl.OriginalDefinition);

            var retargetedClassC3Method1 = (MethodSymbol)retargetedClassC3.GetMembers("Interface2<Class1>.Method1").Single();
            var retargetedClassC3Method1Impl = retargetedClassC3Method1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Method1, retargetedClassC3Method1Impl.OriginalDefinition);

            var retargetedClassC1Property1 = (PropertySymbol)retargetedClassC1.GetMembers("Interface2<S>.Property1").Single();
            var retargetedClassC1Property1Impl = retargetedClassC1Property1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Property1, retargetedClassC1Property1Impl.OriginalDefinition);

            var retargetedClassC2Property1 = (PropertySymbol)retargetedClassC2.GetMembers("Interface2<System.Int32>.Property1").Single();
            var retargetedClassC2Property1Impl = retargetedClassC2Property1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Property1, retargetedClassC2Property1Impl.OriginalDefinition);

            var retargetedClassC3Property1 = (PropertySymbol)retargetedClassC3.GetMembers("Interface2<Class1>.Property1").Single();
            var retargetedClassC3Property1Impl = retargetedClassC3Property1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Property1, retargetedClassC3Property1Impl.OriginalDefinition);

            var retargetedClassC1Event1 = (EventSymbol)retargetedClassC1.GetMembers("Interface2<S>.Event1").Single();
            var retargetedClassC1Event1Impl = retargetedClassC1Event1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Event1, retargetedClassC1Event1Impl.OriginalDefinition);

            var retargetedClassC2Event1 = (EventSymbol)retargetedClassC2.GetMembers("Interface2<System.Int32>.Event1").Single();
            var retargetedClassC2Event1Impl = retargetedClassC2Event1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Event1, retargetedClassC2Event1Impl.OriginalDefinition);

            var retargetedClassC3Event1 = (EventSymbol)retargetedClassC3.GetMembers("Interface2<Class1>.Event1").Single();
            var retargetedClassC3Event1Impl = retargetedClassC3Event1.ExplicitInterfaceImplementations.Single();
            Assert.Same(interfaceV2Event1, retargetedClassC3Event1Impl.OriginalDefinition);
        }

        [Fact]
        public void ExplicitInterfaceImplementationRetargetingGenericType()
        {
            var source1 = @"
public class C1<T>
{
    public interface I1
    {
        void M(T x);
    }
}
";
            var ref1 = CreateEmptyCompilation("").ToMetadataReference();
            var compilation1 = CreateCompilation(source1, references: new[] { ref1 });

            var source2 = @"
public class C2<U> : C1<U>.I1
{
    void C1<U>.I1.M(U x) {}
}
";
            var compilation2 = CreateCompilation(source2, references: new[] { compilation1.ToMetadataReference(), ref1, CreateEmptyCompilation("").ToMetadataReference() });

            var compilation3 = CreateCompilation("", references: new[] { compilation1.ToMetadataReference(), compilation2.ToMetadataReference() });

            Assert.NotSame(compilation2.GetTypeByMetadataName("C1`1"), compilation3.GetTypeByMetadataName("C1`1"));

            var c2 = compilation3.GetTypeByMetadataName("C2`1");
            Assert.IsType<RetargetingNamedTypeSymbol>(c2);

            var m = c2.GetMethod("C1<U>.I1.M");

            Assert.Equal(c2.Interfaces().Single().GetMethod("M"), m.ExplicitInterfaceImplementations.Single());
        }
    }
}
