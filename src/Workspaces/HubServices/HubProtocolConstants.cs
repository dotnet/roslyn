using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.HubServices.SymbolSearch.Data;

namespace Microsoft.CodeAnalysis.HubServices
{
    internal static class HubProtocolConstants
    {
        public const string IdPropertyName = nameof(HubDataModel.Id);
        public const string DataPropertyName = nameof(HubDataModel.Data);

        public const string CancelOperation = nameof(CancelOperation);

        public const string ResponseType = nameof(ResponseType);
        public const string RanToCompletion = nameof(RanToCompletion);
        public const string Faulted = nameof(Faulted);
        public const string Canceled = nameof(Canceled);

        public const string PackageSources = nameof(PackageSources);
        public const string CacheDirectory = nameof(CacheDirectory);

        public const string Source = nameof(Source);
        public const string Name = nameof(Name);
        public const string Arity = nameof(Arity);

        public const string Version = nameof(PackageWithTypeResult.Version);
        public const string TypeName = nameof(PackageWithTypeResult.TypeName);
        public const string Rank = nameof(PackageWithTypeResult.Rank);
        public const string PackageName = nameof(PackageWithTypeResult.PackageName);
        public const string ContainingNamespaceNames = nameof(PackageWithTypeResult.ContainingNamespaceNames);

        public const string AssemblyName = nameof(ReferenceAssemblyWithTypeResult.AssemblyName);
    }
}