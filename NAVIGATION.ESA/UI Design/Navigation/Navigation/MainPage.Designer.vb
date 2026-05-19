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
        Side_Panel = New Panel()
        HOME_BTN = New PictureBox()
        TIMEMODE_BTN = New PictureBox()
        PictureBox7 = New PictureBox()
        NETWORKMODE_BTN = New PictureBox()
        FASTMODE_BTN = New PictureBox()
        COMPASSMODE_BTN = New PictureBox()
        ZOOMOUT_BTN = New PictureBox()
        ZOOMIN_BTN = New PictureBox()
        Map_Panel = New Panel()
        VEHICLE_BOX = New PictureBox()
        NORTH_LBL = New Label()
        COMPASS_BOX = New PictureBox()
        ZOOM_SCALE = New Label()
        IDRIVE_BOX = New PictureBox()
        Side_Panel.SuspendLayout()
        CType(HOME_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(TIMEMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(PictureBox7, ComponentModel.ISupportInitialize).BeginInit()
        CType(NETWORKMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(FASTMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(COMPASSMODE_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(ZOOMOUT_BTN, ComponentModel.ISupportInitialize).BeginInit()
        CType(ZOOMIN_BTN, ComponentModel.ISupportInitialize).BeginInit()
        Map_Panel.SuspendLayout()
        CType(VEHICLE_BOX, ComponentModel.ISupportInitialize).BeginInit()
        CType(COMPASS_BOX, ComponentModel.ISupportInitialize).BeginInit()
        CType(IDRIVE_BOX, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Side_Panel
        ' 
        Side_Panel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        Side_Panel.BackgroundImage = My.Resources.Resources.NAV_PANEL
        Side_Panel.BackgroundImageLayout = ImageLayout.Stretch
        Side_Panel.Controls.Add(HOME_BTN)
        Side_Panel.Controls.Add(TIMEMODE_BTN)
        Side_Panel.Controls.Add(PictureBox7)
        Side_Panel.Controls.Add(NETWORKMODE_BTN)
        Side_Panel.Controls.Add(FASTMODE_BTN)
        Side_Panel.Controls.Add(COMPASSMODE_BTN)
        Side_Panel.Controls.Add(ZOOMOUT_BTN)
        Side_Panel.Controls.Add(ZOOMIN_BTN)
        Side_Panel.Location = New Point(0, 0)
        Side_Panel.Name = "Side_Panel"
        Side_Panel.Size = New Size(100, 900)
        Side_Panel.TabIndex = 0
        ' 
        ' HOME_BTN
        ' 
        HOME_BTN.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        HOME_BTN.BackColor = Color.Transparent
        HOME_BTN.BackgroundImage = My.Resources.Resources.HOME
        HOME_BTN.BackgroundImageLayout = ImageLayout.Zoom
        HOME_BTN.Location = New Point(15, 710)
        HOME_BTN.Name = "HOME_BTN"
        HOME_BTN.Size = New Size(70, 70)
        HOME_BTN.TabIndex = 9
        HOME_BTN.TabStop = False
        ' 
        ' TIMEMODE_BTN
        ' 
        TIMEMODE_BTN.BackColor = Color.Transparent
        TIMEMODE_BTN.BackgroundImage = My.Resources.Resources.DAY
        TIMEMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        TIMEMODE_BTN.Location = New Point(15, 420)
        TIMEMODE_BTN.Name = "TIMEMODE_BTN"
        TIMEMODE_BTN.Size = New Size(70, 70)
        TIMEMODE_BTN.TabIndex = 8
        TIMEMODE_BTN.TabStop = False
        ' 
        ' PictureBox7
        ' 
        PictureBox7.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        PictureBox7.BackColor = Color.Transparent
        PictureBox7.BackgroundImage = My.Resources.Resources.SETTINGS
        PictureBox7.BackgroundImageLayout = ImageLayout.Zoom
        PictureBox7.Location = New Point(15, 810)
        PictureBox7.Name = "PictureBox7"
        PictureBox7.Size = New Size(70, 70)
        PictureBox7.TabIndex = 7
        PictureBox7.TabStop = False
        ' 
        ' NETWORKMODE_BTN
        ' 
        NETWORKMODE_BTN.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        NETWORKMODE_BTN.BackColor = Color.Transparent
        NETWORKMODE_BTN.BackgroundImage = My.Resources.Resources.OFFLINE
        NETWORKMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        NETWORKMODE_BTN.Location = New Point(15, 610)
        NETWORKMODE_BTN.Name = "NETWORKMODE_BTN"
        NETWORKMODE_BTN.Size = New Size(70, 70)
        NETWORKMODE_BTN.TabIndex = 6
        NETWORKMODE_BTN.TabStop = False
        ' 
        ' FASTMODE_BTN
        ' 
        FASTMODE_BTN.BackColor = Color.Transparent
        FASTMODE_BTN.BackgroundImage = My.Resources.Resources.FAST_MODE
        FASTMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        FASTMODE_BTN.Location = New Point(15, 320)
        FASTMODE_BTN.Name = "FASTMODE_BTN"
        FASTMODE_BTN.Size = New Size(70, 70)
        FASTMODE_BTN.TabIndex = 5
        FASTMODE_BTN.TabStop = False
        ' 
        ' COMPASSMODE_BTN
        ' 
        COMPASSMODE_BTN.BackColor = Color.Transparent
        COMPASSMODE_BTN.BackgroundImage = My.Resources.Resources.COMPASS_MODE
        COMPASSMODE_BTN.BackgroundImageLayout = ImageLayout.Zoom
        COMPASSMODE_BTN.Location = New Point(15, 220)
        COMPASSMODE_BTN.Name = "COMPASSMODE_BTN"
        COMPASSMODE_BTN.Size = New Size(70, 70)
        COMPASSMODE_BTN.TabIndex = 4
        COMPASSMODE_BTN.TabStop = False
        ' 
        ' ZOOMOUT_BTN
        ' 
        ZOOMOUT_BTN.BackColor = Color.Transparent
        ZOOMOUT_BTN.BackgroundImage = My.Resources.Resources.ZOOM_OUT
        ZOOMOUT_BTN.BackgroundImageLayout = ImageLayout.Zoom
        ZOOMOUT_BTN.Location = New Point(15, 120)
        ZOOMOUT_BTN.Name = "ZOOMOUT_BTN"
        ZOOMOUT_BTN.Size = New Size(70, 70)
        ZOOMOUT_BTN.TabIndex = 3
        ZOOMOUT_BTN.TabStop = False
        ' 
        ' ZOOMIN_BTN
        ' 
        ZOOMIN_BTN.BackColor = Color.Transparent
        ZOOMIN_BTN.BackgroundImage = My.Resources.Resources.ZOOM_IN
        ZOOMIN_BTN.BackgroundImageLayout = ImageLayout.Zoom
        ZOOMIN_BTN.Location = New Point(15, 20)
        ZOOMIN_BTN.Name = "ZOOMIN_BTN"
        ZOOMIN_BTN.Size = New Size(70, 70)
        ZOOMIN_BTN.TabIndex = 2
        ZOOMIN_BTN.TabStop = False
        ' 
        ' Map_Panel
        ' 
        Map_Panel.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Map_Panel.BackColor = Color.FromArgb(CByte(34), CByte(34), CByte(34))
        Map_Panel.Controls.Add(VEHICLE_BOX)
        Map_Panel.Controls.Add(NORTH_LBL)
        Map_Panel.Controls.Add(COMPASS_BOX)
        Map_Panel.Controls.Add(ZOOM_SCALE)
        Map_Panel.Controls.Add(IDRIVE_BOX)
        Map_Panel.Location = New Point(100, 0)
        Map_Panel.Name = "Map_Panel"
        Map_Panel.Size = New Size(1340, 900)
        Map_Panel.TabIndex = 1
        ' 
        ' VEHICLE_BOX
        ' 
        VEHICLE_BOX.Anchor = AnchorStyles.None
        VEHICLE_BOX.BackColor = Color.Transparent
        VEHICLE_BOX.BackgroundImageLayout = ImageLayout.Zoom
        VEHICLE_BOX.Location = New Point(635, 415)
        VEHICLE_BOX.Name = "VEHICLE_BOX"
        VEHICLE_BOX.Size = New Size(70, 70)
        VEHICLE_BOX.TabIndex = 2
        VEHICLE_BOX.TabStop = False
        ' 
        ' NORTH_LBL
        ' 
        NORTH_LBL.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        NORTH_LBL.BackColor = Color.FromArgb(CByte(38), CByte(38), CByte(38))
        NORTH_LBL.Font = New Font("Segoe UI", 35F)
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
        COMPASS_BOX.BackgroundImage = My.Resources.Resources.COMPASS
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
        ZOOM_SCALE.Font = New Font("Segoe UI", 32F)
        ZOOM_SCALE.ForeColor = Color.Red
        ZOOM_SCALE.Location = New Point(199, 762)
        ZOOM_SCALE.Name = "ZOOM_SCALE"
        ZOOM_SCALE.Size = New Size(208, 66)
        ZOOM_SCALE.TabIndex = 0
        ZOOM_SCALE.Text = "200 m"
        ZOOM_SCALE.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' IDRIVE_BOX
        ' 
        IDRIVE_BOX.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        IDRIVE_BOX.BackColor = Color.Transparent
        IDRIVE_BOX.BackgroundImage = My.Resources.Resources.IDRIVE_ON
        IDRIVE_BOX.BackgroundImageLayout = ImageLayout.Zoom
        IDRIVE_BOX.Location = New Point(46, 729)
        IDRIVE_BOX.Name = "IDRIVE_BOX"
        IDRIVE_BOX.Size = New Size(504, 159)
        IDRIVE_BOX.TabIndex = 0
        IDRIVE_BOX.TabStop = False
        ' 
        ' MainPage
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        ClientSize = New Size(1440, 900)
        Controls.Add(Map_Panel)
        Controls.Add(Side_Panel)
        DoubleBuffered = True
        FormBorderStyle = FormBorderStyle.None
        Name = "MainPage"
        Text = "HiQ-Nav"
        WindowState = FormWindowState.Maximized
        Side_Panel.ResumeLayout(False)
        CType(HOME_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(TIMEMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(PictureBox7, ComponentModel.ISupportInitialize).EndInit()
        CType(NETWORKMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(FASTMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(COMPASSMODE_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(ZOOMOUT_BTN, ComponentModel.ISupportInitialize).EndInit()
        CType(ZOOMIN_BTN, ComponentModel.ISupportInitialize).EndInit()
        Map_Panel.ResumeLayout(False)
        CType(VEHICLE_BOX, ComponentModel.ISupportInitialize).EndInit()
        CType(COMPASS_BOX, ComponentModel.ISupportInitialize).EndInit()
        CType(IDRIVE_BOX, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents Side_Panel As Panel
    Friend WithEvents Map_Panel As Panel
    Friend WithEvents COMPASS_BOX As PictureBox
    Friend WithEvents ZOOM_SCALE As Label
    Friend WithEvents IDRIVE_BOX As PictureBox
    Friend WithEvents NORTH_LBL As Label
    Friend WithEvents NETWORKMODE_BTN As PictureBox
    Friend WithEvents FASTMODE_BTN As PictureBox
    Friend WithEvents COMPASSMODE_BTN As PictureBox
    Friend WithEvents ZOOMOUT_BTN As PictureBox
    Friend WithEvents ZOOMIN_BTN As PictureBox
    Friend WithEvents HOME_BTN As PictureBox
    Friend WithEvents TIMEMODE_BTN As PictureBox
    Friend WithEvents PictureBox7 As PictureBox
    Friend WithEvents VEHICLE_BOX As PictureBox

End Class
