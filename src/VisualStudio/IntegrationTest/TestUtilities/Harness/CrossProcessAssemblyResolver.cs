// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public sealed class CrossProcessAssemblyResolver : MarshalByRefObject, IAssemblyResolver
    {
        public string TryResolveAssembly(AssemblyName assemblyName)
        {
            // All xunit assemblies are resolved by the test runner
            if (!assemblyName.Name.StartsWith("xunit."))
            {
                switch (assemblyName.Name)
                {
                    case "Microsoft.VisualStudio.LanguageServices.IntegrationTests":
                        break;

                    default:
                        // All other assemblies are resolved by VS
                        return null;
                }
            }

            try
            {
                return Assembly.Load(assemblyName)?.Location;
            }
            catch
            {
                return null;
            }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
