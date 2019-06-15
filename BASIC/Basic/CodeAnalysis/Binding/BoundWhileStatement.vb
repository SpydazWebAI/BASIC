﻿Option Explicit On
Option Strict On
Option Infer On

Namespace Global.Basic.CodeAnalysis.Binding

  Friend NotInheritable Class BoundWhileStatement
    Inherits BoundStatement

    Sub New(condition As BoundExpression, body As BoundStatement)
      Me.Condition = condition
      Me.Body = body
    End Sub

    Public Overrides ReadOnly Property Kind As BoundNodeKind = BoundNodeKind.WhileStatement
    Public ReadOnly Property Condition As BoundExpression
    Public ReadOnly Property Body As BoundStatement

  End Class

End Namespace