// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal class PullMemberUpWarningViewModel : AbstractNotifyPropertyChanged
    {
        public List<string> WarningMessageContainer { get; set; }

        internal PullMemberUpWarningViewModel(List<string> warningMessages)
        {
            WarningMessageContainer = warningMessages;
        }
    }
}
