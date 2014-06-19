using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Roslyn.Compilers.Scripting;

namespace Roslyn.Compilers.CSharp.Scripting
{
    public sealed class Submission : CommonSubmission
    {
        // TODO: null if compiled into a method
        private readonly NamedTypeSymbol scriptClass;

        private readonly Imports imports;

        internal Submission(
            CommonSubmission previous,
            NamedTypeSymbol scriptClass,
            Imports imports,
            Delegate factory) 
            : base(previous, factory)
        {
            Debug.Assert(imports != null);
            this.scriptClass = scriptClass;
            this.imports = imports;
        }

        internal NamedTypeSymbol ScriptClass
        {
            get
            {
                return scriptClass;
            }
        }

        internal Imports Imports
        {
            get
            {
                return imports;
            }
        }

        public Compilation Compilation
        {
            get
            {
                return ((SourceAssemblySymbol)scriptClass.ContainingAssembly).Compilation;
            }
        }

        internal override Type HostObjectType 
        {
            get
            {
                return Compilation.HostObjectType;
            }
        }

        internal static Submission FromCompilation(
            Type delegateType,
            Compilation compilation,
            ReflectionEmitResult emitResult,
            CommonSubmission previousInteraction)
        {
            Imports imports = ((SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace).GetBoundImports().SingleOrDefault() ?? Imports.Empty;
            var factory = (emitResult != null) ? Delegate.CreateDelegate(delegateType, emitResult.EntryPoint) : null;
            return new Submission(previousInteraction, compilation.ScriptClass, imports, factory);
        }

        internal override IEnumerable<MetadataReference> GetReferences()
        {
            var referenceNames = scriptClass.ContainingAssembly.Modules.SelectMany(module => module.GetReferencedAssemblies());
            foreach (AssemblyName referenceName in referenceNames)
            {
                if (referenceName.CodeBase != null)
                {
                    // TODO (tomat): could we avoid assembly binding in the new compilation and
                    // return MD reference that holds on the existing AssemblySymbols?
                    yield return new AssemblyFileReference(referenceName.CodeBase);
                }
            }
        }
    }
}
