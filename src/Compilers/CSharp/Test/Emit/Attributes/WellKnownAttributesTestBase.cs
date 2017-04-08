// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class WellKnownAttributesTestBase : EmitMetadataTestBase
    {
        internal NamespaceSymbol Get_System_Runtime_InteropServices_NamespaceSymbol(ModuleSymbol m)
        {
            NamespaceSymbol sysNS = Get_System_NamespaceSymbol(m);
            return Get_System_Runtime_InteropServices_NamespaceSymbol(sysNS);
        }

        internal NamespaceSymbol Get_System_Runtime_InteropServices_WindowsRuntime_NamespaceSymbol(ModuleSymbol m)
        {
            NamespaceSymbol interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(m);
            return interopNS.GetMember<NamespaceSymbol>("WindowsRuntime");
        }

        internal NamespaceSymbol Get_System_Runtime_InteropServices_NamespaceSymbol(NamespaceSymbol systemNamespace)
        {
            var runtimeNS = systemNamespace.GetMember<NamespaceSymbol>("Runtime");
            return runtimeNS.GetMember<NamespaceSymbol>("InteropServices");
        }

        internal NamespaceSymbol Get_System_Runtime_CompilerServices_NamespaceSymbol(ModuleSymbol m)
        {
            NamespaceSymbol sysNS = Get_System_NamespaceSymbol(m);
            return Get_System_Runtime_CompilerServices_NamespaceSymbol(sysNS);
        }

        internal NamespaceSymbol Get_System_Runtime_CompilerServices_NamespaceSymbol(NamespaceSymbol systemNamespace)
        {
            var runtimeNS = systemNamespace.GetMember<NamespaceSymbol>("Runtime");
            return runtimeNS.GetMember<NamespaceSymbol>("CompilerServices");
        }

        internal NamespaceSymbol Get_System_Diagnostics_NamespaceSymbol(ModuleSymbol m)
        {
            NamespaceSymbol sysNS = Get_System_NamespaceSymbol(m);
            return sysNS.GetMember<NamespaceSymbol>("Diagnostics");
        }

        internal NamespaceSymbol Get_System_Security_NamespaceSymbol(ModuleSymbol m)
        {
            NamespaceSymbol sysNS = Get_System_NamespaceSymbol(m);
            return sysNS.GetMember<NamespaceSymbol>("Security");
        }

        internal NamespaceSymbol Get_System_NamespaceSymbol(ModuleSymbol m)
        {
            var assembly = m.ContainingSymbol;
            SourceAssemblySymbol sourceAssembly = assembly as SourceAssemblySymbol;
            if (sourceAssembly != null)
            {
                return sourceAssembly.DeclaringCompilation.GlobalNamespace.GetMember<NamespaceSymbol>("System");
            }
            else
            {
                var peAssembly = (PEAssemblySymbol)assembly;
                return peAssembly.CorLibrary.GlobalNamespace.GetMember<NamespaceSymbol>("System");
            }
        }

        internal static void VerifyParamArrayAttribute(ParameterSymbol parameter, SourceModuleSymbol module, bool expected = true, OutputKind outputKind = OutputKind.ConsoleApplication)
        {
            Assert.Equal(expected, parameter.IsParams);

            var emitModule = new PEAssemblyBuilder(module.ContainingSourceAssembly, EmitOptions.Default, outputKind, GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            var paramArrayAttributeCtor = (MethodSymbol)emitModule.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ParamArrayAttribute__ctor);
            bool found = false;

            var context = new EmitContext(emitModule, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            foreach (Microsoft.Cci.ICustomAttribute attr in parameter.GetSynthesizedAttributes())
            {
                if (paramArrayAttributeCtor == (MethodSymbol)attr.Constructor(context))
                {
                    Assert.False(found, "Multiple ParamArrayAttribute");
                    found = true;
                }
            }

            Assert.Equal(expected, found);
            context.Diagnostics.Verify();
        }
    }
}
