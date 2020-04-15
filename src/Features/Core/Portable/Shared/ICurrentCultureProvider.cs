// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Shared
{
    internal interface ICurrentCultureProvider : IWorkspaceService
    {
        CultureInfo CurrentCulture { get; }
    }

    [ExportWorkspaceService(typeof(ICurrentCultureProvider)), Shared]
    internal class DefaultCurrentCultureProvider : ICurrentCultureProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultCurrentCultureProvider()
        {
        }

        public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
    }
}
