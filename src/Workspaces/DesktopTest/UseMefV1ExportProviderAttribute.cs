// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal class UseMefV1ExportProviderAttribute : UseExportProviderAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            DesktopMefHostServices.ResetHostServicesTestOnly();
            base.Before(methodUnderTest);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            base.After(methodUnderTest);
            DesktopMefHostServices.ResetHostServicesTestOnly();
        }
    }
}
