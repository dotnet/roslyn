' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

#If False Then
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Roslyn.Services.Editor.VisualBasic.UnitTests.Organizing
    Public Class OrganizeModifiersTests
        Inherits AbstractOrganizerTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestMethods1()
            Dim initial =
<element>class C
  shared public sub F()
  end sub
end class</element>.Value
            Dim final =
<element>class C
  public shared sub F()
  end sub
end class</element>.Value

            Check(initial, final)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestMethods2()
            Dim initial =
<element>class D
  public shared sub S()
  end sub
end class</element>.Value
            Dim final =
<element>class D
  public shared sub S()
  end sub
end class</element>.Value

            Check(initial, final)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestMethods3()

            Dim initial =
<element>class E
  public shared partial sub S()
  end sub
end class</element>.Value
            Dim final =
<element>class E
  public shared partial sub S()
  end sub
end class</element>.Value

            Check(initial, final)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Sub TestMethods4()

            Dim initial =
<element>class F
  shared public partial sub S()
  end sub
end class</element>.Value
            Dim final =
<element>class F
  public shared partial sub S()
  end sub
end class</element>.Value

            Check(initial, final)
        End Sub
    End Class
End Namespace
#End If
