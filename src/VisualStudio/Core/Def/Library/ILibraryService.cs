// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal interface ILibraryService : ILanguageService
{
    NavInfoFactory NavInfoFactory { get; }
}
