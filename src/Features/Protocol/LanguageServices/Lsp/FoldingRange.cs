// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.LanguageServer.Protocol
{
    [DataContract]
    public class FoldingRange
    {
        [DataMember(IsRequired = true, Name = "startLine")]
        public int StartLine { get; set; }

        [DataMember(Name = "startCharacter")]
        public int StartCharacter { get; set; }

        [DataMember(IsRequired = true, Name = "endLine")]
        public int EndLine { get; set; }

        [DataMember(Name = "endCharacter")]
        public int EndCharacter { get; set; }

        [DataMember(Name = "kind")]
        public string Kind { get; set; }
    }
}
