// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PostSharp.Backstage.Extensibility;

namespace Caravela.Compiler.Licensing
{
    internal class CaravelaCompilerApplicationInfo : IApplicationInfo
    {
        public CaravelaCompilerApplicationInfo()
        {
            var metadataAttributes =
                typeof(CaravelaCompilerApplicationInfo).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute),
                    inherit: false);

            Version? version = null;
            bool? isPrerelease = null;
            DateTime? buildDate = null;

            bool AllMetadataFound() => version != null && isPrerelease != null && buildDate != null;

            foreach (var metadataAttributeObject in metadataAttributes)
            {
                var metadataAttribute = (AssemblyMetadataAttribute)metadataAttributeObject;

                switch (metadataAttribute.Key)
                {
                    case "CaravelaCompilerVersion":
                        if (!string.IsNullOrEmpty(metadataAttribute.Value))
                        {
                            var versionParts = metadataAttribute.Value.Split('-');
                            version = Version.Parse(versionParts[0]);
                            isPrerelease = versionParts.Length > 1;
                        }

                        break;

                    case "CaravelaCompilerBuildDate":
                        if (!string.IsNullOrEmpty(metadataAttribute.Value))
                        {
                            buildDate = DateTime.Parse(metadataAttribute.Value, CultureInfo.InvariantCulture);
                        }

                        break;
                }

                if (AllMetadataFound())
                {
                    break;
                }
            }

            if (!AllMetadataFound())
            {
                throw new InvalidOperationException(
                    $"{nameof(CaravelaCompilerApplicationInfo)} has failed to initialize.");
            }

            this.Version = version!;
            this.IsPrerelease = isPrerelease!.Value;
            this.BuildDate = buildDate!.Value;
        }

        public DateTime BuildDate { get; }

        public string Name => "Caravela Compiler";

        public Version Version { get; }

        public bool IsPrerelease { get; }
    }
}
