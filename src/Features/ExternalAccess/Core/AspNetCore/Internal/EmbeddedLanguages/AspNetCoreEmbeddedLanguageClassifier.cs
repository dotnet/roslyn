// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages;

[ExportEmbeddedLanguageClassifier(
    nameof(AspNetCoreEmbeddedLanguageClassifier),
    [LanguageNames.CSharp],
    supportsUnannotatedAPIs: false,
    // Add more syntax names here in the future if there are additional cases ASP.Net would like to light up on.
    identifiers: ["Route"]), Shared]
internal class AspNetCoreEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AspNetCoreEmbeddedLanguageClassifier()
    {
    }

    public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
    {
        if (context.Project is null)
            return;

        var classifiers = AspNetCoreClassifierExtensionProvider.GetExtensions(context.Project);
        if (classifiers.Length == 0)
            return;

        var aspContext = new AspNetCoreEmbeddedLanguageClassificationContext(context);
        foreach (var classifier in classifiers)
            classifier.RegisterClassifications(aspContext);
    }
}
