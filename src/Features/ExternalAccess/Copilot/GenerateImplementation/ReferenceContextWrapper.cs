// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class ReferenceContextWrapper(ReferenceContext context)
    {
        public string FileName => context.FileName;
        public string SurroundingCode => context.SurroundingCode;
    }
}
