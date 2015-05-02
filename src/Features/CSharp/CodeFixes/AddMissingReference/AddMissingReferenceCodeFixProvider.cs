// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingReference;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddMissingReference
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddMissingReference), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.SimplifyNames)]
    internal class AddMissingReferenceCodeFixProvider : CodeFixProvider, ReportCrashDumpsToMicrosoft
    {
        private const string CS0012 = "CS0012"; // The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'ProjectA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0012); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var uniqueIdentities = new HashSet<AssemblyIdentity>();
            foreach (var diagnostic in context.Diagnostics)
            {
                AssemblyIdentity identity;
                if (HACK_TryGetMissingAssemblyIdentity(diagnostic.GetMessage(), out identity) && !uniqueIdentities.Contains(identity))
                {
                    uniqueIdentities.Add(identity);
                    context.RegisterCodeFix(
                        await AddMissingReferenceCodeAction.CreateAsync(context.Document.Project, identity, context.CancellationToken).ConfigureAwait(false),
                        diagnostic);
                }
            }
        }

        private bool HACK_TryGetMissingAssemblyIdentity(string message, out AssemblyIdentity assemblyIdentity)
        {
            return AssemblyIdentity.TryParseDisplayName(message.Split('\'')[3], out assemblyIdentity);
        }
    }
}
