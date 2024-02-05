// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        internal static void VerifyParamArrayAttribute(ParameterSymbol parameter, bool expected = true)
        {
            Assert.Equal(expected, parameter.IsParams);
            Assert.Equal(expected, parameter.IsParamsArray);
            Assert.False(parameter.IsParamsCollection);

            var peParameter = (PEParameterSymbol)parameter;
            var allAttributes = ((PEModuleSymbol)parameter.ContainingModule).GetCustomAttributesForToken(peParameter.Handle);
            var paramArrayAttributes = allAttributes.Where(a => a.AttributeClass.ToTestDisplayString() == "System.ParamArrayAttribute");

            if (expected)
            {
                Assert.Equal(1, paramArrayAttributes.Count());
            }
            else
            {
                Assert.Empty(paramArrayAttributes);
            }
        }
    }
}
