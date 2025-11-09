Imports vb14 = VBlib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports pkar.UI.Toasts
Imports pkar.UI.Triggers
Imports pkar.DotNetExtensions

' 2022.02.06
' ikonka dla Toast (ta sama co na liście)
' 

' 2022.02.03
' * VBlib, do którego trafiają wszystkie poza Rasff i UOKiK (bo te mają RSS)
' * wysyłanie Toastu - z Builder (niestety, w VBLib nie ma .GetXml więc nie da się przenieść do VBLib)
' * MinVers na 16299, co pozwala na VBlib .Net Std 2.0, a to daje RSS
' * klasy Source* udało się w całości przenieść do VBlib, po przenosinach Toast do App, zaś związane z UI - do xaml.vb, głównie Settings
' * byla wiec konieczna migracja z ServiceModel.Syndication.SyndicationFeed do ServiceModel.Syndication.SyndicationFeed

' 2021.12.10
' tak jakby UOKIK_Reg, GIS, GIF i jeszcze jedno miało w Timer problem ze ściąganiem danych (get/post, "text associated with this error code cannot be found")

' 2021.11.30
' * uaktualnienie linku do RASFF

' 2021.06.18
' * UOKIK-R nie reagował na wyłączenie
' * trochę dodatkowego DebugOut
' * 

' STORE: 2021.06.16
' opóźnienie, bo niby działało, ale ciągle nie było prezentowania informacji z JSON z RAPEX
' początek: 2021.04.07

Public NotInheritable Class MainPage
    Inherits Page


    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        '  CrashMessageInit() zbędne w aktualnej wersji biblioteki
        Me.InitDialogs
        ProgRingInit(True, True)

        Await CrashMessageShowAsync()

        If Not IsTriggersRegistered("Wycofania") Then
            If Not Await CanRegisterTriggersAsync() Then
                Me.MsgBox("errNoBackgroud")
            Else
                RegisterTimerTrigger("Wycofania_Timer", 90, False, New Windows.ApplicationModel.Background.SystemCondition(Windows.ApplicationModel.Background.SystemConditionType.InternetAvailable))
            End If
        End If

        ProgRingShow(True)
        'If VBlib.App.glItems.Count < 1 Then
        App.WczytajCache()
        DodajLinkiSzukania()

        ProgRingShow(False)

        If VBlib.App.glItems IsNot Nothing Then
            uiList.ItemsSource = From c In VBlib.App.glItems Order By c.sData Descending
        End If
        ToolTipService.SetToolTip(uiRefresh, VBlib.GetSettingsDate("lastCheck").ToExifString)
    End Sub

    Public Sub GoDetailsToastId(sIcon As String, sId As String)

        For Each oItem As VBlib.JednoPowiadomienie In VBlib.App.glItems
            If oItem.sIcon = sIcon AndAlso oItem.sId = sId Then
                Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)
            End If
        Next
    End Sub


    Private Sub DodajLinkiSzukania()
        For Each oZrodlo As VBlib.Source_Base In VBlib.App.gaSrc
            Dim oMFI As New MenuFlyoutItem
            oMFI.Text = oZrodlo.GetSettingName
            oMFI.Name = "uiSearchMFI_" & oZrodlo.GetSettingName
            oMFI.DataContext = oZrodlo.GetSearchLink
            AddHandler oMFI.Click, AddressOf uiMFIsearch_Click
            uiSearch.Items.Add(oMFI)
        Next
    End Sub

    Private Sub uiMFIsearch_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim sLink As String = CType(oMFI.DataContext, String)
        Dim oUri As New Uri(sLink)
        oUri.OpenBrowser
    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)

        'Dim oLista As Collection(Of JednoPowiadomienie) = New Collection(Of JednoPowiadomienie)

        If VBlib.App.gaSrc.Count < 1 Then Return

        ProgRingShow(True, False, 0, VBlib.App.gaSrc.Count)
        Await App.SciagnijDane(Me)
        ProgRingShow(False)

        uiList.ItemsSource = From c In VBlib.App.glItems Order By c.sData Descending

    End Sub

    Private Sub uiOpenDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As FrameworkElement = TryCast(sender, FrameworkElement)
        If oMFI Is Nothing Then Return
        Dim oItem As VBlib.JednoPowiadomienie = TryCast(oMFI.DataContext, VBlib.JednoPowiadomienie)

        ' wiemy już co
        Me.Navigate(GetType(Detailsy), oItem.sLink)

    End Sub


    Private Sub uiOpenWeb_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As VBlib.JednoPowiadomienie = TryCast(oMFI.DataContext, VBlib.JednoPowiadomienie)
        If oItem Is Nothing Then Return

        Dim oUri As New Uri(oItem.sLink)
        oUri.OpenBrowser()
    End Sub

    Private Sub uiCopyLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As VBlib.JednoPowiadomienie = TryCast(oMFI.DataContext, VBlib.JednoPowiadomienie)
        If oItem Is Nothing Then Return

        oItem.sLink.SendToClipboard
    End Sub

End Class



Public Class KonwersjaIkonki
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object

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
End Class

Public Class KonwersjaIkonkiVisibility
    Inherits ValueConverterOneWayWithPar

    Protected Overrides Function Convert(value As Object, param As String) As Object

        Dim bIcon As Boolean = vb14.GetSettingsBool("uiConfig_ShowIcons", False)
        If param = "icon" Then Return bIcon
        Return Not bIcon

    End Function

End Class

Public Class KonwersjaDatyStr
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim sTmp As String
        Try
            sTmp = CType(value, String)
            Return sTmp.Substring(2, 8)  ' yyyy.mm.dd
        Catch
        End Try
        Return "???"
    End Function
End Class
