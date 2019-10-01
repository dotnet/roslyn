// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseSystemHashCode;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement
{
    public partial class UseSystemHashCodeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseSystemHashCodeDiagnosticAnalyzer(), new UseSystemHashCodeCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithFieldWithBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + j.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode(), j);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithFieldWithNoBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + j.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(j);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithNoFieldWithBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode());
    }
}");
        }
    }
}
