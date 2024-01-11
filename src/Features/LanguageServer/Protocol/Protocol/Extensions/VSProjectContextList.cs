﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// <see cref="VSProjectContextList" /> represents the response to the
    /// 'textDocument/_vs_getProjectContexts' request.
    /// </summary>
    [DataContract]
    internal class VSProjectContextList
    {
        /// <summary>
        /// Gets or sets the document contexts associated with a text document.
        /// </summary>
        [DataMember(Name = "_vs_projectContexts")]
        public VSProjectContext[] ProjectContexts
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the index of the default entry of the <see cref="VSProjectContext" /> array.
        /// </summary>
        [DataMember(Name = "_vs_defaultIndex")]
        public int DefaultIndex
        {
            get;
            set;
        }
    }
}
