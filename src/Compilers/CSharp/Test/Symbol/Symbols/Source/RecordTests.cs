﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Assert.Equal(c.TypeParameters[0], t.Type.TypeSymbol);
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

            var eqs = c.GetMethod("Equals");
            var param = Assert.Single(eqs.Parameters);
            Assert.False(param.Type.IsNull);
            Assert.Equal(c, param.Type.TypeSymbol);
        }
    }
}
