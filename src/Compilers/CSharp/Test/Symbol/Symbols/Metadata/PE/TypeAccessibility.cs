﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class TypeAccessibility : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            AssemblySymbol assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);

            TestTypeAccessibilityHelper(assembly.Modules[0]);
        }

        private void TestTypeAccessibilityHelper(ModuleSymbol module0)
        {
            NamespaceSymbol system = (from n in module0.GlobalNamespace.GetMembers()
                          where n.Kind == SymbolKind.Namespace && n.Name.Equals("System")
                          select n).Cast<NamespaceSymbol>().Single();

            NamedTypeSymbol obj = (from t in system.GetTypeMembers()
                       where t.Name.Equals("Object")
                       select t).Single();

            Assert.Equal(Accessibility.Public, obj.DeclaredAccessibility);

            NamedTypeSymbol frameworkAssembly = (from t in module0.GlobalNamespace.GetTypeMembers()
                                     where t.Name.Equals("FXAssembly")
                                     select t).Single();

            Assert.Equal(Accessibility.Internal, frameworkAssembly.DeclaredAccessibility);

            NamedTypeSymbol @enum = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Enum")
                         select t).Single();

            NamedTypeSymbol console = (from t in system.GetTypeMembers()
                           where t.Name.Equals("Console")
                           select t).Single();

            NamedTypeSymbol controlKeyState = (from t in console.GetTypeMembers()
                                   where t.Name.Equals("ControlKeyState")
                                   select t).Single();

            Assert.Equal(Accessibility.Internal, controlKeyState.DeclaredAccessibility);

            NamedTypeSymbol activationContext = (from t in system.GetTypeMembers()
                                     where t.Name.Equals("ActivationContext")
                                     select t).Single();

            NamedTypeSymbol contextForm = (from t in activationContext.GetTypeMembers()
                               where t.Name.Equals("ContextForm")
                               select t).Single();

            Assert.Equal(Accessibility.Public, contextForm.DeclaredAccessibility);

            NamespaceSymbol runtime = (from t in system.GetMembers()
                           where t.Kind == SymbolKind.Namespace && t.Name.Equals("Runtime")
                           select t).Cast<NamespaceSymbol>().Single();

            NamespaceSymbol remoting = (from t in runtime.GetMembers()
                            where t.Kind == SymbolKind.Namespace && t.Name.Equals("Remoting")
                            select t).Cast<NamespaceSymbol>().Single();

            NamespaceSymbol messaging = (from t in remoting.GetMembers()
                             where t.Kind == SymbolKind.Namespace && t.Name.Equals("Messaging")
                             select t).Cast<NamespaceSymbol>().Single();

            NamedTypeSymbol messageSmuggler = (from t in messaging.GetTypeMembers()
                                   where t.Name.Equals("MessageSmuggler")
                                   select t).Single();

            NamedTypeSymbol serializedArg = (from t in messageSmuggler.GetTypeMembers()
                                 where t.Name.Equals("SerializedArg")
                                 select t).Single();

            Assert.Equal(Accessibility.Protected, serializedArg.DeclaredAccessibility);

            NamespaceSymbol security = (from t in system.GetMembers()
                            where t.Kind == SymbolKind.Namespace && t.Name.Equals("Security")
                            select t).Cast<NamespaceSymbol>().Single();

            NamespaceSymbol accessControl = (from t in security.GetMembers()
                                 where t.Kind == SymbolKind.Namespace && t.Name.Equals("AccessControl")
                                 select t).Cast<NamespaceSymbol>().Single();

            NamedTypeSymbol nativeObjectSecurity = (from t in accessControl.GetTypeMembers()
                                        where t.Name.Equals("NativeObjectSecurity")
                                        select t).Single();

            NamedTypeSymbol exceptionFromErrorCode = (from t in nativeObjectSecurity.GetTypeMembers()
                                          where t.Name.Equals("ExceptionFromErrorCode")
                                          select t).Single();

            Assert.Equal(Accessibility.ProtectedOrInternal, exceptionFromErrorCode.DeclaredAccessibility);

            Assert.Same(module0, module0.GlobalNamespace.Locations.Single().MetadataModule);
            Assert.Same(module0, system.Locations.Single().MetadataModule);
            Assert.Same(module0, runtime.Locations.Single().MetadataModule);
            Assert.Same(module0, obj.Locations.Single().MetadataModule);
        }
    }
}
