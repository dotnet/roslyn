using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;
using Ref = System.Reflection;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Runtime representation of an interactive session.
    /// </summary>
    /// <remarks>
    /// Session is not thread-safe, i.e. parallel executions against the same session object might fail.
    /// However executing methods defined in a context of a session in parallel is safe as long as the methods themselves are thread-safe.
    /// (i.e. accessing data from previous submissions is safe as long as they are readonly or guarded by a user maintained lock).
    /// </remarks>
    public sealed class Session
    {
        private readonly CommonScriptEngine engine;
        internal readonly object hostObject;
        internal readonly bool isCollectible;
        private readonly Type hostObjectType;
        internal object[] submissions;

        // References added to this session by the user since the last submission.
        private ReadOnlyArray<MetadataReference> pendingReferences;

        // Namespaces imported to this session by the user since the last submission.
        private ReadOnlyArray<string> pendingNamespaces;

        private FileResolver fileResolver;
        
        // The last submission compiled against this session.
        internal CommonCompilation LastSubmission { get; private set; }

        internal Session(CommonScriptEngine engine, object hostObject, Type hostObjectType, bool isCollectible)
        {
            this.pendingReferences = engine.GetReferences();
            this.pendingNamespaces = engine.GetImportedNamespaces();
            this.fileResolver = engine.FileResolver;
            this.engine = engine;
            this.submissions = new object[16];
            this.hostObject = hostObject;
            this.hostObjectType = hostObjectType;
            this.isCollectible = isCollectible;
        }

        public CommonScriptEngine Engine
        {
            get { return engine; }
        }

        internal FileResolver FileResolver
        {
            get 
            {
                return fileResolver; 
            }

            // for testing
            set
            {
                Debug.Assert(value != null);
                fileResolver = value;
            }
        }

        internal Type HostObjectType
        {
            get { return hostObjectType; }
        }

        public void SetReferenceSearchPaths(params string[] paths)
        {
            SetReferenceSearchPaths((IEnumerable<string>)paths);
        }

        public void SetReferenceSearchPaths(IEnumerable<string> paths)
        {
            SetReferenceSearchPaths(ReadOnlyArray<string>.CreateFrom(paths));
        }

        public void SetReferenceSearchPaths(ReadOnlyArray<string> paths)
        {
            if (paths != fileResolver.AssemblySearchPaths)
            {
                FileResolver.ValidateSearchPaths(paths, "paths");
                fileResolver = CommonScriptEngine.CreateFileResolver(paths, engine.BaseDirectory);
            }
        }

        public ReadOnlyArray<string> ReferenceSearchPaths
        {
            get { return fileResolver.AssemblySearchPaths; }
        }

        public void AddReference(string assemblyDisplayNameOrPath)
        {
            AddReference(engine.CreateMetadataReference(assemblyDisplayNameOrPath, this.fileResolver));
        }

        public void AddReference(Ref.Assembly assembly)
        {
            AddReference(engine.CreateMetadataReference(assembly));
        }

        public void AddReference(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }

            if (reference.Properties.Kind != MetadataImageKind.Assembly)
            {
                throw new ArgumentException("Expected an assembly reference.".NeedsLocalization(), "reference");
            }

            pendingReferences = pendingReferences.Append(reference);
        }

        public void ImportNamespace(string @namespace)
        {
            CommonScriptEngine.ValidateNamespace(@namespace);
            pendingNamespaces = pendingNamespaces.Append(@namespace);
        }

        public void ExecuteFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string code = File.ReadAllText(path);
            engine.Execute<object>(code, path, null, this, isInteractive: false);
        }

        public object Execute(string code)
        {
            return engine.Execute<object>(code, path: "", diagnostics: null, session: this, isInteractive: true);
        }

        public T Execute<T>(string code)
        {
            return engine.Execute<T>(code, path: "", diagnostics: null, session: this, isInteractive: true);
        }

        public Submission<T> CompileSubmission<T>(string code, string path = null, bool isInteractive = true)
        {
            return engine.CompileSubmission<T>(code, this, path, isInteractive);
        }

        // internal for testing
        internal ReadOnlyArray<MetadataReference> PendingReferences
        {
            get { return pendingReferences; }
        }

        internal IEnumerable<MetadataReference> GetReferencesForCompilation()
        {
            // TODO (tomat): RESOLVED? bound imports should be reused from previous submission instead of passing 
            // them to every submission in the chain. See bug #7802.

            var previousSubmission = LastSubmission;

            if (previousSubmission != null)
            {
                return previousSubmission.References.Concat(pendingReferences.AsEnumerable());
            }
            else
            {
                return pendingReferences.AsEnumerable();
            }
        }

        internal ReadOnlyArray<string> GetNamespacesForCompilation()
        {
            return pendingNamespaces;
        }

        internal void SubmissionCompiled(CommonCompilation compilation)
        {
            LastSubmission = compilation;
            pendingReferences = ReadOnlyArray<MetadataReference>.Empty;
            // TODO (tomat): shouldn't imported namespaces behave the same as references?
            // do not reset pending imported namespaces - only usings specified in 
            // code are lookued up by the InteractiveUsingsBinder
        }
    }
}

