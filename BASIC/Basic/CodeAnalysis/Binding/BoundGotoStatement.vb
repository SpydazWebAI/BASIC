﻿Option Explicit On
Option Strict On
Option Infer On

Namespace Global.Basic.CodeAnalysis.Binding

  Friend NotInheritable Class BoundGotoStatement
    Inherits BoundStatement

    Sub New(label As LabelSymbol)
      Me.Label = label
    End Sub

    Public Overrides ReadOnly Property Kind As BoundNodeKind = BoundNodeKind.GotoStatement
    Public ReadOnly Property Label As LabelSymbol

  End Class

End Namespace