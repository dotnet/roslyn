// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IndexedProperties_BindingTests : SemanticModelTestBase
    {
        [ClrOnlyFact]
        public void OldGetFormat_IndexedProperties()
        {
            var reference = GetReference();
            var source =
@"
using System;
class B
{

    static void Main(string[] args)
    {
        IA a;
        a = new IA();
        int ret = /*<bind>*/a.get_P1/*</bind>*/(1);
    }
}
";
            IndexedPropertiesBindingChecks(source, reference, SymbolKind.Method, "get_P1");
        }

        [ClrOnlyFact]
        public void IndexedProperties_Complete()
        {
            var reference = GetReference();
            var source =
@"
using System;
class B
{

    static void Main(string[] args)
    {
        IA a;
        a = new IA();
        int ret = a./*<bind>*/P1/*</bind>*/[3]++;
    }
}
";
            IndexedPropertiesBindingChecks(source, reference, SymbolKind.Property, "P1");
        }

        [ClrOnlyFact]
        public void IndexedProperties_Incomplete()
        {
            var reference = GetReference();
            var source =
@"
using System;
class B
{

    static void Main(string[] args)
    {
        IA a;
        a = new IA();
        int ret = /*<bind>*/a.P1/*</bind>*/[3
    }
}
";
            IndexedPropertiesBindingChecks(source, reference, SymbolKind.Property, "P1");
        }

        [ClrOnlyFact]
        public void IndexedProperties_Set_In_Constructor()
        {
            var reference = GetReference();
            var source =
@"
using System;
class B
{

    static void Main(string[] args)
    {
        IA a;
        a = new IA(){/*<bind>*/P1/*</bind>*/ = 2};
    }
}
";
            IndexedPropertiesBindingChecks(source, reference, SymbolKind.Property, "P1");
        }

        [ClrOnlyFact]
        public void IndexedProperties_LINQ()
        {
            var reference = GetReference();
            var source = @"
using System;
using System.Linq;

class B
{
    static void Main(string[] args)
    {
        IA a;
        a = new IA();

        int[] arr = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        var query = from val in arr where val > /*<bind>*/a.P1/*</bind>*/[2] select val;

        foreach (var val in query) Console.WriteLine(val);
    }
}
";
            IndexedPropertiesBindingChecks(source, reference, SymbolKind.Property, "P1");
        }

        private void IndexedPropertiesBindingChecks(string source, MetadataReference reference, SymbolKind symbolKind, string name)
        {
            var tree = Parse(source);
            var comp = CreateCompilation(tree, new[] { reference });

            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var sym = model.GetSymbolInfo(expr);

            Assert.Equal(symbolKind, sym.Symbol.Kind);
            Assert.Equal(name, sym.Symbol.Name);

            var typeInfo = model.GetTypeInfo(expr);
            Assert.NotEqual(default, typeInfo);

            var methodGroup = model.GetMemberGroup(expr);
            Assert.NotEqual(default, methodGroup);

            var indexerGroup = model.GetIndexerGroup(expr);
            Assert.NotEqual(default, indexerGroup);

            var position = GetPositionForBinding(tree);

            // Get the list of LookupNames at the location at the end of the tag
            var actual_lookupNames = model.LookupNames(position);

            Assert.NotEmpty(actual_lookupNames);
            Assert.True(actual_lookupNames.Contains("System"), "LookupNames does not contain System");
            Assert.True(actual_lookupNames.Contains("Main"), "LookupNames does not contain Main");
            Assert.True(actual_lookupNames.Contains("IA"), "LookupNames does not contain IA");
            Assert.True(actual_lookupNames.Contains("A"), "LookupNames does not contain A");
            Assert.True(actual_lookupNames.Contains("a"), "LookupNames does not contain a");

            // Get the list of LookupSymbols at the location at the end of the tag
            var actual_lookupSymbols = model.LookupSymbols(position);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.NotEmpty(actual_lookupSymbols_as_string);
            Assert.True(actual_lookupSymbols_as_string.Contains("void B.Main(System.String[] args)"), "LookupSymbols does not contain Main");
            Assert.True(actual_lookupSymbols_as_string.Contains("System"), "LookupSymbols does not contain System");
            Assert.True(actual_lookupSymbols_as_string.Contains("IA"), "LookupSymbols does not contain IA");
            Assert.True(actual_lookupSymbols_as_string.Contains("A"), "LookupSymbols does not contain A");
        }

        private static MetadataReference GetReference()
        {
            var COMSource = @"
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
<CoClass(GetType(A))>
Public Interface IA
    Property P1(Optional index As Integer = 1) As Integer
End Interface
Public Class A
    Implements IA
    Property P1(Optional index As Integer = 1) As Integer Implements IA.P1
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class
";

            var reference = BasicCompilationUtils.CompileToMetadata(COMSource, verify: Verification.Passes);
            return reference;
        }
    }
}
