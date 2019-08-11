// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // This part contains all the logic for hooking up the CodeRefactoring to the CodeStyleProvider.
    // All the code in this part is an implementation detail and is intentionally private so that
    // subclasses cannot change anything.  All code relevant to subclasses relating to refactorings
    // is contained in AbstractCodeStyleProvider.cs

    internal abstract partial class AbstractCodeStyleProvider<TOptionKind, TCodeStyleProvider>
    {
        private async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var optionValue = optionSet.GetOption(_option);

            var severity = GetOptionSeverity(optionValue);
            switch (severity)
            {
                case ReportDiagnostic.Suppress:
                case ReportDiagnostic.Hidden:
                    // if the severity is Hidden that's equivalent to 'refactoring only', so we want
                    // to try to compute the refactoring here.
                    //
                    // If the severity is 'suppress', that means the user doesn't want the actual
                    // analyzer to run here.  However, we can still check to see if we could offer
                    // the feature here as a refactoring.
                    await ComputeRefactoringsAsync(context, optionValue.Value, analyzerActive: false).ConfigureAwait(false);
                    return;

                case ReportDiagnostic.Error:
                case ReportDiagnostic.Warn:
                case ReportDiagnostic.Info:
                    // User has this option set at a level where we want it checked by the
                    // DiagnosticAnalyser and not the CodeRefactoringProvider.  However, we still
                    // want to check if we want to offer the *reverse* refactoring here in this
                    // single location.
                    //
                    // For example, say this is the "use expression body" feature.  If the user says
                    // they always prefer expression-bodies (with warning level), then we want the
                    // analyzer to always be checking for that.  However, we still want to offer the
                    // refactoring to flip their code to use a block body here, just in case that
                    // was something they wanted to do as a one off (i.e. before adding new
                    // statements.
                    //
                    // TODO(cyrusn): Should we only do this for warn/info?  Argument could be made
                    // that we shouldn't even offer to refactor in the reverse direction if it will
                    // just cause an error.  That said, maybe this is just an intermediary step, and
                    // we shouldn't really be blocking the user from making it.
                    await ComputeRefactoringsAsync(context, optionValue.Value, analyzerActive: true).ConfigureAwait(false);
                    return;
            }
        }

        private async Task ComputeRefactoringsAsync(
            CodeRefactoringContext context, TOptionKind option, bool analyzerActive)
        {
            var (document, span, cancellationToken) = context;

            var computationTask = analyzerActive
                ? ComputeOpposingRefactoringsWhenAnalyzerActiveAsync(document, span, option, cancellationToken)
                : ComputeAllRefactoringsWhenAnalyzerInactiveAsync(document, span, cancellationToken);

            var codeActions = await computationTask.ConfigureAwait(false);
            context.RegisterRefactorings(codeActions);
        }

        public class CodeRefactoringProvider : CodeRefactorings.CodeRefactoringProvider
        {
            public readonly TCodeStyleProvider _codeStyleProvider;

            protected CodeRefactoringProvider()
            {
                _codeStyleProvider = new TCodeStyleProvider();
            }

            public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
                => _codeStyleProvider.ComputeRefactoringsAsync(context);
        }
    }
}
