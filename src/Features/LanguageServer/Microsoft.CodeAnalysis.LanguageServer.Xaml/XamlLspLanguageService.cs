// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml;

/// <summary>
/// Empty XAML Language Service to make misc. project features work with XAML LSP
/// </summary>
[ExportLanguageService(typeof(ILanguageService), StringConstants.XamlLanguageName, ServiceLayer.Default), Shared]
internal class XamlLspLanguageService : ILanguageService
{
    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public XamlLspLanguageService()
    {
    }
}
