// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed partial class CodeStyleHostLanguageServices : HostLanguageServices
    {
        private sealed class MefHostExportProvider : IMefHostExportProvider
        {
            private readonly CompositionHost _compositionContext;

            private MefHostExportProvider(CompositionHost compositionContext)
                => _compositionContext = compositionContext;

            public static MefHostExportProvider Create(string languageName)
            {
                var assemblies = CreateAssemblies(languageName);
                var compositionConfiguration = new ContainerConfiguration().WithAssemblies(assemblies, CodeStyleAttributedModelProvider.Instance);
                return new MefHostExportProvider(compositionConfiguration.CreateContainer());
            }

            private static ImmutableArray<Assembly> CreateAssemblies(string languageName)
            {
                using var disposer = ArrayBuilder<string>.GetInstance(out var assemblyNames);

                assemblyNames.Add("Microsoft.CodeAnalysis.CodeStyle.Fixes");
                switch (languageName)
                {
                    case LanguageNames.CSharp:
                        assemblyNames.Add("Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes");
                        break;

                    case LanguageNames.VisualBasic:
                        assemblyNames.Add("Microsoft.CodeAnalysis.VisualBasic.CodeStyle.Fixes");
                        break;
                }

                return MefHostServices.DefaultAssemblies.Concat(
                    MefHostServicesHelpers.LoadNearbyAssemblies(assemblyNames));
            }

            IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
                => _compositionContext.GetExports<TExtension>().Select(e => new Lazy<TExtension>(() => e));

            IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
                => _compositionContext.GetExports<Lazy<TExtension, TMetadata>>();

            /// <summary>
            /// Custom implementation of <see cref="AttributedModelProvider"/> which excludes MEF parts intended for
            /// languages not supported by the code style layer (only C# and Visual Basic are supported here). This is a
            /// workaround for the fact that MEF 2 does not silently reject MEF parts with missing required imports,
            /// which differs from MEF 1 and VS-MEF which are used elsewhere in the product.
            /// </summary>
            /// <seealso href="https://github.com/dotnet/roslyn/issues/58841"/>
            private sealed class CodeStyleAttributedModelProvider : AttributedModelProvider
            {
                public static readonly CodeStyleAttributedModelProvider Instance = new();

                public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, MemberInfo member)
                {
                    _ = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
                    _ = member ?? throw new ArgumentNullException(nameof(member));

                    if (member is not System.Reflection.TypeInfo && (object)member.DeclaringType != reflectedType)
                        return Array.Empty<Attribute>();

                    return FilterCustomAttributes(CustomAttributeExtensions.GetCustomAttributes(member, inherit: false));
                }

                public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, ParameterInfo parameter)
                {
                    _ = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
                    _ = parameter ?? throw new ArgumentNullException(nameof(parameter));

                    return FilterCustomAttributes(CustomAttributeExtensions.GetCustomAttributes(parameter, inherit: false));
                }

                private static IEnumerable<Attribute> FilterCustomAttributes(IEnumerable<Attribute> attributes)
                {
                    var inputArray = attributes.AsArray();
                    List<Attribute>? outputArray = null;
                    for (var i = 0; i < inputArray.Length; i++)
                    {
                        if (IsExcluded(inputArray[i]))
                        {
                            if (outputArray is null)
                            {
                                if (i == 0 && inputArray.Length == 1)
                                {
                                    return Array.Empty<Attribute>();
                                }

                                outputArray = new List<Attribute>(inputArray.Length - 1);
                                for (var j = 0; j < i; j++)
                                {
                                    outputArray.Add(inputArray[i]);
                                }
                            }

                            // Don't add the current item to the output array
                            continue;
                        }

                        outputArray?.Add(inputArray[i]);
                    }

                    return outputArray?.ToArray() ?? inputArray;

                    static bool IsExcluded(Attribute attribute)
                    {
                        // Exclude language service exports that don't apply to C# or Visual Basic
                        if (attribute is ExportLanguageServiceAttribute { Language: not (LanguageNames.CSharp or LanguageNames.VisualBasic) })
                            return true;

                        // Include this attribute
                        return false;
                    }
                }
            }
        }
    }
}
