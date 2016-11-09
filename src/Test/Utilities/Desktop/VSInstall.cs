// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Roslyn.Test.Utilities
{
    internal static class VSInstall
    {
        private static ISetupConfiguration GetSetupConfiguration()
        {
            ISetupConfiguration setupConfiguration;

            try
            {
                setupConfiguration = new SetupConfiguration();
            }
            catch (COMException comException) when (comException.HResult == Interop.REGDB_E_CLASSNOTREG)
            {
                // Fallback to P/Invoke if the COM registration is missing
                Interop.GetSetupConfiguration(out setupConfiguration);
            }

            return setupConfiguration;
        }

        private static IEnumerable<ISetupInstance> EnumerateVisualStudioInstances()
        {
            var setupConfiguration = GetSetupConfiguration() as ISetupConfiguration2;

            var instanceEnumerator = setupConfiguration.EnumAllInstances();
            var instances = new ISetupInstance[3];

            var instancesFetched = 0;
            instanceEnumerator.Next(instances.Length, instances, out instancesFetched);

            if (instancesFetched == 0)
            {
                throw new Exception("There were no instances of Visual Studio 15.0 or later found.");
            }

            do
            {
                for (var index = 0; index < instancesFetched; index++)
                {
                    yield return instances[index];
                }

                instanceEnumerator.Next(instances.Length, instances, out instancesFetched);
            }
            while (instancesFetched != 0);
        }

        private static ISetupInstance LocateVisualStudioInstance(string vsProductVersion, HashSet<string> requiredPackageIds)
        {
            var instances = EnumerateVisualStudioInstances().Where((instance) => instance.GetInstallationVersion().StartsWith(vsProductVersion));

            var instanceFoundWithInvalidState = false;

            foreach (ISetupInstance2 instance in instances)
            {
                var packages = instance.GetPackages()
                                        .Where((package) => requiredPackageIds.Contains(package.GetId()));

                if (packages.Count() != requiredPackageIds.Count)
                {
                    continue;
                }

                const InstanceState minimumRequiredState = InstanceState.Local | InstanceState.Registered;

                var state = instance.GetState();

                if ((state & minimumRequiredState) == minimumRequiredState)
                {
                    return instance;
                }

                Console.WriteLine($"An instance matching the specified requirements but had an invalid state. (State: {state})");
                instanceFoundWithInvalidState = true;
            }

            throw new Exception(instanceFoundWithInvalidState ?
                                "An instance matching the specified requirements was found but it was in an invalid state." :
                                "There were no instances of Visual Studio 15.0 or later found that match the specified requirements.");
        }

        public static string GetInstallPath(string vsProductVersion, string[] requiredPackageIds)
        {
            var majorVsProductVersion = vsProductVersion.Split('.')[0];

            if (int.Parse(majorVsProductVersion) < 15)
            {
                throw new PlatformNotSupportedException("This helper method is only supported on Visual Studio 15.0 and later.");
            }

            var immutableRequiredPackageIds = new HashSet<string>(requiredPackageIds);
            var instance = LocateVisualStudioInstance(vsProductVersion, immutableRequiredPackageIds) as ISetupInstance2;

            var installationPath = instance.GetInstallationPath();
            return installationPath;
        }
    }
}
