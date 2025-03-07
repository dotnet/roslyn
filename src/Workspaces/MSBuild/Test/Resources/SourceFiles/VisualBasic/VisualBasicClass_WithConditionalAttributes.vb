' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

<Assembly: MyAttribute()> 

''' <summary>
''' This is a Visual Basic Class
''' </summary>
''' <remarks></remarks>
Public Class VisualBasicClass
    Inherits CSharpProject.CSharpClass

End Class

<Diagnostics.Conditional("EnableMyAttribute"), AttributeUsage(AttributeTargets.All, AllowMultiple:=True)> Class MyAttribute
    Inherits Attribute
End Class
