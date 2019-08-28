// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
