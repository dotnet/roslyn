// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting.Core;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.AspNetCore.Razor.Test.Common.Mef;

internal static class ExportProviderExtensions
{
    public static CompositionContext AsCompositionContext(this ExportProvider exportProvider)
    {
        return new CompositionContextShim(exportProvider);
    }

    private class CompositionContextShim : CompositionContext
    {
        private readonly ExportProvider _exportProvider;

        public CompositionContextShim(ExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
        }

        public override bool TryGetExport(CompositionContract contract, [NotNullWhen(true)] out object? export)
        {
            var importMany = contract.MetadataConstraints.Contains(new KeyValuePair<string, object>("IsImportMany", true));
            var (contractType, metadataType, isLazy) = GetContractType(contract.ContractType, importMany);

            var method = (metadataType, isLazy) switch
            {
                (not null, true) => GetExportProviderGenericMethod(nameof(ExportProvider.GetExports), contractType, metadataType),
                (null, true) => GetExportProviderGenericMethod(nameof(ExportProvider.GetExports), contractType),
                (null, false) => GetExportProviderGenericMethod(nameof(ExportProvider.GetExportedValues), contractType),
                _ => null
            };

            if (method is null)
            {
                export = null;
                return false;
            }

            export = method.Invoke(_exportProvider, [contract.ContractName]);
            Assumes.NotNull(export);

            return true;

            static MethodInfo GetExportProviderGenericMethod(string methodName, params Type[] typeArguments)
            {
                var methodInfo = (from method in typeof(ExportProvider).GetTypeInfo().GetMethods()
                                  where method.Name == methodName
                                  where method.IsGenericMethod && method.GetGenericArguments().Length == typeArguments.Length
                                  where method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string)
                                  select method).Single();

                return methodInfo.MakeGenericMethod(typeArguments);
            }
        }

        private static (Type exportType, Type? metadataType, bool isLazy) GetContractType(Type contractType, bool importMany)
        {
            if (importMany)
            {
                if (contractType.IsConstructedGenericType)
                {
                    if (contractType.GetGenericTypeDefinition() == typeof(IList<>)
                        || contractType.GetGenericTypeDefinition() == typeof(ICollection<>)
                        || contractType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        contractType = contractType.GenericTypeArguments[0];
                    }
                }
                else if (contractType.IsArray)
                {
                    contractType = contractType.GetElementType().AssumeNotNull();
                }
            }

            if (contractType.IsConstructedGenericType)
            {
                if (contractType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    return (contractType.GenericTypeArguments[0], null, true);
                }
                else if (contractType.GetGenericTypeDefinition() == typeof(Lazy<,>))
                {
                    return (contractType.GenericTypeArguments[0], contractType.GenericTypeArguments[1], true);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            return (contractType, null, false);
        }
    }
}
