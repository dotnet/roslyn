// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Creates instances of the IInteractiveWindow.  
    /// </summary>
    public interface IInteractiveWindowFactoryService
    {
        /// <summary>
        /// Creates a new interactive window which runs against the provided interactive evaluator.
        /// </summary>
        IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator);
    }
}
