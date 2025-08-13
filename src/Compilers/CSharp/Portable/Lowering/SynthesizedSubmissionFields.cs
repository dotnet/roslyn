// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Tracks synthesized fields that are needed in a submission being compiled.
    /// </summary>
    /// <remarks>
    /// For every other submission referenced by this submission we add a field, so that we can access members of the target submission.
    /// A field is also needed for the host object, if provided.
    /// </remarks>
    internal class SynthesizedSubmissionFields
    {
        private readonly NamedTypeSymbol _declaringSubmissionClass;
        private readonly CSharpCompilation _compilation;

        private FieldSymbol _hostObjectField;
        private Dictionary<ImplicitNamedTypeSymbol, FieldSymbol> _previousSubmissionFieldMap;

        public SynthesizedSubmissionFields(CSharpCompilation compilation, NamedTypeSymbol submissionClass)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(submissionClass.IsSubmissionClass);

            _declaringSubmissionClass = submissionClass;
            _compilation = compilation;
        }

        internal int Count
        {
            get
            {
                return _previousSubmissionFieldMap == null ? 0 : _previousSubmissionFieldMap.Count;
            }
        }

        internal IEnumerable<FieldSymbol> FieldSymbols
        {
            get
            {
                return _previousSubmissionFieldMap == null ? Array.Empty<FieldSymbol>() : (IEnumerable<FieldSymbol>)_previousSubmissionFieldMap.Values;
            }
        }

        internal FieldSymbol GetHostObjectField()
        {
            if ((object)_hostObjectField != null)
            {
                return _hostObjectField;
            }

            var hostObjectTypeSymbol = _compilation.GetHostObjectTypeSymbol();
            if ((object)hostObjectTypeSymbol != null && hostObjectTypeSymbol.Kind != SymbolKind.ErrorType)
            {
                return _hostObjectField = new SynthesizedFieldSymbol(
                    _declaringSubmissionClass, hostObjectTypeSymbol, "<host-object>", DeclarationModifiers.Private, isReadOnly: true, isStatic: false);
            }

            return null;
        }

        internal FieldSymbol GetOrMakeField(ImplicitNamedTypeSymbol previousSubmissionType)
        {
            if (_previousSubmissionFieldMap == null)
            {
                _previousSubmissionFieldMap = new Dictionary<ImplicitNamedTypeSymbol, FieldSymbol>();
            }

            FieldSymbol previousSubmissionField;
            if (!_previousSubmissionFieldMap.TryGetValue(previousSubmissionType, out previousSubmissionField))
            {
                // TODO: generate better name?
                previousSubmissionField = new SynthesizedFieldSymbol(
                    _declaringSubmissionClass,
                    previousSubmissionType,
                    "<" + previousSubmissionType.Name + ">",
                    isReadOnly: true);
                _previousSubmissionFieldMap.Add(previousSubmissionType, previousSubmissionField);
            }
            return previousSubmissionField;
        }

        internal void AddToType(NamedTypeSymbol containingType, PEModuleBuilder moduleBeingBuilt)
        {
            foreach (var field in FieldSymbols)
            {
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, field.GetCciAdapter());
            }

            FieldSymbol hostObjectField = GetHostObjectField();
            if ((object)hostObjectField != null)
            {
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, hostObjectField.GetCciAdapter());
            }
        }
    }
}
