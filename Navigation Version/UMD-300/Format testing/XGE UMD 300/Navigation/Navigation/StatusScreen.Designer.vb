<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class StatusScreen
    Inherits System.Windows.Forms.Form

    'Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.  
    'Das Bearbeiten mit dem Code-Editor ist nicht möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        DateTime = New Timer(components)
        Label1 = New Label()
        LblTime = New Label()
        Window_Bar = New Panel()
        LblStatus = New Label()
        LOAD_GIF = New BufferedPictureBox()
        Window_Bar.SuspendLayout()
        CType(LOAD_GIF, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' DateTime
        ' 
        DateTime.Enabled = True
        ' 
        ' Label1
        ' 
        Label1.BackColor = Color.Transparent
        Label1.Font = New Font("Adam Medium", 35.9999962F, FontStyle.Regular, GraphicsUnit.Point)
        Label1.ForeColor = Color.White
        Label1.Location = New Point(23, 17)
        Label1.Name = "Label1"
        Label1.Size = New Size(980, 45)
        Label1.TabIndex = 2
        Label1.Text = "Navigation"
        ' 
        ' LblTime
        ' 
        LblTime.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        LblTime.BackColor = Color.Transparent
        LblTime.Font = New Font("Adam Medium", 35.9999962F, FontStyle.Regular, GraphicsUnit.Point)
        LblTime.ForeColor = Color.White
        LblTime.Location = New Point(1595, 16)
        LblTime.Name = "LblTime"
        LblTime.Size = New Size(313, 40)
        LblTime.TabIndex = 3
        LblTime.Text = "22:31"
        LblTime.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' Window_Bar
        ' 
        Window_Bar.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Window_Bar.BackColor = Color.Transparent
        Window_Bar.BackgroundImage = My.Resources.Resources.NAV_BAR
        Window_Bar.BackgroundImageLayout = ImageLayout.Stretch
        Window_Bar.Controls.Add(LblTime)
        Window_Bar.Controls.Add(Label1)
        Window_Bar.Location = New Point(0, 0)
        Window_Bar.Name = "Window_Bar"
        Window_Bar.Size = New Size(1920, 75)
        Window_Bar.TabIndex = 1
        ' 
        ' LblStatus
        ' 
        LblStatus.BackColor = Color.Transparent
        LblStatus.Font = New Font("Adam Medium", 47.9999924F, FontStyle.Regular, GraphicsUnit.Point)
        LblStatus.ForeColor = Color.White
        LblStatus.Location = New Point(65, 130)
        LblStatus.Name = "LblStatus"
        LblStatus.Size = New Size(980, 267)
        LblStatus.TabIndex = 0
        LblStatus.Text = "Please insert the DVD with the Navigation Map Data inside the DVD-Drive."
        ' 
        ' LOAD_GIF
        ' 
        LOAD_GIF.BackColor = Color.Transparent
        LOAD_GIF.Image = My.Resources.Resources.LOAD
        LOAD_GIF.Location = New Point(70, 120)
        LOAD_GIF.Name = "LOAD_GIF"
        LOAD_GIF.Size = New Size(70, 70)
        LOAD_GIF.SizeMode = PictureBoxSizeMode.StretchImage
        LOAD_GIF.TabIndex = 2
        LOAD_GIF.TabStop = False
        ' 
        ' StatusScreen
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        BackgroundImage = My.Resources.Resources.NAV_BCG1
        BackgroundImageLayout = ImageLayout.Stretch
        ClientSize = New Size(1920, 1080)
        Controls.Add(LOAD_GIF)
        Controls.Add(Window_Bar)
        Controls.Add(LblStatus)
        DoubleBuffered = True
        FormBorderStyle = FormBorderStyle.None
        Name = "StatusScreen"
        StartPosition = FormStartPosition.CenterScreen
        Text = "StatusScreen"
        WindowState = FormWindowState.Maximized
        Window_Bar.ResumeLayout(False)
        CType(LOAD_GIF, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub
    Friend WithEvents DateTime As Timer
    Friend WithEvents Label1 As Label
    Friend WithEvents LblTime As Label
    Friend WithEvents Window_Bar As Panel
    Friend WithEvents LblStatus As Label
    Friend WithEvents LOAD_GIF As BufferedPictureBox
End Class
