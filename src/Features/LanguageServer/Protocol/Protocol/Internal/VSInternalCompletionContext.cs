// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension class for <see cref="CompletionContext"/> with properties specific to Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSInternalCompletionContext : CompletionContext
    {
        /// <summary>
        /// Gets or sets the <see cref="CompletionTriggerKind"/> indicating how the completion was triggered.
        /// </summary>
        [DataMember(Name = "_vs_invokeKind")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
        [DefaultValue(VSInternalCompletionInvokeKind.Explicit)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VSInternalCompletionInvokeKind InvokeKind
        {
            get;
            set;
        } = VSInternalCompletionInvokeKind.Explicit;
    }
}
