﻿Option Explicit On
Option Strict On
Option Infer On

Imports System.Collections.Immutable
Imports Basic.CodeAnalysis.Syntax

Namespace Global.Basic.CodeAnalysis.Binding

  Friend NotInheritable Class BoundGlobalScope

    Sub New(previous As BoundGlobalScope, diagnostics As ImmutableArray(Of Diagnostic), variables As ImmutableArray(Of VariableSymbol), expression As BoundExpression)
      Me.Previous = previous
      Me.Diagnostics = diagnostics
      Me.Variables = variables
      Me.Expression = expression
    End Sub

    Public ReadOnly Property Previous As BoundGlobalScope
    Public ReadOnly Property Diagnostics As ImmutableArray(Of Diagnostic)
    Public ReadOnly Property Variables As ImmutableArray(Of VariableSymbol)
    Public ReadOnly Property Expression As BoundExpression

  End Class

  Friend NotInheritable Class Binder

    Private m_scope As BoundScope

    Public Sub New(parent As BoundScope)
      Me.m_scope = New BoundScope(parent)
    End Sub

    Public Shared Function BindGlobalScope(previous As BoundGlobalScope, syntax As CompilationUnitSyntax) As BoundGlobalScope

      Dim parentScope = CreateParentScopes(previous)
      Dim binder = New Binder(parentScope)
      Dim expression = binder.BindExpression(syntax.Expression)
      Dim variables = binder.m_scope.GetDeclaredVariables
      Dim diagnostics = binder.Diagnostics.ToImmutableArray

      If previous IsNot Nothing Then
        diagnostics = diagnostics.InsertRange(0, previous.Diagnostics)
      End If

      Return New BoundGlobalScope(previous, diagnostics, variables, expression)

    End Function

    Private Shared Function CreateParentScopes(previous As BoundGlobalScope) As BoundScope

      Dim stack = New Stack(Of BoundGlobalScope)

      While previous IsNot Nothing
        stack.Push(previous)
        previous = previous.Previous
      End While

      ' submission 3 -> submission 2 -> submission 1

      Dim parent As BoundScope = Nothing

      While stack.Count > 0
        previous = stack.Pop
        Dim scope = New BoundScope(parent)
        For Each v In previous.Variables
          scope.TryDeclare(v)
        Next
        parent = scope
      End While

      Return parent

    End Function

    Public ReadOnly Property Diagnostics As DiagnosticBag = New DiagnosticBag

    Public Function BindExpression(syntax As ExpressionSyntax) As BoundExpression

      Select Case syntax.Kind
        Case SyntaxKind.ParenExpression
          Return Me.BindParenExpression(DirectCast(syntax, ParenExpressionSyntax))
        Case SyntaxKind.LiteralExpression
          Return Me.BindLiteralEpression(DirectCast(syntax, LiteralExpressionSyntax))
        Case SyntaxKind.NameExpression
          Return Me.BindNameExpression(DirectCast(syntax, NameExpressionSyntax))
        Case SyntaxKind.AssignmentExpression
          Return Me.BindAssignmentExpression(DirectCast(syntax, AssignmentExpressionSyntax))
        Case SyntaxKind.UnaryExpression
          Return Me.BindUnaryEpression(DirectCast(syntax, UnaryExpressionSyntax))
        Case SyntaxKind.BinaryExpression
          Return Me.BindBinaryEpression(DirectCast(syntax, BinaryExpressionSyntax))
        Case Else
          Throw New Exception($"Unexpected syntax {syntax.Kind}")
      End Select

    End Function

    Private Function BindParenExpression(syntax As ParenExpressionSyntax) As BoundExpression
      Return Me.BindExpression(syntax.Expression)
    End Function

    Private Function BindLiteralEpression(syntax As LiteralExpressionSyntax) As BoundExpression
      Dim value = If(syntax.Value, 0)
      Return New BoundLiteralExpression(value)
    End Function

    Private Function BindNameExpression(syntax As NameExpressionSyntax) As BoundExpression
      Dim name = syntax.IdentifierToken.Text
      Dim variable As VariableSymbol = Nothing
      If Not Me.m_scope.TryLookup(name, variable) Then
        Me.Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name)
        Return New BoundLiteralExpression(0)
      End If
      Return New BoundVariableExpression(variable)
    End Function

    Private Function BindAssignmentExpression(syntax As AssignmentExpressionSyntax) As BoundExpression

      Dim name = syntax.IdentifierToken.Text
      Dim boundExpression = Me.BindExpression(syntax.Expression)

      Dim variable As VariableSymbol = Nothing
      If Not Me.m_scope.TryLookup(name.ToLower, variable) Then
        variable = New VariableSymbol(name.ToLower, boundExpression.Type)
        Me.m_scope.TryDeclare(variable)
      End If

      'If Not Me.m_scope.TryDeclare(variable) Then
      '  Me.Diagnostics.ReportVariableAlreadyDeclared(syntax.IdentifierToken.Span, name)
      'End If

      If boundExpression.Type IsNot variable.Type Then
        Me.Diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type)
        Return boundExpression
      End If

      Return New BoundAssignmentExpression(variable, boundExpression)

    End Function

    Private Function BindUnaryEpression(syntax As UnaryExpressionSyntax) As BoundExpression
      Dim boundOperand = Me.BindExpression(syntax.Operand)
      Dim boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type)
      If boundOperator Is Nothing Then
        Me.Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type)
        Return boundOperand
      End If
      Return New BoundUnaryExpression(boundOperator, boundOperand)
    End Function

    Private Function BindBinaryEpression(syntax As BinaryExpressionSyntax) As BoundExpression
      Dim boundLeft = Me.BindExpression(syntax.Left)
      Dim boundRight = Me.BindExpression(syntax.Right)
      Dim boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type)
      If boundOperator Is Nothing Then
        Me.Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type)
        Return boundLeft
      End If
      Return New BoundBinaryExpression(boundLeft, boundOperator, boundRight)
    End Function

  End Class

End Namespace