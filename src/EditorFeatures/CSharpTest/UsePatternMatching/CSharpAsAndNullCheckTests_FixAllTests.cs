// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    public partial class CSharpAsAndNullCheckTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int M()
    {
        string a;
        {|FixAllInDocument:var|} x = o as string;
        if (x != null)
        {
        }

        var y = o as string;
        if (y != null)
        {
        }

        if ((a = o as string) == null)
        {
        }

        var c = o as string;
        var d = c != null ? 1 : 0;

        var e = o as string;
        return e != null ? 1 : 0;
    }
}",
@"class C
{
    int M()
    {
        if (o is string x)
        {
        }

        if (o is string y)
        {
        }

        if (!(o is string a))
        {
        }

        var d = o is string c ? 1 : 0;

        return o is string e ? 1 : 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task FixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
@"class Symbol
{
    public ContainingSymbol { get; }

    void M(object o, bool b0, bool b1)
    {
        {|FixAllInDocument:var|} symbol = o as Symbol;
        if (symbol != null)
        {
            while ((object)symbol != null && b1)
            {
                symbol = symbol.ContainingSymbol as Symbol;
            }

            if ((object)symbol == null || b2)
            {
                throw null;
            }

            var use = symbol;
        }
    }
}",
@"class Symbol
{
    public ContainingSymbol { get; }

    void M(object o, bool b0, bool b1)
    {
    if (o is Symbol symbol)
    {
        while ((object)symbol != null && b1)
        {
            symbol = symbol.ContainingSymbol as Symbol;
        }

        if ((object)symbol == null || b2)
        {
            throw null;
        }

        var use = symbol;
    }
}
}");
        }

        [WorkItem(26679, "https://github.com/dotnet/roslyn/issues/26679")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task FixAllInDocument3()
        {
            await TestInRegularAndScriptAsync(
@"class Test
{
    void M()
    {
        {|FixAllInDocument:IMethodSymbol|} methodSymbol;
        IPropertySymbol propertySymbol;
        IEventSymbol eventSymbol;
        bool isImplementingExplicitly;

        // Only methods, properties and events can implement an interface member
        if ((methodSymbol = memberSymbol as IMethodSymbol) != null)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = methodSymbol.ExplicitInterfaceImplementations.Any();
        }
        else if ((propertySymbol = memberSymbol as IPropertySymbol) != null)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = propertySymbol.ExplicitInterfaceImplementations.Any();
        }
        else if ((eventSymbol = memberSymbol as IEventSymbol) != null)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = eventSymbol.ExplicitInterfaceImplementations.Any();
        }
        else
        {
            return false;
        }
    }
}",
@"class Test
{
    void M()
    {
        bool isImplementingExplicitly;

        // Only methods, properties and events can implement an interface member
        if (memberSymbol is IMethodSymbol methodSymbol)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = methodSymbol.ExplicitInterfaceImplementations.Any();
        }
        else if (memberSymbol is IPropertySymbol propertySymbol)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = propertySymbol.ExplicitInterfaceImplementations.Any();
        }
        else if (memberSymbol is IEventSymbol eventSymbol)
        {
            // Check if the member is implementing an interface explicitly
            isImplementingExplicitly = eventSymbol.ExplicitInterfaceImplementations.Any();
        }
        else
        {
            return false;
        }
    }
}");
        }

        [WorkItem(26680, "https://github.com/dotnet/roslyn/issues/26680")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
        public async Task FixAllInDocument4()
        {
            await TestInRegularAndScriptAsync(
@"class Test
{
    void M()
    {
        {|FixAllInDocument:var|} firstTextPartSyntax = summaryElement.Content[0] as XmlTextSyntax;
        var classReferencePart = summaryElement.Content[1] as XmlEmptyElementSyntax;
        var secondTextPartSyntax = summaryElement.Content[2] as XmlTextSyntax;

        if (firstTextPartSyntax != null && classReferencePart != null && secondTextPartSyntax != null)
        {
        }
    }
}",
@"class Test
{
    void M()
    {
        if (summaryElement.Content[0] is XmlTextSyntax firstTextPartSyntax && summaryElement.Content[1] is XmlEmptyElementSyntax classReferencePart && summaryElement.Content[2] is XmlTextSyntax secondTextPartSyntax)
        {
        }
    }
}");
        }
    }
}
