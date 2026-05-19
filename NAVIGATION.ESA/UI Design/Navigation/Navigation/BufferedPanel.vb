Public Class BufferedPanel
    Inherits Panel

    Public Sub New()
        Me.SetStyle(
            ControlStyles.AllPaintingInWmPaint Or
            ControlStyles.UserPaint Or
            ControlStyles.OptimizedDoubleBuffer Or
            ControlStyles.ResizeRedraw, True)
        Me.UpdateStyles()
    End Sub
End Class