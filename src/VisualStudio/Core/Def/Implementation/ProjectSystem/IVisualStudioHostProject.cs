// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal interface IVisualStudioHostProject
    {
        ProjectId Id { get; }
    }
}
