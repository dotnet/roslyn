// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents analyzers stored in an analyzer assembly file.
    /// </summary>
    /// <remarks>
    /// Analyzer are read from the file, owned by the reference, and doesn't change 
    /// since the reference is accessed until the reference object is garbage collected.
    /// During this time the file is open and its content is read-only.
    /// 
    /// If you need to manage the lifetime of the anayzer reference (and the file stream) explicitly use <see cref="AnalyzerImageReference"/>.
    /// </remarks>
    public sealed class AnalyzerFileReference : AnalyzerReference
    {
        private readonly string fullPath;
        private string displayName;
        private ImmutableArray<IDiagnosticAnalyzer>? lazyAnalyzers;

        public AnalyzerFileReference(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException("fullPath");
            }

            // TODO: remove full path normalization
            CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

            try
            {
                this.fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "fullPath");
            }

            lazyAnalyzers = null;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers()
        {
            if (!lazyAnalyzers.HasValue)
            {
                lazyAnalyzers = MetadataCache.GetOrCreateAnalyzersFromFile(this);
            }

            return lazyAnalyzers.Value;
        }

        public override string FullPath
        {
            get
            {
                return this.fullPath;
            }
        }

        public override string Display
        {
            get
            {
                if (displayName == null)
                {
                    try
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(this.FullPath);
                        displayName = assemblyName.Name;
                        return displayName;
                    }
                    catch (ArgumentException)
                    { }
                    catch (BadImageFormatException)
                    { }
                    catch (SecurityException)
                    { }
                    catch (FileLoadException)
                    { }
                    catch (FileNotFoundException)
                    { }

                    displayName = base.Display;
                }

                return displayName;
            }
        }

        /// <summary>
        /// Returns the <see cref="ImmutableArray{T}"/> of <see cref="IDiagnosticAnalyzer"/> defined in the given <paramref name="analyzerAssemblies"/>.
        /// </summary>
        public static ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers(ImmutableArray<AnalyzerFileReference> analyzerAssemblies)
        {
            var builder = ImmutableArray.CreateBuilder<IDiagnosticAnalyzer>();

            foreach (var analyzerAssembly in analyzerAssemblies)
            {
                analyzerAssembly.AddAnalyzers(builder, diagnosticsOpt: null, messageProviderOpt: null);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="IDiagnosticAnalyzer"/> defined in this assembly reference.
        /// </summary>
        internal void AddAnalyzers(ImmutableArray<IDiagnosticAnalyzer>.Builder builder, List<DiagnosticInfo> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            // We handle loading of analyzer assemblies ourselves. This allows us to avoid locking the assembly
            // file on disk.
            Type[] types = null;
            Exception ex = null;

            try
            {
                Assembly analyzerAssembly = InMemoryAssemblyLoader.Load(fullPath);
                types = analyzerAssembly.GetTypes();
            }
            catch (FileLoadException e)
            { ex = e; }
            catch (BadImageFormatException e)
            { ex = e; }
            catch (SecurityException e)
            { ex = e; }
            catch (ArgumentException e)
            { ex = e; }
            catch (PathTooLongException e)
            { ex = e; }
            catch (ReflectionTypeLoadException e)
            { ex = e; }

            if (ex != null)
            {
                var typeLoadEx = ex as ReflectionTypeLoadException;
                if (diagnosticsOpt != null && messageProviderOpt != null)
                {
                    var message = typeLoadEx == null ?
                        messageProviderOpt.WRN_UnableToLoadAnalyzer :
                        messageProviderOpt.INF_UnableToLoadSomeTypesInAnalyzer;
                    diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, message, fullPath, ex.Message));
                }

                if (typeLoadEx != null)
                {
                    types = typeLoadEx.Types.Where(t => t != null).ToArray();
                }
                else
                {
                    return;
                }
            }

            bool hasAnalyzers = false;
            foreach (var type in types)
            {
                try
                {
                    if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDiagnosticAnalyzer)) && type.IsDefined(typeof(DiagnosticAnalyzerAttribute)))
                    {
                        hasAnalyzers = true;
                        builder.Add((IDiagnosticAnalyzer)Activator.CreateInstance(type));
                    }
                }
                catch (Exception e)
                {
                    if (diagnosticsOpt != null && messageProviderOpt != null)
                    {
                        diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.WRN_AnalyzerCannotBeCreated, type.FullName, fullPath, e.Message));
                    }
                }
            }

            if (!hasAnalyzers && ex == null && diagnosticsOpt != null && messageProviderOpt != null)
            {
                // If there are no analyzers in this assembly, let the user know.
                diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.WRN_NoAnalyzerInAssembly, fullPath));
            }
        }

        public override bool Equals(object obj)
        {
            AnalyzerFileReference other = obj as AnalyzerFileReference;

            if (other != null)
            {
                return other.Display == this.Display &&
                       other.FullPath == this.FullPath &&
                       other.IsUnresolved == this.IsUnresolved;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Display,
                        Hash.Combine(this.FullPath, this.IsUnresolved.GetHashCode()));
        }
    }
}
