// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.LanguageServer.Protocol
{
    [DataContract]
    public class FoldingRangeParams
    {
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; }
    }
}
