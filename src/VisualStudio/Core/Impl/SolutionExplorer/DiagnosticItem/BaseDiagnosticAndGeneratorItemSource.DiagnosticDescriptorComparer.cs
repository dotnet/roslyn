// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal abstract partial class BaseDiagnosticAndGeneratorItemSource
{
    internal sealed class DiagnosticDescriptorComparer : IComparer<DiagnosticDescriptor>
    {
        public int Compare(DiagnosticDescriptor x, DiagnosticDescriptor y)
        {
            var comparison = StringComparer.CurrentCulture.Compare(x.Id, y.Id);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.CurrentCulture.Compare(x.Title.ToString(CultureInfo.CurrentUICulture), y.Title.ToString(CultureInfo.CurrentUICulture));
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.CurrentCulture.Compare(x.MessageFormat.ToString(CultureInfo.CurrentUICulture), y.MessageFormat.ToString(CultureInfo.CurrentUICulture));

            return comparison;
        }
    }
}
