using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [ExportLanguageService(typeof(IUseAutoPropertyService), LanguageNames.CSharp), Shared]
    internal class CSharpUseAutoPropertyService : IUseAutoPropertyService
    {
        public SyntaxToken OnTokenRenamed(SyntaxToken oldToken, SyntaxToken newToken)
        {
            var parent = oldToken.Parent as ExpressionSyntax;
            if (parent != null)
            {
                if (parent.IsRightSideOfDot())
                {
                    parent = parent.Parent as ExpressionSyntax;
                }

                if (parent != null && 
                    parent.Parent.IsKind(SyntaxKind.Argument) && 
                    ((ArgumentSyntax)parent.Parent).RefOrOutKeyword.Kind() != SyntaxKind.None)
                {
                    // We accessed the field in a ref/out location.  Add a conflict annotation so the
                    // usre knows there's a problem here.
                    return newToken.WithAdditionalAnnotations(ConflictAnnotation.Create(CSharpFeaturesResources.ConflictsDetected));
                }
            }

            return newToken;
        }
    }
}