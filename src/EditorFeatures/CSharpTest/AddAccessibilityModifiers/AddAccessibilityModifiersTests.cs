﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAccessibilityModifiers
{
    public partial class AddAccessibilityModifiersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddAccessibilityModifiersDiagnosticAnalyzer(), new CSharpAddAccessibilityModifiersCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
        public async Task TestAllConstructs()
        {
            await TestInRegularAndScriptAsync(
@"
namespace Outer
{
    namespace Inner1.Inner2
    {
        class {|FixAllInDocument:C|}
        {
            class NestedClass { }

            struct NestedStruct { }

            int f1;
            int f2, f3;
            public int f4;

            event Action e1, e2;
            public event Action e3;

            event Action e4 { add { } remove { } }
            public event Action e5 { add { } remove { } }
            event Action I.e6 { add { } remote { } }

            static C() { }
            C() { }
            public C(int i) { }

            ~C() { }

            void M1() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            int P1 { get; }
            public int P2 { get; }
            int I.P3 { get; }

            int this[int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
        }

        interface I
        {
            event Action e6;
            void M3();
            int P3 { get; }
            int this[bool b] { get; }
        }

        delegate void D();

        enum E
        {
            EMember
        }
    }
}",
@"
namespace Outer
{
    namespace Inner1.Inner2
    {
        internal class {|FixAllInDocument:C|}
        {
            private class NestedClass { }

            private struct NestedStruct { }

            private int f1;
            private int f2, f3;
            public int f4;

            private event Action e1, e2;
            public event Action e3;

            private event Action e4 { add { } remove { } }
            public event Action e5 { add { } remove { } }
            event Action I.e6 { add { } remote { } }

            static C() { }
            private C() { }
            public C(int i) { }

            ~C() { }

            private void M1() { }
            public void M2() { }
            void I.M3() { }
            partial void M4() { }

            private int P1 { get; }
            public int P2 { get; }
            int I.P3 { get; }

            private int this[int i] { get; }
            public int this[string s] { get; }
            int I.this[bool b] { get; }
        }

        internal interface I
        {
            event Action e6;
            void M3();
            int P3 { get; }
            int this[bool b] { get; }
        }

        internal delegate void D();

        internal enum E
        {
            EMember
        }
    }
}");
        }
    }
}
