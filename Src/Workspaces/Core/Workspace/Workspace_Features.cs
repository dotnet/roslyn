// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Workspace
    {
        public Workspace(
            FeaturePack features,
            string workspaceKind)
            : this(
                GetWorkspaceServiceProviderFactory(features).CreateWorkspaceServiceProvider(workspaceKind))
        {
        }

        private static ConditionalWeakTable<FeaturePack, IWorkspaceServiceProviderFactory> factories
            = new ConditionalWeakTable<FeaturePack, IWorkspaceServiceProviderFactory>();

        internal static IWorkspaceServiceProviderFactory GetWorkspaceServiceProviderFactory(FeaturePack features)
        {
            IWorkspaceServiceProviderFactory factory;
            if (!factories.TryGetValue(features, out factory))
            {
                factory = factories.GetValue(features, CreateWorkspaceServiceProviderFactory);
            }

            return factory;
        }

        private static IWorkspaceServiceProviderFactory CreateWorkspaceServiceProviderFactory(FeaturePack features)
        {
            var exports = features.ComposeExports();
            var factory = exports.GetExports<IWorkspaceServiceProviderFactory>().Single().Value;

#if MEF
            // need to tell factory about export source since it is constructed via MEF and the export source is not part of the MEF composition.
            ((WorkspaceServiceProviderFactory)factory).SetExports(exports);
#endif

            return factory;
        }
    }
}