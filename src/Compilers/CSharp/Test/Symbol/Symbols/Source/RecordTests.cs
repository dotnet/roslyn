// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RecordTests : CSharpTestBase
    {
        [Fact]
        public void GeneratedConstructorClass()
        {
            var comp = CreateCompilation(@"class C(int x, string y);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctor = c.GetMethod(".ctor");
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var y = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, y.Type.SpecialType);
            Assert.Equal("y", y.Name);
        }

        [Fact]
        public void GeneratedConstructorDefaultValuesClass()
        {
            var comp = CreateCompilation(@"class C<T>(int x, T t = default);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");
            Assert.Equal(1, c.Arity);
            var ctor = c.GetMethod(".ctor");
            Assert.Equal(0, ctor.Arity);
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var t = ctor.Parameters[1];
            Assert.Equal(c.TypeParameters[0], t.Type);
            Assert.Equal("t", t.Name);
        }

        [Fact]
        public void GeneratedPropertiesClass()
        {
            var comp = CreateCompilation("class C(int x, int y);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var x = c.GetProperty("x");
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.True(x.IsReadOnly);
            Assert.Equal(Accessibility.Public, x.DeclaredAccessibility);
            Assert.False(x.IsVirtual);
            Assert.False(x.IsStatic);

            var y = c.GetProperty("y");
            Assert.Equal(SpecialType.System_Int32, y.Type.SpecialType);
            Assert.True(y.IsReadOnly);
            Assert.Equal(Accessibility.Public, y.DeclaredAccessibility);
            Assert.False(x.IsVirtual);
            Assert.False(x.IsStatic);
        }

        [Fact]
        public void GeneratedExplicitEqualsClass()
        {
            var comp = CreateCompilation("class C(int x, int y);");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var equals = (MethodSymbol)Assert.Single(
                c.GetMembers("Equals"),
                s => ((MethodSymbol)s).Parameters[0].Type.SpecialType != SpecialType.System_Object);
            var param = Assert.Single(equals.Parameters);
            Assert.Equal(c, param.Type);
        }

        [Fact]
        public void GeneratedObjEqualsClass()
        {
            var comp = CreateCompilation("class C(int x, int y);");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var equals = (MethodSymbol)Assert.Single(
                c.GetMembers("Equals"),
                s => ((MethodSymbol)s).Parameters[0].Type.SpecialType == SpecialType.System_Object);
            Assert.True(equals.IsVirtual);
            Assert.True(equals.IsOverride);
            Assert.Equal(
                comp.GetSpecialTypeMember(SpecialMember.System_Object__Equals),
                equals.OverriddenMethod);
            var param = Assert.Single(equals.Parameters);
        }

        [Fact]
        public void GeneratedGetHashCodeClass()
        {
            var comp = CreateCompilation("class C(int x, int y);");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var hashcode = c.GetMethod("GetHashCode");
            Assert.True(hashcode.IsVirtual);
            Assert.True(hashcode.IsOverride);
            Assert.Equal(
                comp.GetSpecialTypeMember(SpecialMember.System_Object__GetHashCode),
                hashcode.OverriddenMethod);
            Assert.Empty(hashcode.Parameters);
            Assert.Equal(0, hashcode.ParameterCount);
            Assert.Equal(SpecialType.System_Int32, hashcode.ReturnType.SpecialType);
        }
    }
}
