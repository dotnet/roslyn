' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This is a binder for use when early decoding of well known attributes. The binder will only bind expressions that can appear in an attribute.
    ''' Its purpose is to allow a symbol to safely decode any attribute without the possibility of any attribute related infinite recursion during binding.
    ''' If an attribute and its arguments are valid then this binder returns a BoundAttributeExpression otherwise it returns a BadExpression.
    ''' </summary>
    Friend NotInheritable Class EarlyWellKnownAttributeBinder
        Inherits Binder

        Private ReadOnly _owner As Symbol
        Friend Sub New(owner As Symbol, containingBinder As Binder)
            MyBase.New(containingBinder, isEarlyAttributeBinder:=True)
            Me._owner = owner
        End Sub

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return If(_owner, MyBase.ContainingMember)
            End Get
        End Property

        ' This binder is only used to bind expressions in an attribute context.
        Public Overrides ReadOnly Property BindingLocation As BindingLocation
            Get
                Return BindingLocation.Attribute
            End Get
        End Property

        ' Hide the GetAttribute overload which takes a diagnostic bag.
        ' This ensures that diagnostics from the early bound attributes are never preserved.
        Friend Shadows Function GetAttribute(node As AttributeSyntax, boundAttributeType As NamedTypeSymbol, <Out> ByRef generatedDiagnostics As Boolean) As SourceAttributeData
            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim earlyAttribute = MyBase.GetAttribute(node, boundAttributeType, diagnostics)
            generatedDiagnostics = Not diagnostics.IsEmptyWithoutResolution()
            diagnostics.Free()
            Return earlyAttribute
        End Function

        ''' <summary>
        ''' Check that the syntax can appear in an attribute argument.
        ''' </summary>
        Friend Shared Function CanBeValidAttributeArgument(node As ExpressionSyntax, memberAccessBinder As Binder) As Boolean
            Debug.Assert(node IsNot Nothing)

            ' 11.2 Constant Expressions
            '
            'A constant expression is an expression whose value can be fully evaluated at compile time. The type of a constant expression can be Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Char, Single, Double, Decimal, Date, Boolean, String, Object, or any enumeration type. The following constructs are permitted in constant expressions:
            '
            '         Literals (including Nothing).
            '         References to constant type members or constant locals.
            '         References to members of enumeration types.
            '         Parenthesized subexpressions.
            '         Coercion expressions, provided the target type is one of the types listed above. Coercions to and from String are an exception to this rule and are only allowed on null values because String conversions are always done in the current culture of the execution environment at run time. Note that constant coercion expressions can only ever use intrinsic conversions.
            '         The +, – and Not unary operators, provided the operand and result is of a type listed above.
            '         The +, –, *, ^, Mod, /, \, <<, >>, &, And, Or, Xor, AndAlso, OrElse, =, <, >, <>, <=, and => binary operators, provided each operand and result is of a type listed above.
            '         The conditional operator If, provided each operand and result is of a type listed above.
            '         The following run-time functions:
            '            Microsoft.VisualBasic.Strings.ChrW
            '            Microsoft.VisualBasic.Strings.Chr, if the constant value is between 0 and 128
            '            Microsoft.VisualBasic.Strings.AscW, if the constant string is not empty
            '            Microsoft.VisualBasic.Strings.Asc, if the constant string is not empty
            '
            ' In addition, attributes allow array expressions including both array literal and array creation expressions as well as GetType expressions.

            Select Case node.Kind

                Case _
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxKind.StringLiteralExpression,
                    SyntaxKind.CharacterLiteralExpression,
                    SyntaxKind.TrueLiteralExpression,
                    SyntaxKind.FalseLiteralExpression,
                    SyntaxKind.NothingLiteralExpression,
                    SyntaxKind.DateLiteralExpression
                    ' Literals (including Nothing).
                    Return True

                Case _
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxKind.GlobalName,
                    SyntaxKind.IdentifierName
                    ' References to constant type members or constant locals.
                    ' References to members of enumeration types.
                    Return True

                Case SyntaxKind.ParenthesizedExpression
                    ' Parenthesized subexpressions.
                    Return True

                Case _
                    SyntaxKind.CTypeExpression,
                    SyntaxKind.TryCastExpression,
                    SyntaxKind.DirectCastExpression,
                    SyntaxKind.PredefinedCastExpression
                    ' Coercion expressions, provided the target type is one of the types listed above. 
                    ' Coercions to and from String are an exception to this rule and are only allowed on null values
                    ' because String conversions are always done in the current culture of the execution environment
                    ' at run time. Note that constant coercion expressions can only ever use intrinsic conversions.
                    Return True

                Case _
                    SyntaxKind.UnaryPlusExpression,
                    SyntaxKind.UnaryMinusExpression,
                    SyntaxKind.NotExpression
                    ' The +, – and Not unary operators, provided the operand and result is of a type listed above.
                    Return True

                Case _
                    SyntaxKind.AddExpression,
                    SyntaxKind.SubtractExpression,
                    SyntaxKind.MultiplyExpression,
                    SyntaxKind.ExponentiateExpression,
                    SyntaxKind.DivideExpression,
                    SyntaxKind.ModuloExpression,
                    SyntaxKind.IntegerDivideExpression,
                    SyntaxKind.LeftShiftExpression,
                    SyntaxKind.RightShiftExpression,
                    SyntaxKind.ConcatenateExpression,
                    SyntaxKind.AndExpression,
                    SyntaxKind.OrExpression,
                    SyntaxKind.ExclusiveOrExpression,
                    SyntaxKind.AndAlsoExpression,
                    SyntaxKind.OrElseExpression,
                    SyntaxKind.EqualsExpression,
                    SyntaxKind.NotEqualsExpression,
                    SyntaxKind.LessThanOrEqualExpression,
                    SyntaxKind.GreaterThanOrEqualExpression,
                    SyntaxKind.LessThanExpression,
                    SyntaxKind.GreaterThanExpression
                    ' The +, –, *, ^, Mod, /, \, <<, >>, &, And, Or, Xor, AndAlso, OrElse, =, <, >, <>, <=, and => binary operators,
                    ' provided each operand and result is of a type listed above.
                    Return True

                Case _
                    SyntaxKind.BinaryConditionalExpression,
                    SyntaxKind.TernaryConditionalExpression
                    ' The conditional operator If, provided each operand and result is of a type listed above.
                    Return True

                Case SyntaxKind.InvocationExpression
                    ' The following run-time functions may appear in constant expressions:
                    '     Microsoft.VisualBasic.Strings.ChrW
                    '     Microsoft.VisualBasic.Strings.Chr, if the constant value is between 0 and 128
                    '     Microsoft.VisualBasic.Strings.AscW, if the constant string is not empty
                    '     Microsoft.VisualBasic.Strings.Asc, if the constant string is not empty

                    Dim memberAccess = TryCast(DirectCast(node, InvocationExpressionSyntax).Expression, MemberAccessExpressionSyntax)
                    If memberAccess IsNot Nothing Then
                        Dim diagnostics = DiagnosticBag.GetInstance
                        Dim boundExpression = memberAccessBinder.BindExpression(memberAccess, diagnostics)
                        diagnostics.Free()

                        If boundExpression.HasErrors Then
                            Return False
                        End If

                        Dim boundMethodGroup = TryCast(boundExpression, BoundMethodGroup)
                        If boundMethodGroup IsNot Nothing AndAlso boundMethodGroup.Methods.Length = 1 Then

                            Dim method = boundMethodGroup.Methods(0)

                            Dim compilation As VisualBasicCompilation = memberAccessBinder.Compilation
                            If method Is compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__ChrWInt32Char) OrElse
                                method Is compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__ChrInt32Char) OrElse
                                method Is compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscWCharInt32) OrElse
                                method Is compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscCharInt32) Then

                                Return True
                            End If

                        End If
                    End If

                    Return False

                Case SyntaxKind.CollectionInitializer,
                     SyntaxKind.ArrayCreationExpression,
                     SyntaxKind.GetTypeExpression
                    ' These are not constants and are special for attribute expressions.
                    ' SyntaxKind.CollectionInitializer in this case really means ArrayLiteral, i.e.{1, 2, 3}.
                    ' SyntaxKind.ArrayCreationExpression is array creation expression, i.e. new Char {'a'c, 'b'c}.
                    ' SyntaxKind.GetTypeExpression is a GetType expression, i.e. GetType(System.String).
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Friend Overrides Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            ' When early binding attributes, extension methods should always be ignored.
            Return ContainingBinder.BinderSpecificLookupOptions(options) Or LookupOptions.IgnoreExtensionMethods
        End Function

    End Class

End Namespace
