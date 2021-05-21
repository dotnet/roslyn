// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class TypeAccessibility : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(Net40.mscorlib);

            TestTypeAccessibilityHelper(assembly.Modules[0]);
        }

        private void TestTypeAccessibilityHelper(ModuleSymbol module0)
        {
            var system = (from n in module0.GlobalNamespace.GetMembers()
                          where n.Kind == SymbolKind.Namespace && n.Name.Equals("System")
                          select n).Cast<NamespaceSymbol>().Single();

            var obj = (from t in system.GetTypeMembers()
                       where t.Name.Equals("Object")
                       select t).Single();

            Assert.Equal(Accessibility.Public, obj.DeclaredAccessibility);

            var frameworkAssembly = (from t in module0.GlobalNamespace.GetTypeMembers()
                                     where t.Name.Equals("FXAssembly")
                                     select t).Single();

            Assert.Equal(Accessibility.Internal, frameworkAssembly.DeclaredAccessibility);

            var @enum = (from t in system.GetTypeMembers()
                         where t.Name.Equals("Enum")
                         select t).Single();

            var console = (from t in system.GetTypeMembers()
                           where t.Name.Equals("Console")
                           select t).Single();

            var controlKeyState = (from t in console.GetTypeMembers()
                                   where t.Name.Equals("ControlKeyState")
                                   select t).Single();

            Assert.Equal(Accessibility.Internal, controlKeyState.DeclaredAccessibility);

            var activationContext = (from t in system.GetTypeMembers()
                                     where t.Name.Equals("ActivationContext")
                                     select t).Single();

            var contextForm = (from t in activationContext.GetTypeMembers()
                               where t.Name.Equals("ContextForm")
                               select t).Single();

            Assert.Equal(Accessibility.Public, contextForm.DeclaredAccessibility);

            var runtime = (from t in system.GetMembers()
                           where t.Kind == SymbolKind.Namespace && t.Name.Equals("Runtime")
                           select t).Cast<NamespaceSymbol>().Single();

            var remoting = (from t in runtime.GetMembers()
                            where t.Kind == SymbolKind.Namespace && t.Name.Equals("Remoting")
                            select t).Cast<NamespaceSymbol>().Single();

            var messaging = (from t in remoting.GetMembers()
                             where t.Kind == SymbolKind.Namespace && t.Name.Equals("Messaging")
                             select t).Cast<NamespaceSymbol>().Single();

            var messageSmuggler = (from t in messaging.GetTypeMembers()
                                   where t.Name.Equals("MessageSmuggler")
                                   select t).Single();

            var serializedArg = (from t in messageSmuggler.GetTypeMembers()
                                 where t.Name.Equals("SerializedArg")
                                 select t).Single();

            Assert.Equal(Accessibility.Protected, serializedArg.DeclaredAccessibility);

            var security = (from t in system.GetMembers()
                            where t.Kind == SymbolKind.Namespace && t.Name.Equals("Security")
                            select t).Cast<NamespaceSymbol>().Single();

            var accessControl = (from t in security.GetMembers()
                                 where t.Kind == SymbolKind.Namespace && t.Name.Equals("AccessControl")
                                 select t).Cast<NamespaceSymbol>().Single();

            var nativeObjectSecurity = (from t in accessControl.GetTypeMembers()
                                        where t.Name.Equals("NativeObjectSecurity")
                                        select t).Single();

            var exceptionFromErrorCode = (from t in nativeObjectSecurity.GetTypeMembers()
                                          where t.Name.Equals("ExceptionFromErrorCode")
                                          select t).Single();

            Assert.Equal(Accessibility.ProtectedOrInternal, exceptionFromErrorCode.DeclaredAccessibility);

            Assert.Same(module0, module0.GlobalNamespace.Locations.Single().MetadataModuleInternal);
            Assert.Same(module0, system.Locations.Single().MetadataModuleInternal);
            Assert.Same(module0, runtime.Locations.Single().MetadataModuleInternal);
            Assert.Same(module0, obj.Locations.Single().MetadataModuleInternal);
        }
    }
}
