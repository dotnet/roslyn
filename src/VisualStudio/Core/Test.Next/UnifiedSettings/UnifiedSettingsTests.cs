// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings
{
    public class UnifiedSettingsTests
    {
        [Fact]
        public async Task VisualBasicIntellisenseTest()
        {
            using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
            using var pkgDefFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
            var jsonDocument = await JsonNode.ParseAsync(registrationFileStream, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var properties = jsonDocument!.Root["properties"];
        }
    }
}
