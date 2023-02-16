// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// A parameter object that indicates whether the code actions are allowed to offer generating code in
    /// hidden regions.
    /// </summary>
    /// <remarks>
    /// Code actions normal don't generate code into hidden regions (eg things protected by <c>#line hidden</c> directives
    /// but Razor generated files are almost entirely hidden, so the Razor client relaxes this rule at will,
    /// in order to provide better code fixes and refactorings to users. Not all code fixes necessarily support
    /// this option.
    /// </remarks>
    [DataContract]
    internal class CodeActionParamsWithOptions : CodeActionParams
    {
        [DataMember(Name = "allowGenerateInHiddenCode")]
        public bool AllowGenerateInHiddenCode { get; set; }
    }
}
