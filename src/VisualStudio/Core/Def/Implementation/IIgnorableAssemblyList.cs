// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal interface IIgnorableAssemblyList
    {
        bool Includes(AssemblyIdentity assemblyIdentity);
    }
}
