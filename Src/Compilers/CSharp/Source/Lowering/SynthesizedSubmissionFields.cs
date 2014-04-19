// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

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
        private readonly NamedTypeSymbol declaringSubmissionClass;
        private readonly CSharpCompilation compilation;

        private FieldSymbol hostObjectField;
        private Dictionary<ImplicitNamedTypeSymbol, FieldSymbol> previousSubmissionFieldMap;

        public SynthesizedSubmissionFields(CSharpCompilation compilation, NamedTypeSymbol submissionClass)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(submissionClass.IsSubmissionClass);

            this.declaringSubmissionClass = submissionClass;
            this.compilation = compilation;
        }

        internal int Count
        {
            get
            {
                return previousSubmissionFieldMap == null ? 0 : previousSubmissionFieldMap.Count;
            }
        }

        internal IEnumerable<FieldSymbol> FieldSymbols
        {
            get
            {
                return previousSubmissionFieldMap == null ? SpecializedCollections.EmptyArray<FieldSymbol>() : (IEnumerable<FieldSymbol>)previousSubmissionFieldMap.Values;
            }
        }

        internal FieldSymbol GetHostObjectField()
        {
            if ((object)hostObjectField != null)
            {
                return hostObjectField;
            }

            var hostObjectTypeSymbol = compilation.GetHostObjectTypeSymbol();
            if ((object)hostObjectTypeSymbol != null && hostObjectTypeSymbol.Kind != SymbolKind.ErrorType)
            {
                return hostObjectField = new SynthesizedFieldSymbol(
                    declaringSubmissionClass, hostObjectTypeSymbol, "<host-object>", isPublic: false, isReadOnly: true, isStatic: false);
            }

            return null;
        }

        internal FieldSymbol GetOrMakeField(ImplicitNamedTypeSymbol previousSubmissionType)
        {
            if (previousSubmissionFieldMap == null)
            {
                previousSubmissionFieldMap = new Dictionary<ImplicitNamedTypeSymbol, FieldSymbol>();
            }

            FieldSymbol previousSubmissionField;
            if (!previousSubmissionFieldMap.TryGetValue(previousSubmissionType, out previousSubmissionField))
            {
                // TODO: generate better name?
                previousSubmissionField = new SynthesizedFieldSymbol(
                    declaringSubmissionClass,
                    previousSubmissionType,
                    "<" + previousSubmissionType.Name + ">",
                    isReadOnly: true);
                previousSubmissionFieldMap.Add(previousSubmissionType, previousSubmissionField);
            }
            return previousSubmissionField;
        }

        internal void AddToType(NamedTypeSymbol containingType, PEModuleBuilder moduleBeingBuilt)
        {
            foreach (var field in FieldSymbols)
            {
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, field);
            }

            FieldSymbol hostObjectField = GetHostObjectField();
            if ((object)hostObjectField != null)
            {
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, hostObjectField);
            }
        }
    }
}