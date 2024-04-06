// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// This type only exists for binary compat with TypeScript.  Once they move to EA for
/// <see cref="ClassificationTypeMap"/>, then we can remove this.
/// </summary>
internal abstract class AbstractClassificationTypeMap : IClassificationTypeMap
{
    public abstract IClassificationType GetClassificationType(string name);
}

[Export]
internal sealed class ClassificationTypeMap : AbstractClassificationTypeMap
{
    private readonly IClassificationTypeRegistryService _registryService;
    private readonly Dictionary<string, IClassificationType> _identityMap;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ClassificationTypeMap(IClassificationTypeRegistryService registryService)
    {
        _registryService = registryService;

        // Prepopulate the identity map with the constant string values from ClassificationTypeNames
        var fields = typeof(ClassificationTypeNames).GetFields();
        _identityMap = new Dictionary<string, IClassificationType>(fields.Length, ReferenceEqualityComparer.Instance);

        foreach (var field in fields)
        {
            // The strings returned from reflection do not have reference-identity with the string constants used by
            // the compiler. Fortunately, a call to string.Intern fixes them.
            var rawValue = (string?)field.GetValue(null);
            Contract.ThrowIfNull(rawValue);
            var value = string.Intern(rawValue);

            _identityMap.Add(value, registryService.GetClassificationType(ClassificationLayer.Semantic, value));
        }
    }

    public override IClassificationType GetClassificationType(string name)
    {
        var type = GetClassificationTypeWorker(name);
        if (type == null)
        {
            FatalError.ReportAndCatch(new Exception($"classification type doesn't exist for {name}"));
        }

        return type ?? GetClassificationTypeWorker(ClassificationTypeNames.Text);
    }

    private IClassificationType GetClassificationTypeWorker(string name)
    {
        return _identityMap.TryGetValue(name, out var result)
            ? result
            : _registryService.GetClassificationType(ClassificationLayer.Semantic, name);
    }
}
