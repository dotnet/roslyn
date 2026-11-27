// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

/// <summary>
/// Contains an updated message token and a signal of whether the document Uri was changed.
/// </summary>
[Obsolete("Please move to GenericInterceptionMiddleLayer and generic interceptors.")]
public struct InterceptionResult
{
    public static readonly InterceptionResult NoChange = new(null, false);

    public InterceptionResult(JToken? newToken, bool changedDocumentUri)
    {
        UpdatedToken = newToken;
        ChangedDocumentUri = changedDocumentUri;
    }

    public JToken? UpdatedToken { get; }
    public bool ChangedDocumentUri { get; }
}
