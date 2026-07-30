Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

' =====================================================================================
' PERFORMANCE-ÜBERARBEITUNG (Surface Pro 2 / schwache GDI+-Software-Rasterisierung)
'
' Der größte Kostenblock im alten Code war:
'   1) Eine vollständige Rotation der gesamten (oversized) Quell-Tile per
'      Graphics.DrawImage mit Bilinear-Interpolation - JEDEN Frame, auch wenn
'      sich nur die Blickrichtung um 0,1° geändert hat.
'   2) Pro Bildschirmzeile (oft 400-700 Zeilen) ein EIGENER g.DrawImage()-Aufruf
'      mit Bilinear-Stretch. GDI+ hat pro Aufruf einen relativ hohen Fixkosten-
'      Overhead (Validierung, Transform-Setup, Software-Rasterisierung) - das
'      multipliziert sich brutal bei mehreren hundert Aufrufen/Frame.
'   3) Pro Zeile (Nebel-Band) bzw. pro Stern jeweils ein "New SolidBrush(...)".
'
' Lösung: Rotation + perspektivisches Warping werden in EINEM einzigen,
' manuellen Pixel-Loop zusammengefasst (LockBits + Marshal.Copy auf Byte-
' Arrays, kein Unsafe-Code nötig). Es gibt nur noch genau EINEN g.DrawImage()-
' Aufruf für den gesamten Bodenbereich. Zusätzlich wird die Quell-Tile nur neu
' kopiert, wenn sich die Tile-Referenz tatsächlich geändert hat (nicht bei
' jedem reinen Heading-Smoothing-Tick), und Zeilen, die ohnehin komplett vom
' Nebel überdeckt würden, werden gar nicht erst gesampelt.
' =====================================================================================

