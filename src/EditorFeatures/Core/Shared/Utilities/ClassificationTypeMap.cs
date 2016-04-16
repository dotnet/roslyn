// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [Export]
    internal class ClassificationTypeMap
    {
        private readonly Dictionary<string, IClassificationType> _identityMap;
        private readonly IClassificationTypeRegistryService _registryService;

        public IClassificationFormatMapService ClassificationFormatMapService { get; }

        [ImportingConstructor]
        internal ClassificationTypeMap(
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService registryService)
        {
            this.ClassificationFormatMapService = classificationFormatMapService;
            _registryService = registryService;

            // Prepopulate the identity map with the constant string values from ClassificationTypeNames
            var fields = typeof(ClassificationTypeNames).GetFields();
            _identityMap = new Dictionary<string, IClassificationType>(fields.Length, ReferenceEqualityComparer.Instance);
            foreach (var field in fields)
            {
                var value = (string)field.GetValue(null);

                // The strings returned from reflection do not have reference-identity
                // with the string constants used by the compiler. Fortunately, a call
                // to string.Intern fixes them.
                value = string.Intern(value);
                _identityMap.Add(value, registryService.GetClassificationType(value));
            }
        }

        public IClassificationType GetClassificationType(string name)
        {
            IClassificationType result;
            return _identityMap.TryGetValue(name, out result)
                ? result
                : _registryService.GetClassificationType(name);
        }
    }
}
