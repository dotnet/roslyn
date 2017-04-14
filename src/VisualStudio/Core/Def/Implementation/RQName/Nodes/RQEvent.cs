// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName.Nodes
{
    internal class RQEvent : RQMethodPropertyOrEvent
    {
        public RQEvent(RQUnconstructedType containingType, RQMethodPropertyOrEventName memberName)
            : base(containingType, memberName)
        { }

        protected override string RQKeyword => RQNameStrings.Event;
    }
}
