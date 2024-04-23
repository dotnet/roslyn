// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal partial class OnTheFlyDocsView
    {
        /// <summary>
        /// Represents the potential states of the view.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// The view is displaying the on-demand hyperlink.
            /// </summary>
            OnDemandLink,

            /// <summary>
            /// The view is in the loading state.
            /// </summary>
            Loading,

            /// <summary>
            /// The view is displaying computed results.
            /// </summary>
            Finished,
        }
    }
}
