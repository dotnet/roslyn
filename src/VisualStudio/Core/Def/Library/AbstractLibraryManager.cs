// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal abstract partial class AbstractLibraryManager : IVsCoTaskMemFreeMyStrings
{
    internal readonly Guid LibraryGuid;

    public readonly IThreadingContext ThreadingContext;
    public readonly IComponentModel ComponentModel;
    public readonly IServiceProvider ServiceProvider;

    protected AbstractLibraryManager(Guid libraryGuid, IComponentModel componentModel, IServiceProvider serviceProvider)
    {
        LibraryGuid = libraryGuid;
        ComponentModel = componentModel;
        ServiceProvider = serviceProvider;
        ThreadingContext = componentModel.GetService<IThreadingContext>();
    }
}