Public NotInheritable Class Perspective3DRenderer

    Public Property HorizonFrac As Single = 0.27F
    Public Property VehicleGroundFrac As Single = 0.62F
    Public Property DepthScale As Single = 0.55F
    Public Property NearHalfWidthFrac As Single = 0.22F
    Public Property FogCutoffScale As Single = 1.0F
    Public Property HazeBandFrac As Single = 0.35F
    Public Property IsDay As Boolean = True
    Public Property MapPaperColor As Color = Color.FromArgb(224, 218, 200)
    Public Property VehicleImage As Image = Nothing
    Public Property EnableStars As Boolean = True

    ' --- Sterne-Cache: Brushes jetzt EINMAL erzeugt statt pro Stern/Frame ---
    Private _starPts As PointF()
    Private _starSizes As Single()
    Private _starBrushes As SolidBrush()
    Private _starsForW As Integer = -1
    Private _starsForH As Integer = -1

    ' --- Wiederverwendeter Zielpuffer für den Bodenbereich (kein Re-Alloc pro Frame) ---
    Private _groundBitmap As Bitmap
    Private _groundBuffer As Byte()
    Private _groundBufW As Integer = -1
    Private _groundBufH As Integer = -1

    ' --- Cache der Quell-Tile-Pixel: nur neu kopieren, wenn sich die Tile-Referenz ändert ---
    Private _cachedTileRef As Bitmap = Nothing
    Private _cachedTileBytes As Byte()
    Private _cachedTileStride As Integer
    Private _cachedTw As Integer
    Private _cachedTh As Integer

    Private Sub EnsureStars(panelW As Integer, horizonY As Integer)
        If _starPts IsNot Nothing AndAlso _starsForW = panelW AndAlso _starsForH = horizonY Then Return
        _starsForW = panelW
        _starsForH = horizonY

        If _starBrushes IsNot Nothing Then
            For Each b In _starBrushes
                b.Dispose()
            Next
        End If

        Dim count = Math.Max(90, CInt(panelW * Math.Max(1, horizonY) / 3200.0F))
        count = Math.Min(count, 450)

        Dim rnd As New Random(20260619)
        ReDim _starPts(count - 1)
        ReDim _starSizes(count - 1)
        ReDim _starBrushes(count - 1)

        Dim skyH = Math.Max(2, horizonY)
        For i = 0 To count - 1
            _starPts(i) = New PointF(
                CSng(rnd.NextDouble()) * panelW,
                CSng(rnd.NextDouble()) * skyH * 0.92F)
            _starSizes(i) = 0.8F + CSng(rnd.NextDouble()) * 1.7F
            Dim alpha = 90 + rnd.Next(0, 130)
            _starBrushes(i) = New SolidBrush(Color.FromArgb(alpha, 235, 240, 255))
        Next
    End Sub

    Private Sub EnsureGroundBuffer(w As Integer, h As Integer)
        If _groundBitmap IsNot Nothing AndAlso _groundBufW = w AndAlso _groundBufH = h Then Return
        _groundBitmap?.Dispose()
        _groundBitmap = New Bitmap(Math.Max(1, w), Math.Max(1, h), PixelFormat.Format32bppPArgb)
        _groundBufW = w
        _groundBufH = h
        ReDim _groundBuffer(Math.Max(1, w) * Math.Max(1, h) * 4 - 1)
    End Sub

    ' Kopiert die Quell-Tile nur dann (4-Byte-pro-Pixel) in ein Byte-Array, wenn sich
    ' die Bitmap-Referenz seit dem letzten Aufruf geändert hat. Bei reinem Heading-
    ' Smoothing (Tile bleibt gleich, nur Blickrichtung ändert sich) entfällt dieser
    ' Kopiervorgang komplett.
    Private Sub EnsureSourceBytes(sourceTile As Bitmap, tw As Integer, th As Integer)
        If ReferenceEquals(sourceTile, _cachedTileRef) AndAlso _cachedTw = tw AndAlso _cachedTh = th Then Return

        Dim rect As New Rectangle(0, 0, tw, th)
        Dim data As BitmapData = sourceTile.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Try
            Dim stride = data.Stride
            Dim bytes(stride * th - 1) As Byte
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length)
            _cachedTileBytes = bytes
            _cachedTileStride = stride
        Finally
            sourceTile.UnlockBits(data)
        End Try

        _cachedTileRef = sourceTile
        _cachedTw = tw
        _cachedTh = th
    End Sub

    Public Sub Render(g As Graphics, sourceTile As Bitmap, panelW As Integer, panelH As Integer, headingDeg As Single)

        If sourceTile Is Nothing OrElse panelW < 4 OrElse panelH < 4 Then Return

        Dim tw = sourceTile.Width
        Dim th = sourceTile.Height
        If tw < 2 OrElse th < 2 Then Return
        Dim srcCX = tw / 2.0F
        Dim srcCY = th / 2.0F

        Dim bgColor As Color = If(IsDay, MapPaperColor, Color.FromArgb(18, 18, 24))
        Dim skyTop As Color = If(IsDay, Color.FromArgb(95, 155, 210), Color.FromArgb(8, 14, 48))
        Dim skyBot As Color = If(IsDay, Color.FromArgb(165, 198, 228), Color.FromArgb(20, 32, 65))
        Dim hazeColor As Color = skyBot

        Dim horizonY = CInt(panelH * HorizonFrac)
        Dim groundH = panelH - horizonY

        Using skyBrush As New LinearGradientBrush(
                New Rectangle(0, 0, panelW, Math.Max(2, horizonY + 2)),
                skyTop, skyBot, LinearGradientMode.Vertical)
            g.FillRectangle(skyBrush, 0, 0, panelW, horizonY + 2)
        End Using

        If Not IsDay AndAlso EnableStars Then
            EnsureStars(panelW, horizonY)
            g.SmoothingMode = SmoothingMode.AntiAlias
            For i = 0 To _starPts.Length - 1
                Dim p = _starPts(i)
                Dim s = _starSizes(i)
                g.FillEllipse(_starBrushes(i), p.X - s / 2.0F, p.Y - s / 2.0F, s, s)
            Next
            g.SmoothingMode = SmoothingMode.None
        End If

        If groundH <= 0 Then Return

        EnsureGroundBuffer(panelW, groundH)
        EnsureSourceBytes(sourceTile, tw, th)

        Dim srcBytes = _cachedTileBytes
        Dim srcStride = _cachedTileStride

        Dim Vf = VehicleGroundFrac
        Dim D = DepthScale * (th / 2.0F)
        Dim nearHalfW = CSng(tw) * NearHalfWidthFrac * Vf
        Dim invGroundH = 1.0F / CSng(groundH)
        Dim maxHalfW = CSng(tw) / 2.0F - 0.5F
        Dim invPanelW = 1.0F / CSng(panelW)

        Dim t0 = (Vf * D) / (srcCY + D)
        Dim t0Fade = Math.Min(t0 * 1.3F, Vf * HazeBandFrac)
        Dim screenY0 = horizonY - 1 + CInt(t0Fade * groundH)

        Dim rad = CDbl(headingDeg) * Math.PI / 180.0
        Dim cosA = CSng(Math.Cos(rad))
        Dim sinA = CSng(Math.Sin(rad))

        Dim bgR = CSng(bgColor.R) : Dim bgG = CSng(bgColor.G) : Dim bgB = CSng(bgColor.B)
        Dim hzR = CSng(hazeColor.R) : Dim hzG = CSng(hazeColor.G) : Dim hzB = CSng(hazeColor.B)

        Dim buf = _groundBuffer
        Dim outStride = panelW * 4
        Dim fogScale = Math.Max(0.0F, Math.Min(1.0F, FogCutoffScale))

        For ry = 0 To groundH - 1
            Dim screenY = horizonY + ry
            Dim rowOff = ry * outStride

            Dim skipSampling = False
            Dim fogFrac As Single = 0.0F
            If screenY <= screenY0 Then
                Dim span = Math.Max(1, screenY0 - horizonY)
                Dim baseFrac = CSng(screenY0 - screenY) / CSng(span)
                If baseFrac < 0.0F Then baseFrac = 0.0F
                fogFrac = CSng(Math.Pow(baseFrac, 0.5)) * fogScale
                If fogFrac > 0.0F Then skipSampling = True
            End If

            If skipSampling Then
                Dim rr = CByte(Math.Min(255.0F, Math.Max(0.0F, hzR * fogFrac + bgR * (1.0F - fogFrac))))
                Dim gg = CByte(Math.Min(255.0F, Math.Max(0.0F, hzG * fogFrac + bgG * (1.0F - fogFrac))))
                Dim bb = CByte(Math.Min(255.0F, Math.Max(0.0F, hzB * fogFrac + bgB * (1.0F - fogFrac))))
                For x = 0 To panelW - 1
                    Dim o = rowOff + x * 4
                    buf(o) = bb : buf(o + 1) = gg : buf(o + 2) = rr : buf(o + 3) = 255
                Next
                Continue For
            End If

            Dim t = CSng(ry + 1) * invGroundH
            If t < 0.0005F Then t = 0.0005F

            Dim rowSrcY = srcCY - D * (Vf / t - 1.0F)
            Dim srcHalfW = Math.Min(nearHalfW / t, maxHalfW)
            Dim totalSrcW = srcHalfW * 2.0F

            If totalSrcW < 0.5F Then
                For x = 0 To panelW - 1
                    Dim o = rowOff + x * 4
                    buf(o) = CByte(bgB) : buf(o + 1) = CByte(bgG) : buf(o + 2) = CByte(bgR) : buf(o + 3) = 255
                Next
                Continue For
            End If

            Dim srcLeft = srcCX - srcHalfW

            For x = 0 To panelW - 1
                Dim fx = (CSng(x) + 0.5F) * invPanelW
                Dim rotX = srcLeft + fx * totalSrcW

                Dim dx = rotX - srcCX
                Dim dy = rowSrcY - srcCY
                Dim sx = dx * cosA - dy * sinA + srcCX
                Dim sy = dx * sinA + dy * cosA + srcCY

                Dim o = rowOff + x * 4

                If sx < 0.0F OrElse sx >= CSng(tw) - 1.0F OrElse sy < 0.0F OrElse sy >= CSng(th) - 1.0F Then
                    buf(o) = CByte(bgB) : buf(o + 1) = CByte(bgG) : buf(o + 2) = CByte(bgR) : buf(o + 3) = 255
                Else
                    Dim ix As Integer = CInt(Math.Floor(sx))
                    Dim iy As Integer = CInt(Math.Floor(sy))
                    Dim fxr = sx - ix
                    Dim fyr = sy - iy

                    Dim i00 = iy * srcStride + ix * 4
                    Dim i10 = i00 + 4
                    Dim i01 = i00 + srcStride
                    Dim i11 = i01 + 4

                    Dim w00 = (1.0F - fxr) * (1.0F - fyr)
                    Dim w10 = fxr * (1.0F - fyr)
                    Dim w01 = (1.0F - fxr) * fyr
                    Dim w11 = fxr * fyr

                    Dim bb = srcBytes(i00) * w00 + srcBytes(i10) * w10 + srcBytes(i01) * w01 + srcBytes(i11) * w11
                    Dim gg = srcBytes(i00 + 1) * w00 + srcBytes(i10 + 1) * w10 + srcBytes(i01 + 1) * w01 + srcBytes(i11 + 1) * w11
                    Dim rr = srcBytes(i00 + 2) * w00 + srcBytes(i10 + 2) * w10 + srcBytes(i01 + 2) * w01 + srcBytes(i11 + 2) * w11

                    buf(o) = CByte(Math.Min(255.0F, Math.Max(0.0F, bb)))
                    buf(o + 1) = CByte(Math.Min(255.0F, Math.Max(0.0F, gg)))
                    buf(o + 2) = CByte(Math.Min(255.0F, Math.Max(0.0F, rr)))
                    buf(o + 3) = 255
                End If
            Next
        Next

        Dim dstRect As New Rectangle(0, 0, panelW, groundH)
        Dim dstData As BitmapData = _groundBitmap.LockBits(dstRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb)
        Try
            If dstData.Stride = outStride Then
                Marshal.Copy(buf, 0, dstData.Scan0, buf.Length)
            Else
                Dim srcOff = 0
                Dim dstPtr = dstData.Scan0
                For row = 0 To groundH - 1
                    Marshal.Copy(buf, srcOff, IntPtr.Add(dstPtr, row * dstData.Stride), outStride)
                    srcOff += outStride
                Next
            End If
        Finally
            _groundBitmap.UnlockBits(dstData)
        End Try

        g.DrawImageUnscaled(_groundBitmap, 0, horizonY)

        If VehicleImage IsNot Nothing Then
            Dim sz = Math.Max(20, CInt(panelW * 0.045F))
            Dim vx = panelW / 2.0F
            Dim vy = CSng(horizonY) + CSng(groundH) * Vf
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.DrawImage(VehicleImage, vx - sz / 2.0F, vy - sz / 2.0F, CSng(sz), CSng(sz))
        End If
    End Sub

    Public Sub Dispose()
        _groundBitmap?.Dispose()
        _groundBitmap = Nothing
        If _starBrushes IsNot Nothing Then
            For Each b In _starBrushes
                b.Dispose()
            Next
            _starBrushes = Nothing
        End If
    End Sub

End Class