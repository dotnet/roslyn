// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal class MethodImplementationReferenceContextWrapper
    {
        private readonly MethodImplementationReferenceContext _methodImplementationReferenceContext;

        public MethodImplementationReferenceContextWrapper(MethodImplementationReferenceContext methodImplementationReferenceContext)
        {
            _methodImplementationReferenceContext = methodImplementationReferenceContext;
        }

        public string FileName => _methodImplementationReferenceContext.FileName;

        public string SurroundingCode => _methodImplementationReferenceContext.SurroundingCode;
    }
}
