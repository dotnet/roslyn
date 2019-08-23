// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol
{
    [DataContract]
    internal class RoslynExperimentalCapabilities
    {
        [DataMember(Name = "syntacticLspProvider")]
        public bool SyntacticLspProvider
        {
            get;
            set;
        }
    }
}
