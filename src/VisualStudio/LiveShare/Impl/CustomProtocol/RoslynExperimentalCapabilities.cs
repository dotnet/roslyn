// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
