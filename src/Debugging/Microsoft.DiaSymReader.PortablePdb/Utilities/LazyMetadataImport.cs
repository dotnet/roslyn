// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class LazyMetadataImport : IDisposable
    {
        private IMetadataImport _lazyMetadataImport;
        private readonly IMetadataImportProvider _metadataImportProviderOpt;

        public LazyMetadataImport(IMetadataImport metadataImport)
        {
            _lazyMetadataImport = metadataImport;
        }

        public LazyMetadataImport(IMetadataImportProvider _metadataImportProvider)
        {
            _metadataImportProviderOpt = _metadataImportProvider;
        }

        public IMetadataImport GetMetadataImport()
        {
            if (_lazyMetadataImport == null)
            {
                var importer = _metadataImportProviderOpt.GetMetadataImport() as IMetadataImport;
                if (importer == null)
                {
                    throw new InvalidOperationException();
                }

                Interlocked.CompareExchange(ref _lazyMetadataImport, importer, null);
            }

            return _lazyMetadataImport;
        }

        public void Dispose()
        {
            var import = Interlocked.Exchange(ref _lazyMetadataImport, null);
            if (import != null && Marshal.IsComObject(import))
            {
                Marshal.ReleaseComObject(import);
            }
        }
    }
}
