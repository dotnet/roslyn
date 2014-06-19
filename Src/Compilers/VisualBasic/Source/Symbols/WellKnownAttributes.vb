Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.MetadataReader

Namespace Roslyn.Compilers.VisualBasic

    Friend Class WellKnownAttributes
        ' compares by namespace and type name, ignores signatures
        Private Shared Function EarlyDecodeIsTargetAttribute(attributeType As NamedTypeSymbol, description As AttributeDescription) As Boolean
            Debug.Assert(Not attributeType.IsErrorType())
            Dim actualNamespaceName As String = attributeType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)
            Dim options As StringComparison = If(description.MatchIgnoringCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)
            Return actualNamespaceName.Equals(description.[Namespace], options) AndAlso attributeType.Name.Equals(description.Name, options)
        End Function

        Public Shared Function EarlyDecodeIsAttributeUsage(attributeType As NamedTypeSymbol, attributeSyntax As AttributeSyntax) As Boolean
            Return attributeSyntax.ArgumentList IsNot Nothing AndAlso EarlyDecodeIsTargetAttribute(attributeType, AttributeDescription.AttributeUsageAttribute)
        End Function

        Public Shared Function EarlyDecodeIsComImportAttribute(attributeType As NamedTypeSymbol, attributeSyntax As AttributeSyntax) As Boolean
            Return (attributeSyntax.ArgumentList Is Nothing OrElse Not attributeSyntax.ArgumentList.Arguments.Any()) AndAlso EarlyDecodeIsTargetAttribute(attributeType, AttributeDescription.ComImportAttribute)
        End Function

        Public Shared Function EarlyDecodeIsCaseInsensitiveExtensionAttribute(attributeType As NamedTypeSymbol, attributeSyntax As AttributeSyntax) As Boolean
            Return (attributeSyntax.ArgumentList Is Nothing OrElse Not attributeSyntax.ArgumentList.Arguments.Any()) AndAlso EarlyDecodeIsTargetAttribute(attributeType, AttributeDescription.CaseInsensitiveExtensionAttribute)
        End Function

        Public Shared Function EarlyDecodeIsConditionalAttribute(attributeType As NamedTypeSymbol, attributeSyntax As AttributeSyntax) As Boolean
            Return (attributeSyntax.ArgumentList IsNot Nothing AndAlso attributeSyntax.ArgumentList.Arguments.Count = 1) AndAlso EarlyDecodeIsTargetAttribute(attributeType, AttributeDescription.ConditionalAttribute)
        End Function
    End Class
End Namespace
