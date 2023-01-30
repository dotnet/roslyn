// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests;

[UseExportProvider]
public class VisualStudioStorageReadFallbackTests
{
    [Fact]
    public void Fallbacks()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        foreach (var export in exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>())
        {
            var langauge = export.Metadata.ConfigName.StartsWith("csharp_") || export.Metadata.ConfigName.StartsWith("visual_basic_")
                ? null : LanguageNames.CSharp;

            // if no flags are set the result should be default:
            Assert.Equal(default(Optional<object?>), export.Value.TryRead(langauge, (storageKey, storageType) => default(Optional<object?>)));
        }
    }
}
