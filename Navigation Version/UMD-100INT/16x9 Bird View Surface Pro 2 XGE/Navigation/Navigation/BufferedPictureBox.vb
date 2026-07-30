Public Class BufferedPictureBox
    Inherits PictureBox

    Public Sub New()
        Me.SetStyle(
            ControlStyles.AllPaintingInWmPaint Or
            ControlStyles.UserPaint Or
            ControlStyles.OptimizedDoubleBuffer Or
            ControlStyles.SupportsTransparentBackColor Or
            ControlStyles.ResizeRedraw, True)
        Me.UpdateStyles()
    End Sub
End Class