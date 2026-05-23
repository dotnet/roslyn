// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

/// <summary>
/// Contains an updated message token and a signal of whether the document Uri was changed.
/// </summary>
public struct GenericInterceptionResult<TJsonToken>
{
    public static readonly GenericInterceptionResult<TJsonToken> NoChange = new(default, false);

    public GenericInterceptionResult(TJsonToken? newToken, bool changedDocumentUri)
    {
        UpdatedToken = newToken;
        ChangedDocumentUri = changedDocumentUri;
    }

    public TJsonToken? UpdatedToken { get; }
    public bool ChangedDocumentUri { get; }
}
