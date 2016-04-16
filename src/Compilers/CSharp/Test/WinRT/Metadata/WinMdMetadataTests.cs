// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;


using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Unit tests for programs that use the Windows.winmd file.
    // 
    // Checks to see that types are forwarded correctly, that 
    // metadata files are loaded as they should, etc.
    public class WinMdMetadataTests : CSharpTestBase
    {
        /// <summary>
        /// Make sure that the members of a function are forwarded to their appropriate types.
        /// We do this by checking that the first parameter of
        /// Windows.UI.Text.ITextRange.SetPoint(Point p...) gets forwarded to the 
        /// System.Runtime.WindowsRuntime assembly.
        /// </summary> 
        [Fact]
        public void FunctionPrototypeForwarded()
        {
            var text = "public class A{};";
            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);

            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            wns1 = wns1.GetMember<NamespaceSymbol>("UI");
            wns1 = wns1.GetMember<NamespaceSymbol>("Text");
            var itextrange = wns1.GetMember<PENamedTypeSymbol>("ITextRange");
            var func = itextrange.GetMember<PEMethodSymbol>("SetPoint");
            var pt = ((PEParameterSymbol)(func.Parameters[0])).Type as PENamedTypeSymbol;
            Assert.Equal(pt.ContainingAssembly.Name, "System.Runtime.WindowsRuntime");
        }

        /// <summary>
        /// Make sure that a delegate defined in Windows.winmd has a public constructor
        /// (by default, all delegates in Windows.winmd have a private constructor).
        /// </summary> 
        [Fact]
        public void DelegateConstructorMarkedPublic()
        {
            var text = "public class A{};";
            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);

            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            wns1 = wns1.GetMember<NamespaceSymbol>("UI");
            wns1 = wns1.GetMember<NamespaceSymbol>("Xaml");
            var itextrange = wns1.GetMember<PENamedTypeSymbol>("SuspendingEventHandler");
            var func = itextrange.GetMember<PEMethodSymbol>(".ctor");
            Assert.Equal(func.DeclaredAccessibility, Accessibility.Public);
        }

        /// <summary>
        /// Verify that Windows.Foundation.Uri forwards successfully
        /// to System.Uri
        /// </summary>
        [Fact]
        public void TypeForwardingRenaming()
        {
            var text = "public class A{};";
            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);

            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            wns1 = wns1.GetMember<NamespaceSymbol>("Foundation");
            var iref = wns1.GetMember<PENamedTypeSymbol>("IUriRuntimeClass");
            var func = iref.GetMember<PEMethodSymbol>("CombineUri");
            var ret = func.ReturnType;
            Assert.Equal(func.ReturnType.ToTestDisplayString(), "System.Uri");
        }

        /// <summary>
        /// Verify that WinMd types are marked as private so that the
        /// C# developer cannot instantiate them.
        /// </summary>
        [Fact]
        public void WinMdTypesDefPrivate()
        {
            var text = "public class A{};";
            var comp = CreateWinRtCompilation(text);
            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);
            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");

            var wns2 = wns1.GetMember<NamespaceSymbol>("Foundation");
            var clas = wns2.GetMember<PENamedTypeSymbol>("Point");
            Assert.Equal(clas.DeclaredAccessibility, Accessibility.Internal);
        }

        /// <summary>
        /// Verify that Windows.UI.Colors.Black is forwarded to the
        /// System.Runtime.WindowsRuntime.dll assembly.
        /// </summary>
        [Fact]
        public void WinMdColorType()
        {
            var text = "public class A{};";

            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);
            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            var wns2 = wns1.GetMember<NamespaceSymbol>("UI");
            var clas = wns2.GetMember<TypeSymbol>("Colors");
            var blk = clas.GetMembers("Black").Single();
            //The windows.winmd module points to a Windows.UI.Color which should be modified to belong
            //to System.Runtime.WindowsRuntime
            Assert.Equal(((Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PENamedTypeSymbol)
                ((((Microsoft.CodeAnalysis.CSharp.Symbols.PropertySymbol)(blk)).GetMethod).ReturnType)).ContainingModule.ToString(),
                   "System.Runtime.WindowsRuntime.dll");
        }

        /// <summary>
        /// Ensure that a simple program that uses projected types can compile
        /// and run.
        /// </summary>
        [ConditionalFact(typeof(OSVersionWin8))]
        public void WinMdColorTest()
        {
            var text = @"using Windows.UI;
                             using Windows.Foundation;
                         
                             public class A{
                                public static void Main(){
                                
                                    var a = Colors.Black;
                                    System.Console.WriteLine(a.ToString());
                                }
                             };";

            CompileAndVerify(text, WinRtRefs, expectedOutput: "#FF000000");
        }

        /// <summary>
        /// Test that the metadata adapter correctly projects IReference to INullable
        /// </summary>
        [Fact]
        public void IReferenceToINullableType()
        {
            var text = "public class A{};";

            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);
            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            var wns2 = wns1.GetMember<NamespaceSymbol>("Globalization");
            var wns3 = wns2.GetMember<NamespaceSymbol>("NumberFormatting");
            var clas = wns3.GetMember<TypeSymbol>("DecimalFormatter");
            var puint = clas.GetMembers("ParseUInt").Single();

            // The return type of ParseUInt should be Nullable<ulong>, not IReference<ulong>
            Assert.Equal("ulong?",
                ((Microsoft.CodeAnalysis.CSharp.Symbols.ConstructedNamedTypeSymbol)
                (((Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol)puint).ReturnType)).ToString());
        }

        /// <summary>
        /// Test that a program projects IReference to INullable.
        /// </summary>
        [Fact]
        public void WinMdIReferenceINullableTest()
        {
            var source =
@"using System;
using Windows.Globalization.NumberFormatting;

public class C
{
    public static void Main(string[] args)
    {
        var format = new DecimalFormatter();
        ulong result = format.ParseUInt(""10"") ?? 0;
        Console.WriteLine(result);
        result = format.ParseUInt(""-1"") ?? 0;
        Console.WriteLine(result);
    }
}";
            var verifier = CompileAndVerifyOnWin8Only(source,
                expectedOutput: "10\r\n0");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(1169511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1169511")]
        public void WinMdAssemblyQualifiedType()
        {
            var source =
@"using System;

[MyAttribute(typeof(C1))]
public class C
{
    public static void Main(string[] args)
    {
    }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(System.Type type)
    {
    }
}
";
            CompileAndVerify(
                source,
                WinRtRefs.Concat(new[] { AssemblyMetadata.CreateFromImage(TestResources.WinRt.W1).GetReference() }),
                symbolValidator: m =>
            {
                var module = (PEModuleSymbol)m;
                var c = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMember("C");
                var attributeHandle = module.Module.MetadataReader.GetCustomAttributes(c.Handle).Single();
                string value;
                module.Module.TryExtractStringValueFromAttribute(attributeHandle, out value);

                Assert.Equal("C1, W, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime", value);
            });
        }
    }
}
