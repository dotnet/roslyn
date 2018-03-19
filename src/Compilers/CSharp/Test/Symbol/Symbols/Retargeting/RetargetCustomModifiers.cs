﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Retargeting
{
    public class RetargetCustomModifiers : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            PortableExecutableReference oldMsCorLib = TestReferences.NetFx.v4_0_21006.mscorlib;
            MetadataReference newMsCorLib = MscorlibRef;

            var c1 = CSharpCompilation.Create("C1", references: new[]
            {
                oldMsCorLib,
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.netmodule
            });

            AssemblySymbol c1Assembly = c1.Assembly;

            CSharpCompilation c2 = CSharpCompilation.Create("C2", references: new MetadataReference[] { newMsCorLib, new CSharpCompilationReference(c1) });

            AssemblySymbol mscorlibAssembly = c2.GetReferencedAssemblySymbol(newMsCorLib);

            Assert.NotSame(mscorlibAssembly, c1.GetReferencedAssemblySymbol(oldMsCorLib));

            NamedTypeSymbol modifiers = c2.GlobalNamespace.GetTypeMembers("Modifiers").Single();

            Assert.IsAssignableFrom<PENamedTypeSymbol>(modifiers);

            FieldSymbol f0 = modifiers.GetMembers("F0").OfType<FieldSymbol>().Single();

            Assert.Equal(1, f0.CustomModifiers.Length);

            CustomModifier f0Mod = f0.CustomModifiers[0];

            Assert.True(f0Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", f0Mod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, f0Mod.Modifier.ContainingAssembly);

            MethodSymbol m1 = modifiers.GetMembers("F1").OfType<MethodSymbol>().Single();
            ParameterSymbol p1 = m1.Parameters[0];
            ParameterSymbol p2 = modifiers.GetMembers("F2").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m5 = modifiers.GetMembers("F5").OfType<MethodSymbol>().Single();
            ParameterSymbol p5 = m5.Parameters[0];

            ParameterSymbol p6 = modifiers.GetMembers("F6").OfType<MethodSymbol>().Single().Parameters[0];

            MethodSymbol m7 = modifiers.GetMembers("F7").OfType<MethodSymbol>().Single();

            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length);

            Assert.Equal(1, p1.CustomModifiers.Length);

            CustomModifier p1Mod = p1.CustomModifiers[0];

            Assert.True(p1Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p1Mod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, p1Mod.Modifier.ContainingAssembly);

            Assert.Equal(2, p2.CustomModifiers.Length);

            foreach (CustomModifier p2Mod in p2.CustomModifiers)
            {
                Assert.True(p2Mod.IsOptional);
                Assert.Equal("System.Runtime.CompilerServices.IsConst", p2Mod.Modifier.ToTestDisplayString());
                Assert.Same(mscorlibAssembly, p2Mod.Modifier.ContainingAssembly);
            }

            Assert.True(m5.ReturnsVoid);
            Assert.Equal(1, m5.ReturnTypeCustomModifiers.Length);

            CustomModifier m5Mod = m5.ReturnTypeCustomModifiers[0];
            Assert.True(m5Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m5Mod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, m5Mod.Modifier.ContainingAssembly);

            Assert.Equal(0, p5.CustomModifiers.Length);

            ArrayTypeSymbol p5Type = (ArrayTypeSymbol)p5.Type;

            Assert.Equal("System.Int32", p5Type.ElementType.ToTestDisplayString());

            Assert.Equal(1, p5Type.CustomModifiers.Length);
            CustomModifier p5TypeMod = p5Type.CustomModifiers[0];

            Assert.True(p5TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p5TypeMod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, p5TypeMod.Modifier.ContainingAssembly);

            Assert.Equal(0, p6.CustomModifiers.Length);

            PointerTypeSymbol p6Type = (PointerTypeSymbol)p6.Type;

            Assert.Equal("System.Int32", p6Type.PointedAtType.ToTestDisplayString());

            Assert.Equal(1, p6Type.CustomModifiers.Length);
            CustomModifier p6TypeMod = p6Type.CustomModifiers[0];

            Assert.True(p6TypeMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", p6TypeMod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, p6TypeMod.Modifier.ContainingAssembly);

            Assert.False(m7.ReturnsVoid);
            Assert.Equal(1, m7.ReturnTypeCustomModifiers.Length);

            CustomModifier m7Mod = m7.ReturnTypeCustomModifiers[0];
            Assert.True(m7Mod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsConst", m7Mod.Modifier.ToTestDisplayString());
            Assert.Same(mscorlibAssembly, m7Mod.Modifier.ContainingAssembly);
        }

        [Fact]
        public void Test2()
        {
            PortableExecutableReference oldMsCorLib = TestReferences.NetFx.v4_0_21006.mscorlib;
            MetadataReference newMsCorLib = MscorlibRef;

            var source = @"
public class Modifiers
{
    public volatile int volatileFld;

    void F1(System.DateTime* p)
    {
    }
}
";

            CSharpCompilation c1 = CSharpCompilation.Create("C1", new[] { Parse(source) }, new[] { oldMsCorLib });

            AssemblySymbol c1Assembly = c1.Assembly;

            var r1 = new CSharpCompilationReference(c1);
            CSharpCompilation c2 = CSharpCompilation.Create("C2", references: new[] { newMsCorLib, r1 });
            AssemblySymbol c1AsmRef = c2.GetReferencedAssemblySymbol(r1);

            Assert.NotSame(c1Assembly, c1AsmRef);

            AssemblySymbol mscorlibAssembly = c2.GetReferencedAssemblySymbol(newMsCorLib);

            Assert.NotSame(mscorlibAssembly, c1.GetReferencedAssemblySymbol(oldMsCorLib));

            NamedTypeSymbol modifiers = c2.GlobalNamespace.GetTypeMembers("Modifiers").Single();

            Assert.IsType<RetargetingNamedTypeSymbol>(modifiers);

            FieldSymbol volatileFld = modifiers.GetMembers("volatileFld").OfType<FieldSymbol>().Single();

            Assert.Equal(1, volatileFld.CustomModifiers.Length);

            CustomModifier volatileFldMod = volatileFld.CustomModifiers[0];

            Assert.False(volatileFldMod.IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsVolatile", volatileFldMod.Modifier.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Int32, volatileFld.Type.SpecialType);
            Assert.Same(mscorlibAssembly, volatileFldMod.Modifier.ContainingAssembly);

            Assert.Equal("volatileFld", volatileFld.Name);
            Assert.True(volatileFld.IsVolatile);
            Assert.Same(volatileFld, volatileFld.OriginalDefinition);
            Assert.Null(volatileFld.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false));
            Assert.Null(volatileFld.ConstantValue);
            Assert.Null(volatileFld.AssociatedSymbol);
            Assert.Same(c1AsmRef, volatileFld.ContainingAssembly);
            Assert.Same(c1AsmRef.Modules[0], volatileFld.ContainingModule);
            Assert.Same(modifiers, volatileFld.ContainingSymbol);
            Assert.Equal(Accessibility.Public, volatileFld.DeclaredAccessibility);
            Assert.False(volatileFld.IsConst);
            Assert.False(volatileFld.IsReadOnly);
            Assert.False(volatileFld.IsStatic);
            Assert.Same(volatileFld.ContainingModule, ((RetargetingFieldSymbol)volatileFld).RetargetingModule);
            Assert.Same(c1Assembly, ((RetargetingFieldSymbol)volatileFld).UnderlyingField.ContainingAssembly);

            MethodSymbol m1 = modifiers.GetMembers("F1").OfType<MethodSymbol>().Single();

            Assert.Equal(0, m1.ReturnTypeCustomModifiers.Length);
            Assert.True(!m1.ExplicitInterfaceImplementations.IsDefault);
            Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length);
            Assert.False(m1.HidesBaseMethodsByName);
            Assert.False(m1.IsExtensionMethod);
            Assert.Equal(((RetargetingMethodSymbol)m1).UnderlyingMethod.CallingConvention, m1.CallingConvention);
            Assert.Null(m1.AssociatedSymbol);
            Assert.Same(c1AsmRef.Modules[0], m1.ContainingModule);

            ParameterSymbol p1 = m1.Parameters[0];

            Assert.Equal(0, p1.CustomModifiers.Length);
            Assert.Same(c1AsmRef.Modules[0], p1.ContainingModule);
            Assert.False(p1.HasExplicitDefaultValue, "Parameter has default value");
            Assert.Equal(0, p1.Ordinal);

            //PointerTypeSymbol p1Type = (PointerTypeSymbol)p1.Type;

            //Assert.Same(mscorlibAssembly, p1Type.ContainingAssembly);
            //Assert.Equal(SpecialType.System_DateTime, p1Type.PointedAtType.SpecialType);
            //Assert.Equal(0, p1Type.CustomModifiers.Count);
        }
    }
}
