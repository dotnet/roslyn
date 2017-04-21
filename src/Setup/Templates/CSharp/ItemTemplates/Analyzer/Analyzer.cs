using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace $rootnamespace$
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class $safeitemname$ : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "$safeitemname$";
        internal static readonly LocalizableString Title = "$safeitemname$ Title";
        internal static readonly LocalizableString MessageFormat = "$safeitemname$ '{0}'";
        internal const string Category = "$safeitemname$ Category";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}