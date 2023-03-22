// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// Exports <see cref="IXamlEditAndContinueSolutionProvider"/> for the XAML Language Service in Visual Studio.
    /// </summary>
    [Export(typeof(IXamlEditAndContinueSolutionProvider))]
    internal class XamlEditAndContinueSolutionProvider : IXamlEditAndContinueSolutionProvider, IDisposable
    {
        private readonly IEnumerable<IEditAndContinueSolutionProvider> _editAndContinueSolutionProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlEditAndContinueSolutionProvider([ImportMany] IEnumerable<IEditAndContinueSolutionProvider> editAndContinueSolutionProviders)
        {
            _editAndContinueSolutionProviders = editAndContinueSolutionProviders;

            foreach (var provider in _editAndContinueSolutionProviders)
            {
                provider.SolutionCommitted += OnEditAndContinueSolutionCommitted;
            }
        }

        public event Action<Solution>? SolutionCommitted;

        public void Dispose()
        {
            foreach (var provider in _editAndContinueSolutionProviders)
            {
                provider.SolutionCommitted -= OnEditAndContinueSolutionCommitted;
            }
        }

        private void OnEditAndContinueSolutionCommitted(Solution solution)
        {
            SolutionCommitted?.Invoke(solution);
        }
    }
}
