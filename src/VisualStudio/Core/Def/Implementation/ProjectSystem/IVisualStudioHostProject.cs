// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
