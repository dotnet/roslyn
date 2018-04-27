// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class IsAccessibleWithin
    {
        [Fact]
        void TestUsingMockSymbols_01()
        {
            IAssemblySymbol mscorlibAssem = new AssemblySymbol(new AssemblyIdentity("mscorlib"));
            INamespaceSymbol mscorlibGlobalNS = new NamespaceSymbol("", mscorlibAssem);
            INamespaceSymbol mscorlibSystemNS = new NamespaceSymbol("System", mscorlibGlobalNS);
            INamedTypeSymbol objectType = new NamedTypeSymbol("Object", mscorlibSystemNS, Accessibility.Public, null);
            INamedTypeSymbol valueType = new NamedTypeSymbol("ValueType", mscorlibSystemNS, Accessibility.Public, objectType);
            INamedTypeSymbol intType = new NamedTypeSymbol("Int32", mscorlibSystemNS, Accessibility.Public, valueType);
            GenericNamedTypeSymbol ienumerable = new GenericNamedTypeSymbol("IEnumerable", 1, mscorlibSystemNS, Accessibility.Public, objectType);

            IAssemblySymbol sourceAssem = new AssemblySymbol(new AssemblyIdentity("only"));
            INamespaceSymbol globalNS = new NamespaceSymbol("", sourceAssem);

            //class A
            INamedTypeSymbol classA = new NamedTypeSymbol("A", globalNS, Accessibility.Internal, objectType);
            //{
            //    static private int priv;
            IFieldSymbol privField = new FieldSymbol("priv", classA, Accessibility.Private) { IsStatic = true };
            //    static public int pub;
            IFieldSymbol pubField = new FieldSymbol("pub", classA, Accessibility.Public) { IsStatic = true };
            //    protected int prot;
            IFieldSymbol protField = new FieldSymbol("prot", classA, Accessibility.Protected);
            //    static private Goo unknowntype;
            IErrorTypeSymbol unknownType = new ErrorType();

            //    private class K {}
            INamedTypeSymbol classK = new NamedTypeSymbol("K", classA, Accessibility.Private, objectType);

            //    private K[] karray;
            IArrayTypeSymbol karrayType = new ArrayType(classK);
            //    private A[] aarray;
            IArrayTypeSymbol aarrayType = new ArrayType(classA);
            //    private IEnumerable<K> kenum;
            INamedTypeSymbol kenumType = ienumerable.Construct(new[] { classK }, objectType);
            //    private IEnumerable<A> aenum;
            INamedTypeSymbol aenumType = ienumerable.Construct(new[] { classA }, objectType);
            //}

            //class B
            //{}
            INamedTypeSymbol classB = new NamedTypeSymbol("B", globalNS, Accessibility.Internal, objectType);

            //class ADerived: A
            //{}
            INamedTypeSymbol classADerived = new NamedTypeSymbol("ADerived", globalNS, Accessibility.Internal, classA);

            //class ADerived2: A
            //{}
            INamedTypeSymbol classADerived2 = new NamedTypeSymbol("ADerived2", globalNS, Accessibility.Internal, classA);

            ISymbol nullSymbol = null;
            //Assert.Throws<ArgumentNullException>(() => { classA.IsAccessibleWithin(nullSymbol); });
            //Assert.Throws<ArgumentNullException>(() => { nullSymbol.IsAccessibleWithin(classA); });
            //Assert.Throws<ArgumentException>(() => { classA.IsAccessibleWithin(pubField); });

            Assert.True(classA.IsAccessibleWithin(classB));
            Assert.True(pubField.IsAccessibleWithin(classB));
            Assert.False(privField.IsAccessibleWithin(classB));
            Assert.False(karrayType.IsAccessibleWithin(classB));
            Assert.True(aarrayType.IsAccessibleWithin(classB));
            Assert.False(kenumType.IsAccessibleWithin(classB));
            Assert.True(aenumType.IsAccessibleWithin(classB));
            Assert.True(unknownType.IsAccessibleWithin(classB));
            Assert.True(globalNS.IsAccessibleWithin(classB));
            Assert.True(protField.IsAccessibleWithin(classA));
            Assert.True(protField.IsAccessibleWithin(classA, classADerived));
            Assert.False(protField.IsAccessibleWithin(classB));
            Assert.False(protField.IsAccessibleWithin(classB, classADerived));
            Assert.True(protField.IsAccessibleWithin(classA));
            Assert.True(protField.IsAccessibleWithin(classADerived, classADerived));
            Assert.False(protField.IsAccessibleWithin(classADerived, classADerived2));

            Assert.True(classA.IsAccessibleWithin(sourceAssem));
            Assert.True(aarrayType.IsAccessibleWithin(sourceAssem));
            Assert.False(karrayType.IsAccessibleWithin(sourceAssem));
            Assert.False(classA.IsAccessibleWithin(mscorlibAssem));
            Assert.True(unknownType.IsAccessibleWithin(sourceAssem));
            Assert.True(mscorlibAssem.IsAccessibleWithin(sourceAssem));
        }

        [Fact]
        public void TestUsingMockSymbols_02()
        {
            IAssemblySymbol mscorlibAssem = new AssemblySymbol(new AssemblyIdentity("mscorlib"));
            INamespaceSymbol mscorlibGlobalNS = new NamespaceSymbol("", mscorlibAssem);
            INamespaceSymbol mscorlibSystemNS = new NamespaceSymbol("System", mscorlibGlobalNS);
            INamedTypeSymbol objectType = new NamedTypeSymbol("Object", mscorlibSystemNS, Accessibility.Public, null);
            INamedTypeSymbol valueType = new NamedTypeSymbol("ValueType", mscorlibSystemNS, Accessibility.Public, objectType);
            INamedTypeSymbol intType = new NamedTypeSymbol("Int32", mscorlibSystemNS, Accessibility.Public, valueType);

            //using SomeAlias = System.Int32;
            IAliasSymbol SomeAlias = new AliasSymbol("SomeAlias", intType);
            AssemblySymbol sourceAssem = new AssemblySymbol(new AssemblyIdentity("C1"));
            INamespaceSymbol globalNS = new NamespaceSymbol("", sourceAssem);

            //[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("C3")]
            sourceAssem.GiveAccessTo(new AssemblyIdentity("C3"));
            //internal class Outer
            INamedTypeSymbol Outer = new NamedTypeSymbol("Outer", globalNS, Accessibility.Internal, objectType);
            //{
            //    private class Inner
            INamedTypeSymbol Outer_Inner = new NamedTypeSymbol("Inner", Outer, Accessibility.Private, objectType);
            //    {
            //        public int Field;
            IFieldSymbol Outer_Inner_Field = new FieldSymbol("Field", Outer_Inner, Accessibility.Public);
            //    }

            //    private Inner* Pointer;
            IPointerTypeSymbol Outer_Pointer_Type = new PointerTypeSymbol(Outer_Inner);
            //    private int Integer = 1 + 2;
            IMethodSymbol IntegerPlus = new BuiltinOperatorSymbol();

            //    protected int Protected;
            IFieldSymbol Outer_Protected = new FieldSymbol("Protected", Outer, Accessibility.Protected);
            //    protected internal int ProtectedInternal;
            IFieldSymbol Outer_ProtectedInternal = new FieldSymbol("ProtectedInternal", Outer, Accessibility.ProtectedOrInternal);
            //    private protected int PrivateProtected;
            IFieldSymbol Outer_PrivateProtected = new FieldSymbol("PrivateProtected", Outer, Accessibility.ProtectedAndInternal);
            //}
            //internal class Other
            INamedTypeSymbol Other = new NamedTypeSymbol("Other", globalNS, Accessibility.Internal, objectType);
            //{
            //}
            //private class Private
            INamedTypeSymbol Private = new NamedTypeSymbol("Private", globalNS, Accessibility.Private, objectType);
            Assert.Equal(Accessibility.Private, Private.DeclaredAccessibility);
            //{
            //}
            //internal class Derived : Outer
            INamedTypeSymbol Derived = new NamedTypeSymbol("Derived", globalNS, Accessibility.Internal, Outer);
            //{
            //}

            sourceAssem = new AssemblySymbol(new AssemblyIdentity("C2"));
            globalNS = new NamespaceSymbol("", sourceAssem);
            //internal class InOtherCompilation
            //{
            //}
            INamedTypeSymbol InOtherCompilation = new NamedTypeSymbol("InOtherCompilation", globalNS, Accessibility.Internal, objectType);

            sourceAssem = new AssemblySymbol(new AssemblyIdentity("C3"));
            globalNS = new NamespaceSymbol("", sourceAssem);
            //internal class InFriendCompilation
            //{
            //}
            INamedTypeSymbol InFriendCompilation = new NamedTypeSymbol("InFriendCompilation", globalNS, Accessibility.Internal, objectType);

            Assert.True(SomeAlias.IsAccessibleWithin(Outer));
            Assert.True(Outer_Pointer_Type.IsAccessibleWithin(Outer));
            Assert.False(Outer_Pointer_Type.IsAccessibleWithin(Other));
            Assert.True(IntegerPlus.IsAccessibleWithin(Other));
            Assert.True(IntegerPlus.IsAccessibleWithin(Other));
            Assert.True(IntegerPlus.IsAccessibleWithin(sourceAssem));
            Assert.False(Private.IsAccessibleWithin(Other));
            Assert.False(Private.IsAccessibleWithin(Other));
            Assert.False(Private.IsAccessibleWithin(sourceAssem));
            Assert.False(Outer.IsAccessibleWithin(InOtherCompilation));
            Assert.True(Outer.IsAccessibleWithin(InFriendCompilation));
            Assert.False(Outer_Inner_Field.IsAccessibleWithin(Other));
            Assert.False(Outer_Protected.IsAccessibleWithin(Derived, Outer));
            Assert.True(Outer_ProtectedInternal.IsAccessibleWithin(Derived, Outer));
            Assert.False(Outer_PrivateProtected.IsAccessibleWithin(Derived, Outer));
            Assert.True(Outer_Protected.IsAccessibleWithin(Derived));
            Assert.True(Outer_ProtectedInternal.IsAccessibleWithin(Derived));
            Assert.True(Outer_PrivateProtected.IsAccessibleWithin(Derived));
            Assert.False(Outer_Protected.IsAccessibleWithin(sourceAssem));
            Assert.True(Outer_Protected.IsAccessibleWithin(Outer_Inner));
        }

        [Fact]
        public void TestUsingMockSymbols_03()
        {
            IAssemblySymbol mscorlibAssem = new AssemblySymbol(new AssemblyIdentity("mscorlib"));
            INamespaceSymbol mscorlibGlobalNS = new NamespaceSymbol("", mscorlibAssem);
            INamespaceSymbol mscorlibSystemNS = new NamespaceSymbol("System", mscorlibGlobalNS);
            INamedTypeSymbol objectType = new NamedTypeSymbol("Object", mscorlibSystemNS, Accessibility.Public, null);
            INamedTypeSymbol valueType = new NamedTypeSymbol("ValueType", mscorlibSystemNS, Accessibility.Public, objectType);
            INamedTypeSymbol intType = new NamedTypeSymbol("Int32", mscorlibSystemNS, Accessibility.Public, valueType);

            AssemblySymbol sourceAssem = new AssemblySymbol(new AssemblyIdentity("S1")) { IsInteractive = true };
            INamespaceSymbol globalNS = new NamespaceSymbol("", sourceAssem);
            INamedTypeSymbol a = new NamedTypeSymbol("A", globalNS, Accessibility.Internal, objectType);
            IFieldSymbol field1 = new FieldSymbol("Field", a, Accessibility.Internal);

            sourceAssem = new AssemblySymbol(new AssemblyIdentity("S2")) { IsInteractive = true };
            globalNS = new NamespaceSymbol("", sourceAssem);
            INamedTypeSymbol b = new NamedTypeSymbol("B", globalNS, Accessibility.Internal, objectType);

            // interactive assemblies are friends
            Assert.True(field1.IsAccessibleWithin(b));

            sourceAssem = new AssemblySymbol(new AssemblyIdentity("S3")) { IsInteractive = true };
            globalNS = new NamespaceSymbol("", sourceAssem);
            INamedTypeSymbol c = new NamedTypeSymbol("C", globalNS, Accessibility.Public, objectType) { TypeKind = TypeKind.Submission };
            IFieldSymbol field3 = new FieldSymbol("Field3", c, Accessibility.Private);

            sourceAssem = new AssemblySymbol(new AssemblyIdentity("S4")) { IsInteractive = true };
            globalNS = new NamespaceSymbol("", sourceAssem);
            INamedTypeSymbol d = new NamedTypeSymbol("D", globalNS, Accessibility.Internal, objectType) { TypeKind = TypeKind.Submission };
            IFieldSymbol field4 = new FieldSymbol("Field4", d, Accessibility.Protected);

            // submissions can see much
            Assert.True(field4.IsAccessibleWithin(c));
            Assert.True(field3.IsAccessibleWithin(d));
        }

        [Fact]
        public void TestUsingMockSymbols_04()
        {
            // check that equivalent symbols are treated as equal.

            IAssemblySymbol mscorlibAssem1 = new AssemblySymbol(new AssemblyIdentity("mscorlib"));
            INamespaceSymbol mscorlibGlobalNS1 = new NamespaceSymbol("", mscorlibAssem1);
            INamespaceSymbol mscorlibSystemNS1 = new NamespaceSymbol("System", mscorlibGlobalNS1);
            INamedTypeSymbol objectType1 = new NamedTypeSymbol("Object", mscorlibSystemNS1, Accessibility.Public, null);
            INamedTypeSymbol valueType1 = new NamedTypeSymbol("ValueType", mscorlibSystemNS1, Accessibility.Public, objectType1);
            INamedTypeSymbol intType1 = new NamedTypeSymbol("Int32", mscorlibSystemNS1, Accessibility.Public, valueType1);
            IFieldSymbol field1 = new FieldSymbol("Field", intType1, Accessibility.Private);

            IAssemblySymbol mscorlibAssem2 = new AssemblySymbol(new AssemblyIdentity("mscorlib")); // note same identity
            INamespaceSymbol mscorlibGlobalNS2 = new NamespaceSymbol("", mscorlibAssem2);
            INamespaceSymbol mscorlibSystemNS2 = new NamespaceSymbol("System", mscorlibGlobalNS2);
            INamedTypeSymbol objectType2 = new NamedTypeSymbol("Object", mscorlibSystemNS2, Accessibility.Public, null);
            INamedTypeSymbol valueType2 = new NamedTypeSymbol("ValueType", mscorlibSystemNS2, Accessibility.Public, objectType2);
            INamedTypeSymbol intType2 = new NamedTypeSymbol("Int32", mscorlibSystemNS2, Accessibility.Public, valueType2);
            IFieldSymbol field2 = new FieldSymbol("Field", intType2, Accessibility.Private);

            Assert.True(field1.IsAccessibleWithin(intType2));
            Assert.True(field2.IsAccessibleWithin(intType1));

            IAssemblySymbol mscorlibAssem3 = new AssemblySymbol(new AssemblyIdentity("mscorlib3")); // note different identity
            INamespaceSymbol mscorlibGlobalNS3 = new NamespaceSymbol("", mscorlibAssem3);
            INamespaceSymbol mscorlibSystemNS3 = new NamespaceSymbol("System", mscorlibGlobalNS3);
            INamedTypeSymbol objectType3 = new NamedTypeSymbol("Object", mscorlibSystemNS3, Accessibility.Public, null);
            INamedTypeSymbol valueType3 = new NamedTypeSymbol("ValueType", mscorlibSystemNS3, Accessibility.Public, objectType3);
            INamedTypeSymbol intType3 = new NamedTypeSymbol("Int32", mscorlibSystemNS3, Accessibility.Public, valueType3);
            IFieldSymbol field3 = new FieldSymbol("Field", intType3, Accessibility.Private);

            Assert.False(field1.IsAccessibleWithin(intType3));
            Assert.False(field3.IsAccessibleWithin(intType1));
        }

        [Fact]
        public void TestCrossCompilerEquivalence()
        {
            var csharpTree = CSharp.CSharpSyntaxTree.ParseText(@"
internal class A
{
    protected int P;
    internal int I;
    private int R;
    private class N
    {
    }
}

internal class B : A
{
}

internal class C : B
{
}
");
            var vbTree = VisualBasic.VisualBasicSyntaxTree.ParseText(@"
Friend Class A
    Protected P As Integer
    Friend I As Integer
    Private R As Integer
    Private Class N
    End Class
End Class

Friend Class B
    Inherits A
End Class

Friend Class C
    Inherits B
End Class
");
            var assemblyName = "MyAssembly";
            // Note that these two compilations claim to have the same assembly identity
            var csc = CSharpCompilation.Create(assemblyName, new[] { csharpTree }, new MetadataReference[] { /* references */ });
            var Ac = csc.GlobalNamespace.GetMembers("A")[0] as INamedTypeSymbol;
            var APc = Ac.GetMembers("P")[0];
            var AIc = Ac.GetMembers("I")[0];
            var ARc = Ac.GetMembers("R")[0];
            var ANc = Ac.GetMembers("N")[0] as INamedTypeSymbol;
            var Bc = csc.GlobalNamespace.GetMembers("B")[0] as INamedTypeSymbol;
            var Cc = csc.GlobalNamespace.GetMembers("C")[0] as INamedTypeSymbol;

            // A VB assembly that appears to be the same assembly, therefore containing the same types
            var vbc = VisualBasicCompilation.Create(assemblyName, new[] { vbTree }, new MetadataReference[] { /* references */ });
            var Av = vbc.GlobalNamespace.GetMembers("A")[0] as INamedTypeSymbol;
            var APv = Av.GetMembers("P")[0];
            var AIv = Av.GetMembers("I")[0];
            var ARv = Av.GetMembers("R")[0];
            var ANv = Av.GetMembers("N")[0] as INamedTypeSymbol;
            var Bv = vbc.GlobalNamespace.GetMembers("B")[0] as INamedTypeSymbol;
            var Cv = vbc.GlobalNamespace.GetMembers("C")[0] as INamedTypeSymbol;

            Assert.True(APc.IsAccessibleWithin(Bc, Cc));
            Assert.True(APc.IsAccessibleWithin(Bc, Cv));
            Assert.True(APc.IsAccessibleWithin(Bv, Cc));
            Assert.True(APc.IsAccessibleWithin(Bv, Cv));
            Assert.True(APv.IsAccessibleWithin(Bc, Cc));
            Assert.True(APv.IsAccessibleWithin(Bc, Cv));
            Assert.True(APv.IsAccessibleWithin(Bv, Cc));
            Assert.True(APv.IsAccessibleWithin(Bv, Cv));

            Assert.True(AIc.IsAccessibleWithin(Bc));
            Assert.True(AIc.IsAccessibleWithin(Bv));
            Assert.True(AIv.IsAccessibleWithin(Bc));
            Assert.True(AIv.IsAccessibleWithin(Bv));

            Assert.True(ARc.IsAccessibleWithin(ANc));
            Assert.True(ARc.IsAccessibleWithin(ANv));
            Assert.True(ARv.IsAccessibleWithin(ANc));
            Assert.True(ARv.IsAccessibleWithin(ANv));

            // A VB assembly that appears to be a different assembly, therefore containing different types
            vbc = VisualBasicCompilation.Create(assemblyName+"2", new[] { vbTree }, new MetadataReference[] { /* references */ });
            Av = vbc.GlobalNamespace.GetMembers("A")[0] as INamedTypeSymbol;
            APv = Av.GetMembers("P")[0];
            AIv = Av.GetMembers("I")[0];
            ARv = Av.GetMembers("R")[0];
            ANv = Av.GetMembers("N")[0] as INamedTypeSymbol;
            Bv = vbc.GlobalNamespace.GetMembers("B")[0] as INamedTypeSymbol;
            Cv = vbc.GlobalNamespace.GetMembers("C")[0] as INamedTypeSymbol;

            Assert.True(APc.IsAccessibleWithin(Bc, Cc)); // pure assemblyName
            Assert.False(APc.IsAccessibleWithin(Bc, Cv));
            Assert.False(APc.IsAccessibleWithin(Bv, Cc));
            Assert.False(APc.IsAccessibleWithin(Bv, Cv));
            Assert.False(APv.IsAccessibleWithin(Bc, Cc));
            Assert.False(APv.IsAccessibleWithin(Bc, Cv));
            Assert.False(APv.IsAccessibleWithin(Bv, Cc));
            Assert.True(APv.IsAccessibleWithin(Bv, Cv)); // pure assemblyName2

            Assert.True(AIc.IsAccessibleWithin(Bc));
            Assert.False(AIc.IsAccessibleWithin(Bv));
            Assert.False(AIv.IsAccessibleWithin(Bc));
            Assert.True(AIv.IsAccessibleWithin(Bv));

            Assert.True(ARc.IsAccessibleWithin(ANc));
            Assert.False(ARc.IsAccessibleWithin(ANv));
            Assert.False(ARv.IsAccessibleWithin(ANc));
            Assert.True(ARv.IsAccessibleWithin(ANv));
        }

        public abstract class Symbol : ISymbol
        {
            private readonly string _name;
            private readonly ISymbol _containingSymbol;
            private readonly Accessibility _accessibility;

            public Symbol(string name, ISymbol containingSymbol, Accessibility accessibility)
            {
                this._name = name;
                this._containingSymbol = containingSymbol;
                this._accessibility = accessibility;
            }

            ISymbol ISymbol.ContainingSymbol => _containingSymbol;

            IAssemblySymbol ISymbol.ContainingAssembly => _containingSymbol as IAssemblySymbol ?? _containingSymbol?.ContainingAssembly;

            INamedTypeSymbol ISymbol.ContainingType => _containingSymbol as INamedTypeSymbol ?? _containingSymbol?.ContainingType;

            bool ISymbol.IsDefinition => true;

            Accessibility ISymbol.DeclaredAccessibility
            {
                get
                {
                    // We should not be asking for accessibility for cases in which it makes no sense.
                    Assert.NotEqual(Accessibility.NotApplicable, _accessibility);
                    return _accessibility;
                }
            }

            ISymbol ISymbol.OriginalDefinition => this;

            #region unimplemented members
            IModuleSymbol ISymbol.ContainingModule => throw new NotImplementedException();

            INamespaceSymbol ISymbol.ContainingNamespace => throw new NotImplementedException();

            SymbolKind ISymbol.Kind => throw new NotImplementedException();

            string ISymbol.Language => throw new NotImplementedException();

            string ISymbol.Name => _name;

            string ISymbol.MetadataName => throw new NotImplementedException();

            public bool IsStatic { get; set; }

            bool ISymbol.IsVirtual => throw new NotImplementedException();

            bool ISymbol.IsOverride => throw new NotImplementedException();

            bool ISymbol.IsAbstract => throw new NotImplementedException();

            bool ISymbol.IsSealed => throw new NotImplementedException();

            bool ISymbol.IsExtern => throw new NotImplementedException();

            bool ISymbol.IsImplicitlyDeclared => throw new NotImplementedException();

            bool ISymbol.CanBeReferencedByName => throw new NotImplementedException();

            ImmutableArray<Location> ISymbol.Locations => throw new NotImplementedException();

            ImmutableArray<SyntaxReference> ISymbol.DeclaringSyntaxReferences => throw new NotImplementedException();

            bool ISymbol.HasUnsupportedMetadata => throw new NotImplementedException();

            void ISymbol.Accept(SymbolVisitor visitor)
            {
                throw new NotImplementedException();
            }

            TResult ISymbol.Accept<TResult>(SymbolVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            bool IEquatable<ISymbol>.Equals(ISymbol other)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<AttributeData> ISymbol.GetAttributes()
            {
                throw new NotImplementedException();
            }

            string ISymbol.GetDocumentationCommentId()
            {
                throw new NotImplementedException();
            }

            string ISymbol.GetDocumentationCommentXml(CultureInfo preferredCulture, bool expandIncludes, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<SymbolDisplayPart> ISymbol.ToDisplayParts(SymbolDisplayFormat format)
            {
                throw new NotImplementedException();
            }

            string ISymbol.ToDisplayString(SymbolDisplayFormat format)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<SymbolDisplayPart> ISymbol.ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format)
            {
                throw new NotImplementedException();
            }

            string ISymbol.ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class AssemblySymbol : Symbol, IAssemblySymbol
        {
            private readonly AssemblyIdentity _identity;
            private readonly HashSet<AssemblyIdentity> _givesAccessTo = new HashSet<AssemblyIdentity>();

            public AssemblySymbol(AssemblyIdentity identity) : base(null, null, Accessibility.NotApplicable)
            {
                this._identity = identity;
            }

            SymbolKind ISymbol.Kind => SymbolKind.Assembly;

            public bool IsInteractive { get; set; } = false;

            AssemblyIdentity IAssemblySymbol.Identity => _identity;

            // helper function for tests
            public void GiveAccessTo(AssemblyIdentity otherIdentity)
            {
                _givesAccessTo.Add(otherIdentity);
            }

            bool IAssemblySymbol.GivesAccessTo(IAssemblySymbol toAssembly) => _givesAccessTo.Contains(toAssembly.Identity);

            #region unimplemented members
            INamespaceSymbol IAssemblySymbol.GlobalNamespace => throw new NotImplementedException();

            IEnumerable<IModuleSymbol> IAssemblySymbol.Modules => throw new NotImplementedException();

            ICollection<string> IAssemblySymbol.TypeNames => throw new NotImplementedException();

            ICollection<string> IAssemblySymbol.NamespaceNames => throw new NotImplementedException();

            bool IAssemblySymbol.MightContainExtensionMethods => throw new NotImplementedException();

            AssemblyMetadata IAssemblySymbol.GetMetadata()
            {
                throw new NotImplementedException();
            }

            INamedTypeSymbol IAssemblySymbol.GetTypeByMetadataName(string fullyQualifiedMetadataName)
            {
                throw new NotImplementedException();
            }

            INamedTypeSymbol IAssemblySymbol.ResolveForwardedType(string fullyQualifiedMetadataName)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
        {
            public NamespaceOrTypeSymbol(string name, ISymbol containingSymbol, Accessibility accessibility) : base(name, containingSymbol, accessibility)
            {
            }

            #region unimplemented members
            bool INamespaceOrTypeSymbol.IsNamespace => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsType => throw new NotImplementedException();

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public abstract class TypeSymbol : Symbol, ITypeSymbol
        {
            public TypeSymbol(string name, ISymbol containingSymbol, Accessibility accessibility) : base(name, containingSymbol, accessibility)
            {
            }

            ITypeSymbol ITypeSymbol.OriginalDefinition => this;

            #region unimplemented members
            TypeKind ITypeSymbol.TypeKind => throw new NotImplementedException();

            INamedTypeSymbol ITypeSymbol.BaseType => throw new NotImplementedException();

            ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces => throw new NotImplementedException();

            ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces => throw new NotImplementedException();

            bool ITypeSymbol.IsReferenceType => throw new NotImplementedException();

            bool ITypeSymbol.IsValueType => throw new NotImplementedException();

            bool ITypeSymbol.IsAnonymousType => throw new NotImplementedException();

            bool ITypeSymbol.IsTupleType => throw new NotImplementedException();

            SpecialType ITypeSymbol.SpecialType => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsNamespace => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsType => throw new NotImplementedException();

            ISymbol ITypeSymbol.FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
        {
            private readonly ITypeSymbol _elementType;

            public ArrayTypeSymbol(ITypeSymbol elementType) : base(null, null, Accessibility.NotApplicable)
            {
                this._elementType = elementType;
            }

            TypeKind ITypeSymbol.TypeKind => TypeKind.Array;

            SymbolKind ISymbol.Kind => SymbolKind.ArrayType;

            ITypeSymbol IArrayTypeSymbol.ElementType => _elementType;

            #region unimplemented members
            int IArrayTypeSymbol.Rank => throw new NotImplementedException();

            bool IArrayTypeSymbol.IsSZArray => throw new NotImplementedException();

            ImmutableArray<int> IArrayTypeSymbol.LowerBounds => throw new NotImplementedException();

            ImmutableArray<int> IArrayTypeSymbol.Sizes => throw new NotImplementedException();

            ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers => throw new NotImplementedException();

            bool IArrayTypeSymbol.Equals(IArrayTypeSymbol other)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class PointerTypeSymbol : TypeSymbol, IPointerTypeSymbol
        {
            private readonly ITypeSymbol _pointedAtType;

            public PointerTypeSymbol(ITypeSymbol pointedAtType) : base(null, null, Accessibility.NotApplicable)
            {
                this._pointedAtType = pointedAtType;
            }

            ITypeSymbol IPointerTypeSymbol.PointedAtType => _pointedAtType;

            TypeKind ITypeSymbol.TypeKind => TypeKind.Pointer;

            SymbolKind ISymbol.Kind => SymbolKind.PointerType;

            #region unimplemented members
            ImmutableArray<CustomModifier> IPointerTypeSymbol.CustomModifiers => throw new NotImplementedException();
            #endregion unimplemented members
        }

        public class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
        {
            private readonly INamedTypeSymbol _baseType;

            public NamedTypeSymbol(string name, ISymbol containingSymbol, Accessibility accessibility, INamedTypeSymbol baseType) : base(name, containingSymbol, accessibility)
            {
                _baseType = baseType;
            }

            public TypeKind TypeKind { get; set; } = TypeKind.Class;

            SymbolKind ISymbol.Kind => SymbolKind.NamedType;

            INamedTypeSymbol ITypeSymbol.BaseType => _baseType;

            ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments => ImmutableArray<ITypeSymbol>.Empty;

            INamedTypeSymbol INamedTypeSymbol.OriginalDefinition => this;

            int INamedTypeSymbol.Arity => 0;

            #region unimplemented members
            bool INamedTypeSymbol.IsGenericType => throw new NotImplementedException();

            bool INamedTypeSymbol.IsUnboundGenericType => throw new NotImplementedException();

            bool INamedTypeSymbol.IsScriptClass => throw new NotImplementedException();

            bool INamedTypeSymbol.IsImplicitClass => throw new NotImplementedException();

            bool INamedTypeSymbol.IsComImport => throw new NotImplementedException();

            IEnumerable<string> INamedTypeSymbol.MemberNames => throw new NotImplementedException();

            ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters => throw new NotImplementedException();

            IMethodSymbol INamedTypeSymbol.DelegateInvokeMethod => throw new NotImplementedException();

            INamedTypeSymbol INamedTypeSymbol.EnumUnderlyingType => throw new NotImplementedException();

            INamedTypeSymbol INamedTypeSymbol.ConstructedFrom => throw new NotImplementedException();

            ImmutableArray<IMethodSymbol> INamedTypeSymbol.InstanceConstructors => throw new NotImplementedException();

            ImmutableArray<IMethodSymbol> INamedTypeSymbol.StaticConstructors => throw new NotImplementedException();

            ImmutableArray<IMethodSymbol> INamedTypeSymbol.Constructors => throw new NotImplementedException();

            ISymbol INamedTypeSymbol.AssociatedSymbol => throw new NotImplementedException();

            bool INamedTypeSymbol.MightContainExtensionMethods => throw new NotImplementedException();

            INamedTypeSymbol INamedTypeSymbol.TupleUnderlyingType => throw new NotImplementedException();

            ImmutableArray<IFieldSymbol> INamedTypeSymbol.TupleElements => throw new NotImplementedException();

            bool INamedTypeSymbol.IsSerializable => throw new NotImplementedException();

            INamedTypeSymbol INamedTypeSymbol.Construct(params ITypeSymbol[] typeArguments)
            {
                throw new NotImplementedException();
            }

            INamedTypeSymbol INamedTypeSymbol.ConstructUnboundGenericType()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<CustomModifier> INamedTypeSymbol.GetTypeArgumentCustomModifiers(int ordinal)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class ErrorType : NamedTypeSymbol, IErrorTypeSymbol
        {
            public ErrorType() : base(string.Empty, null, Accessibility.Public, null) { }

            SymbolKind ISymbol.Kind => SymbolKind.ErrorType;

            #region unimplemented members
            ImmutableArray<ISymbol> IErrorTypeSymbol.CandidateSymbols => throw new NotImplementedException();

            CandidateReason IErrorTypeSymbol.CandidateReason => throw new NotImplementedException();
            #endregion unimplemented members
        }

        public class NamespaceSymbol : NamespaceOrTypeSymbol, INamespaceSymbol
        {
            public NamespaceSymbol(string name, ISymbol containingSymbol) : base(name, containingSymbol, Accessibility.NotApplicable)
            {
            }

            SymbolKind ISymbol.Kind => SymbolKind.Namespace;

            #region unimplemented members
            bool INamespaceSymbol.IsGlobalNamespace => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsNamespace => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsType => throw new NotImplementedException();

            NamespaceKind INamespaceSymbol.NamespaceKind => throw new NotImplementedException();

            Compilation INamespaceSymbol.ContainingCompilation => throw new NotImplementedException();

            ImmutableArray<INamespaceSymbol> INamespaceSymbol.ConstituentNamespaces => throw new NotImplementedException();

            IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers()
            {
                throw new NotImplementedException();
            }

            IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers(string name)
            {
                throw new NotImplementedException();
            }

            IEnumerable<INamespaceSymbol> INamespaceSymbol.GetNamespaceMembers()
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class FieldSymbol : Symbol, IFieldSymbol
        {
            public FieldSymbol(string name, INamedTypeSymbol containingSymbol, Accessibility accessibility) : base(name, containingSymbol, accessibility)
            {
            }

            SymbolKind ISymbol.Kind => SymbolKind.Field;

            #region unimplemented members
            ISymbol IFieldSymbol.AssociatedSymbol => throw new NotImplementedException();

            bool IFieldSymbol.IsConst => throw new NotImplementedException();

            bool IFieldSymbol.IsReadOnly => throw new NotImplementedException();

            bool IFieldSymbol.IsVolatile => throw new NotImplementedException();

            ITypeSymbol IFieldSymbol.Type => throw new NotImplementedException();

            bool IFieldSymbol.HasConstantValue => throw new NotImplementedException();

            object IFieldSymbol.ConstantValue => throw new NotImplementedException();

            ImmutableArray<CustomModifier> IFieldSymbol.CustomModifiers => throw new NotImplementedException();

            IFieldSymbol IFieldSymbol.OriginalDefinition => throw new NotImplementedException();

            IFieldSymbol IFieldSymbol.CorrespondingTupleField => throw new NotImplementedException();
            #endregion unimplemented members
        }

        public class ArrayType : TypeSymbol, IArrayTypeSymbol
        {
            private INamedTypeSymbol _elementType;

            public ArrayType(INamedTypeSymbol elementType) : base(null, null, Accessibility.NotApplicable)
            {
                this._elementType = elementType;
            }

            SymbolKind ISymbol.Kind => SymbolKind.ArrayType;

            ITypeSymbol IArrayTypeSymbol.ElementType => _elementType;

            #region unimplemented members
            int IArrayTypeSymbol.Rank => throw new NotImplementedException();

            bool IArrayTypeSymbol.IsSZArray => throw new NotImplementedException();

            ImmutableArray<int> IArrayTypeSymbol.LowerBounds => throw new NotImplementedException();

            ImmutableArray<int> IArrayTypeSymbol.Sizes => throw new NotImplementedException();

            ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers => throw new NotImplementedException();

            bool IArrayTypeSymbol.Equals(IArrayTypeSymbol other)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class GenericNamedTypeSymbol : NamedTypeSymbol, INamedTypeSymbol
        {
            private readonly int _arity;
            private readonly ImmutableArray<ITypeParameterSymbol> _typeParameters;

            public GenericNamedTypeSymbol(string name, int arity, ISymbol containingSymbol, Accessibility accessibility, INamedTypeSymbol baseType) : base(name, containingSymbol, accessibility, baseType)
            {
                this._arity = arity;
                this._typeParameters = MakeTypeParameters();
                INamedTypeSymbol i;
            }

            private ImmutableArray<ITypeParameterSymbol> MakeTypeParameters()
            {
                var builder = PooledObjects.ArrayBuilder<ITypeParameterSymbol>.GetInstance();
                for (int i = 0; i < _arity; i++)
                {
                    builder.Add(new TypeParameterSymbol(this, i));
                }

                return builder.ToImmutableAndFree();
            }

            int INamedTypeSymbol.Arity => _arity;

            bool ISymbol.IsDefinition => true;

            ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments => _typeParameters.Cast<ITypeParameterSymbol, ITypeSymbol>();

            ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters => _typeParameters;

            // A helper method just for the benefit of the tests.
            public INamedTypeSymbol Construct(ITypeSymbol[] typeArguments, INamedTypeSymbol substitutedBase)
            {
                Assert.Equal(typeArguments.Length, _arity);
                return new ConstructedNamedTypeSymbol(this, _typeParameters, typeArguments, substitutedBase);
            }
        }

        public class TypeParameterSymbol : Symbol, ITypeParameterSymbol
        {
            private readonly int _ordinal;

            public TypeParameterSymbol(ISymbol containingSymbol, int ordinal) : base("T"+(ordinal+1), containingSymbol, Accessibility.NotApplicable)
            {
                this._ordinal = ordinal;
            }

            #region unimplemented members
            int ITypeParameterSymbol.Ordinal => throw new NotImplementedException();

            VarianceKind ITypeParameterSymbol.Variance => throw new NotImplementedException();

            TypeParameterKind ITypeParameterSymbol.TypeParameterKind => throw new NotImplementedException();

            IMethodSymbol ITypeParameterSymbol.DeclaringMethod => throw new NotImplementedException();

            INamedTypeSymbol ITypeParameterSymbol.DeclaringType => throw new NotImplementedException();

            bool ITypeParameterSymbol.HasReferenceTypeConstraint => throw new NotImplementedException();

            bool ITypeParameterSymbol.HasValueTypeConstraint => throw new NotImplementedException();

            bool ITypeParameterSymbol.HasUnmanagedTypeConstraint => throw new NotImplementedException();

            bool ITypeParameterSymbol.HasConstructorConstraint => throw new NotImplementedException();

            ImmutableArray<ITypeSymbol> ITypeParameterSymbol.ConstraintTypes => throw new NotImplementedException();

            ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition => throw new NotImplementedException();

            ITypeSymbol ITypeSymbol.OriginalDefinition => throw new NotImplementedException();

            ITypeParameterSymbol ITypeParameterSymbol.ReducedFrom => throw new NotImplementedException();

            TypeKind ITypeSymbol.TypeKind => throw new NotImplementedException();

            INamedTypeSymbol ITypeSymbol.BaseType => throw new NotImplementedException();

            ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces => throw new NotImplementedException();

            ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces => throw new NotImplementedException();

            bool ITypeSymbol.IsReferenceType => throw new NotImplementedException();

            bool ITypeSymbol.IsValueType => throw new NotImplementedException();

            bool ITypeSymbol.IsAnonymousType => throw new NotImplementedException();

            bool ITypeSymbol.IsTupleType => throw new NotImplementedException();

            SpecialType ITypeSymbol.SpecialType => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsNamespace => throw new NotImplementedException();

            bool INamespaceOrTypeSymbol.IsType => throw new NotImplementedException();

            ISymbol ITypeSymbol.FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
            {
                throw new NotImplementedException();
            }

            ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }

        public class ConstructedNamedTypeSymbol : NamedTypeSymbol, INamedTypeSymbol
        {
            private readonly INamedTypeSymbol _constructedFrom;
            private readonly int _arity;
            private readonly ImmutableArray<ITypeSymbol> _typeArguments;
            private readonly ImmutableArray<ITypeParameterSymbol> _typeParameters;

            public ConstructedNamedTypeSymbol(INamedTypeSymbol constructedFrom, ImmutableArray<ITypeParameterSymbol> typeParameters, ITypeSymbol[] typeArguments, INamedTypeSymbol substitutedBase) : base(constructedFrom.Name, constructedFrom.ContainingSymbol, constructedFrom.DeclaredAccessibility, substitutedBase)
            {
                this._constructedFrom = constructedFrom;
                this._arity = constructedFrom.Arity;
                this._typeParameters = typeParameters;
                this._typeArguments = typeArguments.ToImmutableArray();
            }

            ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments => _typeArguments;

            ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters => _typeParameters;

            int INamedTypeSymbol.Arity => _arity;

            bool ISymbol.IsDefinition => false;

            ITypeSymbol ITypeSymbol.OriginalDefinition => _constructedFrom.OriginalDefinition;
        }

        public class AliasSymbol : Symbol, IAliasSymbol
        {
            private readonly INamespaceOrTypeSymbol _target;

            public AliasSymbol(string name, INamespaceOrTypeSymbol target) : base(name, null, Accessibility.NotApplicable)
            {
                this._target = target;
            }

            INamespaceOrTypeSymbol IAliasSymbol.Target => _target;

            SymbolKind ISymbol.Kind => SymbolKind.Alias;
        }

        public class BuiltinOperatorSymbol : Symbol, IMethodSymbol
        {
            public BuiltinOperatorSymbol() : base(null, null, Accessibility.NotApplicable)
            {
            }

            SymbolKind ISymbol.Kind => SymbolKind.Method;

            MethodKind IMethodSymbol.MethodKind => MethodKind.BuiltinOperator;

            #region unimplemented members
            int IMethodSymbol.Arity => throw new NotImplementedException();

            bool IMethodSymbol.IsGenericMethod => throw new NotImplementedException();

            bool IMethodSymbol.IsExtensionMethod => throw new NotImplementedException();

            bool IMethodSymbol.IsAsync => throw new NotImplementedException();

            bool IMethodSymbol.IsVararg => throw new NotImplementedException();

            bool IMethodSymbol.IsCheckedBuiltin => throw new NotImplementedException();

            bool IMethodSymbol.HidesBaseMethodsByName => throw new NotImplementedException();

            bool IMethodSymbol.ReturnsVoid => throw new NotImplementedException();

            bool IMethodSymbol.ReturnsByRef => throw new NotImplementedException();

            bool IMethodSymbol.ReturnsByRefReadonly => throw new NotImplementedException();

            RefKind IMethodSymbol.RefKind => throw new NotImplementedException();

            ITypeSymbol IMethodSymbol.ReturnType => throw new NotImplementedException();

            ImmutableArray<ITypeSymbol> IMethodSymbol.TypeArguments => throw new NotImplementedException();

            ImmutableArray<ITypeParameterSymbol> IMethodSymbol.TypeParameters => throw new NotImplementedException();

            ImmutableArray<IParameterSymbol> IMethodSymbol.Parameters => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.ConstructedFrom => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.OriginalDefinition => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.OverriddenMethod => throw new NotImplementedException();

            ITypeSymbol IMethodSymbol.ReceiverType => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.ReducedFrom => throw new NotImplementedException();

            ImmutableArray<IMethodSymbol> IMethodSymbol.ExplicitInterfaceImplementations => throw new NotImplementedException();

            ImmutableArray<CustomModifier> IMethodSymbol.ReturnTypeCustomModifiers => throw new NotImplementedException();

            ImmutableArray<CustomModifier> IMethodSymbol.RefCustomModifiers => throw new NotImplementedException();

            ISymbol IMethodSymbol.AssociatedSymbol => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.PartialDefinitionPart => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.PartialImplementationPart => throw new NotImplementedException();

            INamedTypeSymbol IMethodSymbol.AssociatedAnonymousDelegate => throw new NotImplementedException();

            IMethodSymbol IMethodSymbol.Construct(params ITypeSymbol[] typeArguments)
            {
                throw new NotImplementedException();
            }

            DllImportData IMethodSymbol.GetDllImportData()
            {
                throw new NotImplementedException();
            }

            ImmutableArray<AttributeData> IMethodSymbol.GetReturnTypeAttributes()
            {
                throw new NotImplementedException();
            }

            ITypeSymbol IMethodSymbol.GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
            {
                throw new NotImplementedException();
            }

            IMethodSymbol IMethodSymbol.ReduceExtensionMethod(ITypeSymbol receiverType)
            {
                throw new NotImplementedException();
            }
            #endregion unimplemented members
        }
    }
}
