using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.HubServices
{
    internal static class HubProtocolConstants
    {
        public const string IdPropertyName = nameof(HubDataModel.Id);
        public const string DataPropertyName = nameof(HubDataModel.Data);

        public const string CancelOperationName = "CancelOperation";

        public const string TypePropertyName = "Type";
        public const string RanToCompletionTypePropertyValue = "RanToCompletion";
        public const string FaultedTypePropertyValue = "Faulted";
        public const string CanceledTypePropertyValue = "Canceled";

        public const string PackageSourcesName = "PackageSources";
        public const string CacheDirectoryName = "CacheDirectory";
    }
}
