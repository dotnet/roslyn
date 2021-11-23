// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using PostSharp.Backstage.Extensibility;

namespace Caravela.Compiler.Licensing
{
    internal class CaravelaCompilerApplicationInfo : IApplicationInfo
    {
        public CaravelaCompilerApplicationInfo()
        {
            var attributes =
                typeof(CaravelaCompilerApplicationInfo).Assembly.GetCustomAttributes(typeof(AssemblyBuildInfoAttribute),
                    inherit: false);

            if (attributes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{nameof(CaravelaCompilerApplicationInfo)} has failed to initialize.");
            }

            var assemblyBuildInfo = (AssemblyBuildInfoAttribute)attributes.Single();

            this.BuildDate = assemblyBuildInfo.BuildDate;
            this.Version = assemblyBuildInfo.Version;
            this.IsPrerelease = assemblyBuildInfo.IsPrerelease;

        }

        public DateTime BuildDate { get; }

        public string Name => "Caravela Compiler";

        public Version Version { get; }

        public bool IsPrerelease { get; }
    }
}
