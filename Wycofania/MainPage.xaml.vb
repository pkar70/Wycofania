
' 2021.06.18
' * UOKIK-R nie reagował na wyłączenie
' * trochę dodatkowego DebugOut
' * 

' STORE: 2021.06.16
' opóźnienie, bo niby działało, ale ciągle nie było prezentowania informacji z JSON z RAPEX
' początek: 2021.04.07

Public NotInheritable Class MainPage
    Inherits Page

    'Dim msToastId As String = ""
    'Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
    '    Try
    '        msToastId = e.Parameter.ToString.ToLower
    '    Catch ex As Exception
    '        msToastId = ""
    '    End Try
    'End Sub

    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        CrashMessageInit()
        ProgRingInit(True, True)

        If Not IsTriggersRegistered("Wycofania") Then
            If Not Await CanRegisterTriggersAsync() Then
                DialogBoxRes("errNoBackgroud")
            Else
                RegisterTimerTrigger("Wycofania_Timer", 90)
            End If
        End If

        ProgRingShow(True)
        If App.glItems.Count < 1 Then Await App.WczytajCache()
        DodajLinkiSzukania()

        ProgRingShow(False)

        'Dim bFound As Boolean = False

        'If msToastId <> "" Then  ' nie z Toast, lub Toast ktory mial tylko OPEN
        '    Dim iInd As Integer = msToastId.IndexOf("-")
        '    If iInd < 2 Then
        '        CrashMessageAdd("Bad param from Toast??", "")
        '    Else
        '        Dim sIcon As String = msToastId.Substring(0, iInd)
        '        Dim sId As String = msToastId.Substring(0, iInd + 1)

        '        For Each oItem As JednoPowiadomienie In App.glItems
        '            If oItem.sIcon = sIcon AndAlso oItem.sId = sId Then
        '                bFound = True
        '                Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)
        '            End If
        '        Next
        '    End If
        'End If

        uiList.ItemsSource = From c In App.glItems Order By c.sData Descending

    End Sub

    Public Sub GoDetailsToastId(sIcon As String, sId As String)

        For Each oItem As JednoPowiadomienie In App.glItems
            If oItem.sIcon = sIcon AndAlso oItem.sId = sId Then
                Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)
            End If
        Next
    End Sub


    Private Sub DodajLinkiSzukania()
        For Each oZrodlo As Source_Base In App.gaSrc
            Dim oMFI As MenuFlyoutItem = oZrodlo.GetSearchMFI
            AddHandler oMFI.Click, AddressOf uiMFIsearch_Click
            uiSearch.Items.Add(oMFI)
        Next
    End Sub

    Private Sub uiMFIsearch_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.OpenBrowserSearch(oMFI)
        Next
    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        Dim oLista As Collection(Of JednoPowiadomienie) = New Collection(Of JednoPowiadomienie)

        If App.gaSrc.Count < 1 Then Return

        ProgRingShow(True, False, 0, App.gaSrc.Count)
        Await App.SciagnijDane(True)
        ProgRingShow(False)

        uiList.ItemsSource = From c In App.glItems Order By c.sData Descending

    End Sub

    Private Sub uiOpenDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JednoPowiadomienie = Nothing
        Dim oMFI As FrameworkElement = TryCast(sender, FrameworkElement)
        If oMFI Is Nothing Then Return
        oItem = TryCast(oMFI.DataContext, JednoPowiadomienie)

        'If oMFI IsNot Nothing Then
        '    oItem = TryCast(oMFI.DataContext, JednoPowiadomienie)
        'Else
        '    Dim oButt As Button = TryCast(sender, Button)
        '    If oButt Is Nothing Then Return
        '    oItem = TryCast(oButt.DataContext, JednoPowiadomienie)
        'End If
        'If oItem Is Nothing Then Return

        ' wiemy już co
        Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)

    End Sub


    Private Sub uiOpenWeb_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPowiadomienie = TryCast(oMFI.DataContext, JednoPowiadomienie)
        If oItem Is Nothing Then Return

        OpenBrowser(oItem.sLink)
    End Sub

    Private Sub uiCopyLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPowiadomienie = TryCast(oMFI.DataContext, JednoPowiadomienie)
        If oItem Is Nothing Then Return

        ClipPut(oItem.sLink)
    End Sub

End Class

Public Class KonwersjaDaty
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        If value Is Nothing Then Return False

        Dim sTmp As String = CType(value, String)
        Return sTmp.Substring(2, 8)  ' yyyy.mm.dd
    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

Public Class KonwersjaIkonki
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        ' jeden rządek w ListView ma 32 piksele (znaczy tyle ma buttton)
        Dim sTmp As String
        Try
            sTmp = CType(value, String)
        Catch
            sTmp = "unkn"
        End Try

        Dim oUri As Uri = New Uri("ms-appx:///Assets/icon-" & sTmp.ToLower & ".png")
        Dim oBmpImg As BitmapImage = New BitmapImage(oUri)
        Return oBmpImg

        'ikonki:
        'RAPEX paczka z EU
        'RASFF pigulka/chleb z EU

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

Public Class KonwersjaIkonkiVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        Dim bIcon As Boolean = GetSettingsBool("uiConfig_ShowIcons", False)
        Dim sUnit As String = parameter

        'DebugOut("KonwersjaIkonkiVisibility(,," & sUnit & "..), bIcon=" & bIcon)
        If sUnit = "icon" Then Return bIcon
        Return Not bIcon

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class
