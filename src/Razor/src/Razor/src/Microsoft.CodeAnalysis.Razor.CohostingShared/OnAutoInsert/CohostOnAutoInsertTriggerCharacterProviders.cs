// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.AutoInsert;

namespace Microsoft.VisualStudio.LanguageServices.Razor.LanguageClient.Cohost;

// These are needed to we can get auto-insert trigger character collection
// during registration of CohostOnAutoInsertProvider without using a remote service

[Export(typeof(IOnAutoInsertTriggerCharacterProvider))]
internal sealed class CohostAutoClosingTagOnAutoInsertTriggerCharacterProvider : AutoClosingTagOnAutoInsertProvider;

[Export(typeof(IOnAutoInsertTriggerCharacterProvider))]
internal sealed class CohostCloseTextTagOnAutoInsertTriggerCharacterProvider : CloseTextTagOnAutoInsertProvider;
