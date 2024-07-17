// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages
{
    internal static class EmbeddedLanguagesTestConstants
    {
        public const string StringSyntaxAttributeCodeCSharp = """

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
                public sealed class StringSyntaxAttribute : Attribute
                {
                    public StringSyntaxAttribute(string syntax)
                    {
                        Syntax = syntax;
                        Arguments = Array.Empty<object?>();
                    }

                    public StringSyntaxAttribute(string syntax, params object?[] arguments)
                    {
                        Syntax = syntax;
                        Arguments = arguments;
                    }

                    public string Syntax { get; }
                    public object?[] Arguments { get; }

                    public const string DateTimeFormat = nameof(DateTimeFormat);
                    public const string Json = nameof(Json);
                    public const string Regex = nameof(Regex);
                }
            }
            """;

        public const string StringSyntaxAttributeCodeVB = """

            Namespace System.Diagnostics.CodeAnalysis
                <AttributeUsage(AttributeTargets.Parameter Or AttributeTargets.Field Or AttributeTargets.Property, AllowMultiple:=False, Inherited:=False)>
                Public NotInheritable Class StringSyntaxAttribute
                    Inherits Attribute

                    Public Sub New(syntax As String)
                        Me.Syntax = syntax
                        Arguments = Array.Empty(Of Object)()
                    End Sub

                    Public Sub New(syntax As String, ParamArray arguments As Object())
                        Me.Syntax = syntax
                        Me.Arguments = arguments
                    End Sub

                    Public ReadOnly Property Syntax As String
                    Public ReadOnly Property Arguments As Object()

                    Public Const DateTimeFormat As String = NameOf(DateTimeFormat)
                    Public Const Json As String = NameOf(Json)
                    Public Const Regex As String = NameOf(Regex)
                End Class
            End Namespace

            """;

        public static readonly string StringSyntaxAttributeCodeCSharpXml = SecurityElement.Escape(StringSyntaxAttributeCodeCSharp);
        public static readonly string StringSyntaxAttributeCodeVBXml = SecurityElement.Escape(StringSyntaxAttributeCodeVB);
    }
}
