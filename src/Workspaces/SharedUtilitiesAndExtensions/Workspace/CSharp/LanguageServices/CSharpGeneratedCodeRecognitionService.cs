// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GeneratedCodeRecognition;

[ExportLanguageService(typeof(IGeneratedCodeRecognitionService), LanguageNames.CSharp), Shared]
internal sealed class CSharpGeneratedCodeRecognitionService : AbstractGeneratedCodeRecognitionService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpGeneratedCodeRecognitionService()
    {
    }
}
