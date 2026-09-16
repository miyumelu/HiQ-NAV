<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainPage
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Side_Panel = New Panel()
        POP8 = New Label()
        POP7 = New Label()
        POP6 = New Label()
        POP5 = New Label()
        POP4 = New Label()
        POP3 = New Label()
        POP1 = New Label()
        POP2 = New Label()
        Panel1 = New Panel()
        Label1 = New Label()
        MAPMODE_BTN = New Label()
        HOME_BTN = New PictureBox()
        TIMEMODE_BTN = New PictureBox()
        SETTINGS_BTN = New PictureBox()
        NETWORKMODE_BTN = New PictureBox()
        FASTMODE_BTN = New PictureBox()
        COMPASSMODE_BTN = New PictureBox()
        ZOOMOUT_BTN = New PictureBox()
        ZOOMIN_BTN = New PictureBox()
        Map_Panel = New BufferedPanel()
        Time_Panel = New Panel()
        LblTime = New Label()
        VEHICLE_BOX = New BufferedPictureBox()
        NORTH_LBL = New Label()
        COMPASS_BOX = New BufferedPictureBox()
        ZOOM_SCALE = New Label()
        SysTimer = New Timer(components)
        Side_Panel.SuspendLayout()
        Panel1.SuspendLayout()
        CType(HOME_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(TIMEMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(SETTINGS_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(NETWORKMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(FASTMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(COMPASSMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(ZOOMOUT_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(ZOOMIN_BTN, ComponentModel.ISupportInitialize).BeginInit()
        Map_Panel.SuspendLayout()
        Time_Panel.SuspendLayout()
        CType(VEHICLE_BOX, ComponentModel.ISupportInitialize).BeginInit()
        CType(COMPASS_BOX, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Side_Panel
        ' 
        Side_Panel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        Side_Panel.BackgroundImage = My.Resources.Resources.NAV_PANEL1
        Side_Panel.BackgroundImageLayout = ImageLayout.Stretch
        Side_Panel.Controls.Add(POP8)
        Side_Panel.Controls.Add(POP7)
        Side_Panel.Controls.Add(POP6)
        Side_Panel.Controls.Add(POP5)
        Side_Panel.Controls.Add(POP4)
        Side_Panel.Controls.Add(POP3)
        Side_Panel.Controls.Add(POP1)
        Side_Panel.Controls.Add(POP2)
        Side_Panel.Controls.Add(Panel1)
        Side_Panel.Controls.Add(MAPMODE_BTN)
        Side_Panel.Controls.Add(HOME_BTN)
        Side_Panel.Controls.Add(TIMEMODE_BTN)
        Side_Panel.Controls.Add(SETTINGS_BTN)
        Side_Panel.Controls.Add(NETWORKMODE_BTN)
        Side_Panel.Controls.Add(FASTMODE_BTN)
        Side_Panel.Controls.Add(COMPASSMODE_BTN)
        Side_Panel.Controls.Add(ZOOMOUT_BTN)
        Side_Panel.Controls.Add(ZOOMIN_BTN)
        Side_Panel.Location = New Point(-305, 0)
        Side_Panel.Name = "Side_Panel"
        Side_Panel.Size = New Size(400, 900)
        Side_Panel.TabIndex = 0
        ' 
        ' POP8
        ' 
        POP8.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        POP8.AutoSize = True
        POP8.BackColor = Color.Transparent
        POP8.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP8.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP8.Location = New Point(15, 823)
        POP8.Name = "POP8"
        POP8.Size = New Size(165, 42)
        POP8.TabIndex = 17
        POP8.Text = "Settings"
        ' 
        ' POP7
        ' 
        POP7.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        POP7.AutoSize = True
        POP7.BackColor = Color.Transparent
        POP7.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP7.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP7.Location = New Point(15, 741)
        POP7.Name = "POP7"
        POP7.Size = New Size(126, 42)
        POP7.TabIndex = 16
        POP7.Text = "Home"
        ' 
        ' POP6
        ' 
        POP6.AutoSize = True
        POP6.BackColor = Color.Transparent
        POP6.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP6.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP6.Location = New Point(15, 519)
        POP6.Name = "POP6"
        POP6.Size = New Size(270, 42)
        POP6.TabIndex = 15
        POP6.Text = "Change View"
        ' 
        ' POP5
        ' 
        POP5.AutoSize = True
        POP5.BackColor = Color.Transparent
        POP5.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP5.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP5.Location = New Point(15, 434)
        POP5.Name = "POP5"
        POP5.Size = New Size(214, 42)
        POP5.TabIndex = 14
        POP5.Text = "Map Color"
        ' 
        ' POP4
        ' 
        POP4.AutoSize = True
        POP4.BackColor = Color.Transparent
        POP4.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP4.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP4.Location = New Point(12, 348)
        POP4.Name = "POP4"
        POP4.Size = New Size(232, 42)
        POP4.TabIndex = 13
        POP4.Text = "Quick Load"
        ' 
        ' POP3
        ' 
        POP3.AutoSize = True
        POP3.BackColor = Color.Transparent
        POP3.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP3.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP3.Location = New Point(15, 262)
        POP3.Name = "POP3"
        POP3.Size = New Size(188, 42)
        POP3.TabIndex = 12
        POP3.Text = "Compass"
        ' 
        ' POP1
        ' 
        POP1.AutoSize = True
        POP1.BackColor = Color.Transparent
        POP1.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP1.ForeColor = Color.FromArgb(CByte(111), CByte(255), CByte(190))
        POP1.Location = New Point(15, 93)
        POP1.Name = "POP1"
        POP1.Size = New Size(169, 42)
        POP1.TabIndex = 11
        POP1.Text = "Zoom In"
        ' 
        ' POP2
        ' 
        POP2.AutoSize = True
        POP2.BackColor = Color.Transparent
        POP2.Font = New Font("Adam Medium", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        POP2.ForeColor = Color.FromArgb(CByte(127), CByte(127), CByte(127))
        POP2.Location = New Point(15, 177)
        POP2.Name = "POP2"
        POP2.Size = New Size(202, 42)
        POP2.TabIndex = 1
        POP2.Text = "Zoom Out"
        ' 
        ' Panel1
        ' 
        Panel1.BackgroundImage = My.Resources.Resources.NAV_APP_BAR
        Panel1.BackgroundImageLayout = ImageLayout.Stretch
        Panel1.Controls.Add(Label1)
        Panel1.Location = New Point(0, 0)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(400, 64)
        Panel1.TabIndex = 10
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.BackColor = Color.Transparent
        Label1.Font = New Font("Adam Medium", 27.75F, FontStyle.Regular, GraphicsUnit.Point)
        Label1.ForeColor = Color.White
        Label1.Location = New Point(15, 15)
        Label1.Name = "Label1"
        Label1.Size = New Size(186, 36)
        Label1.TabIndex = 0
        Label1.Text = "Navigation"
        ' 
        ' MAPMODE_BTN
        ' 
        MAPMODE_BTN.BackColor = Color.Transparent
        MAPMODE_BTN.Font = New Font("SF UI Display Light", 27.75F, FontStyle.Regular, GraphicsUnit.Point)
        MAPMODE_BTN.ForeColor = Color.White
        MAPMODE_BTN.Location = New Point(323, 505)
        MAPMODE_BTN.Name = "MAPMODE_BTN"
        MAPMODE_BTN.Size = New Size(70, 70)
        MAPMODE_BTN.TabIndex = 4
        MAPMODE_BTN.Text = "3D"
        MAPMODE_BTN.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' HOME_BTN
        ' 
        HOME_BTN.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        HOME_BTN.BackColor = Color.Transparent
        HOME_BTN.BackgroundImage = My.Resources.Resources.HOME
        HOME_BTN.BackgroundImageLayout = ImageLayout.Zoom
        HOME_BTN.Location = New Point(325, 730)
        HOME_BTN.Name = "HOME_BTN"
        HOME_BTN.Size = New Size(60, 60)
        HOME_BTN.TabIndex = 9
        HOME_BTN.TabStop = False
        ' 
        ' TIMEMODE_BTN
        ' 
        TIMEMODE_BTN.BackColor = Color.Transparent
        TIMEMODE_BTN.BackgroundImage = My.Resources.Resources.DAY
        TIMEMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        TIMEMODE_BTN.Location = New Point(325, 422)
        TIMEMODE_BTN.Name = "TIMEMODE_BTN"
        TIMEMODE_BTN.Size = New Size(60, 60)
        TIMEMODE_BTN.TabIndex = 8
        TIMEMODE_BTN.TabStop = False
        ' 
        ' SETTINGS_BTN
        ' 
        SETTINGS_BTN.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        SETTINGS_BTN.BackColor = Color.Transparent
        SETTINGS_BTN.BackgroundImage = My.Resources.Resources.SETTINGS
        SETTINGS_BTN.BackgroundImageLayout = ImageLayout.Zoom
        SETTINGS_BTN.Location = New Point(325, 815)
        SETTINGS_BTN.Name = "SETTINGS_BTN"
        SETTINGS_BTN.Size = New Size(60, 60)
        SETTINGS_BTN.TabIndex = 7
        SETTINGS_BTN.TabStop = False
        ' 
        ' NETWORKMODE_BTN
        ' 
        NETWORKMODE_BTN.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        NETWORKMODE_BTN.BackColor = Color.Transparent
        NETWORKMODE_BTN.BackgroundImage = My.Resources.Resources.OFFLINE
        NETWORKMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        NETWORKMODE_BTN.Location = New Point(325, 645)
        NETWORKMODE_BTN.Name = "NETWORKMODE_BTN"
        NETWORKMODE_BTN.Size = New Size(60, 60)
        NETWORKMODE_BTN.TabIndex = 6
        NETWORKMODE_BTN.TabStop = False
        ' 
        ' FASTMODE_BTN
        ' 
        FASTMODE_BTN.BackColor = Color.Transparent
        FASTMODE_BTN.BackgroundImage = My.Resources.Resources.FAST_MODE
        FASTMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        FASTMODE_BTN.Location = New Point(325, 337)
        FASTMODE_BTN.Name = "FASTMODE_BTN"
        FASTMODE_BTN.Size = New Size(60, 60)
        FASTMODE_BTN.TabIndex = 5
        FASTMODE_BTN.TabStop = False
        ' 
        ' COMPASSMODE_BTN
        ' 
        COMPASSMODE_BTN.BackColor = Color.Transparent
        COMPASSMODE_BTN.BackgroundImage = My.Resources.Resources.COMPASS_MODE
        COMPASSMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        COMPASSMODE_BTN.Location = New Point(325, 252)
        COMPASSMODE_BTN.Name = "COMPASSMODE_BTN"
        COMPASSMODE_BTN.Size = New Size(60, 60)
        COMPASSMODE_BTN.TabIndex = 4
        COMPASSMODE_BTN.TabStop = False
        ' 
        ' ZOOMOUT_BTN
        ' 
        ZOOMOUT_BTN.BackColor = Color.Transparent
        ZOOMOUT_BTN.BackgroundImage = My.Resources.Resources.ZOOM_OUT
        ZOOMOUT_BTN.BackgroundImageLayout = ImageLayout.Zoom
        ZOOMOUT_BTN.Location = New Point(325, 167)
        ZOOMOUT_BTN.Name = "ZOOMOUT_BTN"
        ZOOMOUT_BTN.Size = New Size(60, 60)
        ZOOMOUT_BTN.TabIndex = 3
        ZOOMOUT_BTN.TabStop = False
        ' 
        ' ZOOMIN_BTN
        ' 
        ZOOMIN_BTN.BackColor = Color.Transparent
        ZOOMIN_BTN.BackgroundImage = My.Resources.Resources.ZOOM_IN
        ZOOMIN_BTN.BackgroundImageLayout = ImageLayout.Zoom
        ZOOMIN_BTN.Location = New Point(325, 82)
        ZOOMIN_BTN.Name = "ZOOMIN_BTN"
        ZOOMIN_BTN.Size = New Size(60, 60)
        ZOOMIN_BTN.TabIndex = 2
        ZOOMIN_BTN.TabStop = False
        ' 
        ' Map_Panel
        ' 
        Map_Panel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Map_Panel.BackColor = Color.FromArgb(CByte(34), CByte(34), CByte(34))
        Map_Panel.Controls.Add(Time_Panel)
        Map_Panel.Controls.Add(VEHICLE_BOX)
        Map_Panel.Controls.Add(NORTH_LBL)
        Map_Panel.Controls.Add(COMPASS_BOX)
        Map_Panel.Controls.Add(ZOOM_SCALE)
        Map_Panel.Location = New Point(95, 0)
        Map_Panel.Name = "Map_Panel"
        Map_Panel.Size = New Size(1500, 900)
        Map_Panel.TabIndex = 1
        ' 
        ' Time_Panel
        ' 
        Time_Panel.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Time_Panel.BackgroundImage = My.Resources.Resources.BOX
        Time_Panel.BackgroundImageLayout = ImageLayout.Stretch
        Time_Panel.Controls.Add(LblTime)
        Time_Panel.Location = New Point(1271, 25)
        Time_Panel.Name = "Time_Panel"
        Time_Panel.Size = New Size(203, 132)
        Time_Panel.TabIndex = 3
        ' 
        ' LblTime
        ' 
        LblTime.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        LblTime.BackColor = Color.Transparent
        LblTime.Font = New Font("Segoe UI", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        LblTime.ForeColor = Color.White
        LblTime.Location = New Point(15, 2)
        LblTime.Name = "LblTime"
        LblTime.Size = New Size(185, 71)
        LblTime.TabIndex = 4
        LblTime.Text = "22:31"
        LblTime.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' VEHICLE_BOX
        ' 
        VEHICLE_BOX.Anchor = AnchorStyles.None
        VEHICLE_BOX.BackColor = Color.Transparent
        VEHICLE_BOX.BackgroundImageLayout = ImageLayout.Zoom
        VEHICLE_BOX.Location = New Point(715, 415)
        VEHICLE_BOX.Name = "VEHICLE_BOX"
        VEHICLE_BOX.Size = New Size(70, 70)
        VEHICLE_BOX.TabIndex = 2
        VEHICLE_BOX.TabStop = False
        ' 
        ' NORTH_LBL
        ' 
        NORTH_LBL.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        NORTH_LBL.BackColor = Color.FromArgb(CByte(38), CByte(38), CByte(38))
        NORTH_LBL.Font = New Font("Segoe UI", 35.0F, FontStyle.Regular, GraphicsUnit.Point)
        NORTH_LBL.ForeColor = Color.White
        NORTH_LBL.Location = New Point(490, 766)
        NORTH_LBL.Name = "NORTH_LBL"
        NORTH_LBL.Size = New Size(51, 66)
        NORTH_LBL.TabIndex = 1
        NORTH_LBL.Text = "N"
        NORTH_LBL.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' COMPASS_BOX
        ' 
        COMPASS_BOX.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        COMPASS_BOX.BackColor = Color.FromArgb(CByte(38), CByte(38), CByte(38))
        COMPASS_BOX.BackgroundImageLayout = ImageLayout.Zoom
        COMPASS_BOX.Location = New Point(430, 770)
        COMPASS_BOX.Name = "COMPASS_BOX"
        COMPASS_BOX.Size = New Size(55, 55)
        COMPASS_BOX.TabIndex = 0
        COMPASS_BOX.TabStop = False
        ' 
        ' ZOOM_SCALE
        ' 
        ZOOM_SCALE.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ZOOM_SCALE.BackColor = Color.FromArgb(CByte(38), CByte(38), CByte(38))
        ZOOM_SCALE.Font = New Font("Segoe UI", 32.0F, FontStyle.Regular, GraphicsUnit.Point)
        ZOOM_SCALE.ForeColor = Color.Red
        ZOOM_SCALE.Location = New Point(199, 762)
        ZOOM_SCALE.Name = "ZOOM_SCALE"
        ZOOM_SCALE.Size = New Size(208, 66)
        ZOOM_SCALE.TabIndex = 0
        ZOOM_SCALE.Text = "200 m"
        ZOOM_SCALE.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' SysTimer
        ' 
        SysTimer.Enabled = True
        ' 
        ' MainPage
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        ClientSize = New Size(1600, 900)
        Controls.Add(Side_Panel)
        Controls.Add(Map_Panel)
        DoubleBuffered = True
        FormBorderStyle = FormBorderStyle.None
        Name = "MainPage"
        Text = "HiQ-Nav"
        WindowState = FormWindowState.Maximized
        Side_Panel.ResumeLayout(False)
        Side_Panel.PerformLayout()
        Panel1.ResumeLayout(False)
        Panel1.PerformLayout()
        CType(HOME_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(TIMEMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(SETTINGS_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(NETWORKMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(FASTMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(COMPASSMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(ZOOMOUT_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(ZOOMIN_BTN, ComponentModel.ISupportInitialize).EndInit()
        Map_Panel.ResumeLayout(False)
        Time_Panel.ResumeLayout(False)
        CType(VEHICLE_BOX, ComponentModel.ISupportInitialize).EndInit()
        CType(COMPASS_BOX, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents Side_Panel As Panel
    Friend WithEvents Map_Panel As BufferedPanel
    Friend WithEvents COMPASS_BOX As BufferedPictureBox
    Friend WithEvents ZOOM_SCALE As Label
    Friend WithEvents NORTH_LBL As Label
    Friend WithEvents NETWORKMODE_BTN As PictureBox
    Friend WithEvents FASTMODE_BTN As PictureBox
    Friend WithEvents COMPASSMODE_BTN As PictureBox
    Friend WithEvents ZOOMOUT_BTN As PictureBox
    Friend WithEvents ZOOMIN_BTN As PictureBox
    Friend WithEvents HOME_BTN As PictureBox
    Friend WithEvents TIMEMODE_BTN As PictureBox
    Friend WithEvents SETTINGS_BTN As PictureBox
    Friend WithEvents VEHICLE_BOX As BufferedPictureBox
    Friend WithEvents Time_Panel As Panel
    Friend WithEvents LblTime As Label
    Friend WithEvents SysTimer As Timer
    Friend WithEvents MAPMODE_BTN As Label
    Friend WithEvents POP3 As Label
    Friend WithEvents POP1 As Label
    Friend WithEvents POP2 As Label
    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label1 As Label
    Friend WithEvents POP6 As Label
    Friend WithEvents POP5 As Label
    Friend WithEvents POP4 As Label
    Friend WithEvents POP8 As Label
    Friend WithEvents POP7 As Label

End Class