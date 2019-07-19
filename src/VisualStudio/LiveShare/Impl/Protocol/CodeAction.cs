//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    [DataContract]
    public class CodeAction
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "edit")]
        public WorkspaceEdit Edit { get; set; }

        [DataMember(Name = "command")]
        public Command Command { get; set; }
    }
}
