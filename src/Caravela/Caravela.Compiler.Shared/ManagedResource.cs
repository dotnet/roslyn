// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Caravela.Compiler
{
    public sealed class ManagedResource
    {
        public ResourceDescription Resource { get; }
        public bool IncludeInRefAssembly { get; }

        public ManagedResource(ResourceDescription resource, bool includeInRefAssembly = false)
        {
            Resource = resource;
            IncludeInRefAssembly = includeInRefAssembly;
        }
        
#if CARAVELA_COMPILER_INTERFACE
        private static InvalidOperationException NewInvalidOperationException() => new InvalidOperationException("This operation works only inside Caravela.");
#endif

        public string? Name =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
                this.Resource.ResourceName;
#endif
        
        public bool IsPublic =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            this.Resource.IsPublic;
#endif
      
        public bool IsEmbedded =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            this.Resource.IsEmbedded;
#endif
        
        public Stream GetData() =>
#if CARAVELA_COMPILER_INTERFACE
            throw NewInvalidOperationException();
#else
            this.Resource.DataProvider();
#endif
        
    }

}
