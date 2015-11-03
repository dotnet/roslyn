// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal interface ILibraryService : ILanguageService
    {
        NavInfoFactory NavInfoFactory { get; }
    }
}
