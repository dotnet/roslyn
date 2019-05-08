// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// This is an abstraction on top of <see cref="KnownUIContexts"/>.
    /// 
    /// We need this abstraction for unit test since KnownUIContext is static VS type that we can't
    /// use or mock in unit test
    /// </summary>
    internal interface IKnownUIContextService
    {
        bool IsSolutionBuilding { get; }
        event EventHandler<UIContextChangedEventArgs> SolutionBuilding;
    }

    [Export(typeof(IKnownUIContextService))]
    internal class KnownUIContextService : IKnownUIContextService
    {
        public bool IsSolutionBuilding => KnownUIContexts.SolutionBuildingContext.IsActive;

        public event EventHandler<UIContextChangedEventArgs> SolutionBuilding
        {
            add { KnownUIContexts.SolutionBuildingContext.UIContextChanged += value; }
            remove { KnownUIContexts.SolutionBuildingContext.UIContextChanged -= value; }
        }
    }
}
