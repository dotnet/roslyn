// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public abstract class CompilationReference : MetadataReference
    {
        public Compilation Compilation { get { return CompilationCore; } }
        internal abstract Compilation CompilationCore { get; }

        internal CompilationReference(MetadataReferenceProperties properties)
            : base(properties)
        {
        }

        internal static MetadataReferenceProperties GetProperties(Compilation compilation, ImmutableArray<string> aliases, bool embedInteropTypes)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException("compilation");
            }

            if (compilation.IsSubmission)
            {
                throw new NotSupportedException(CodeAnalysisResources.CannotCreateReferenceToSubmission);
            }

            if (compilation.Options.OutputKind.IsNetModule())
            {
                throw new NotSupportedException(CodeAnalysisResources.CannotCreateReferenceToModule);
            }

            return new MetadataReferenceProperties(
                MetadataImageKind.Assembly,
                aliases,
                embedInteropTypes);
        }

        public override string Display
        {
            get
            {
                return Compilation.AssemblyName;
            }
        }
    }
}
