Imports vb14 = VBlib.pkarlibmodule14
Imports pkar.Uwp.Ext


NotInheritable Class App
    Inherits Application

#Region "wizard"
    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = OnLaunchFragment(e.PreviousExecutionState)

        ' InitVbLib()

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

    ' wedle https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/send-local-toast
    ' foreground activation
    ' CommandLine, Toasts
    Protected Overrides Async Sub OnActivated(e As IActivatedEventArgs)

        'InitVbLib()

        ' próba czy to commandline
        If e.Kind = ActivationKind.CommandLineLaunch Then

            Dim commandLine As CommandLineActivatedEventArgs = TryCast(e, CommandLineActivatedEventArgs)
            Dim operation As CommandLineActivationOperation = commandLine?.Operation
            Dim strArgs As String = operation?.Arguments

            If Not String.IsNullOrEmpty(strArgs) Then
                InitLib(strArgs.Split(" ").ToList, True)
                Await ObsluzCommandLine(strArgs)
                Window.Current.Close()
                Return
            End If
        End If

        ' jesli nie cmdline (a np. toast), albo cmdline bez parametrow, to pokazujemy okno
        Dim rootFrame As Frame = OnLaunchFragment(e.PreviousExecutionState)

        Dim oToastAct As ToastNotificationActivatedEventArgs
        oToastAct = TryCast(e, ToastNotificationActivatedEventArgs)
        If oToastAct IsNot Nothing Then
            Dim sArgs As String = oToastAct.Argument
            Select Case sArgs.Substring(0, 4)
                Case "OPEN"

                    If rootFrame.Content Is Nothing Then rootFrame.Navigate(GetType(MainPage))

                    Dim iInd As Integer = sArgs.IndexOf("-")
                    If iInd < 2 Then Exit Select

                    Dim sIcon As String = sArgs.Substring(0, iInd)
                    Dim sId As String = sArgs.Substring(0, iInd + 1)
                    Dim oMPage As MainPage = TryCast(rootFrame.Content, MainPage)
                    If oMPage IsNot Nothing Then
                        If VBlib.App.glItems.Count < 1 Then WczytajCache()
                        oMPage.GoDetailsToastId(sIcon, sId)
                    End If
            End Select
        End If

        Window.Current.Activate()
    End Sub

    Protected Function OnLaunchFragment(aes As ApplicationExecutionState) As Frame
        Dim mRootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If mRootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            mRootFrame = New Frame()

            AddHandler mRootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler mRootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            ' Place the frame in the current Window
            Window.Current.Content = mRootFrame

            InitLib(Nothing, True)
        End If

        Return mRootFrame
    End Function


#End Region

    Public Shared Function WczytajCache() As Boolean
        Return VBlib.App.WczytajCache(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path)
    End Function

    Public Shared Function ZapiszCache() As Boolean
        Return VBlib.App.ZapiszCache(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path)
    End Function

    Private Shared mToastIcon As String = ""
    Private Shared mToastLines As String = ""

    Private Shared Sub MakeToastMain(sIcon As String, sTitle As String, sId As String)

        Dim oBldr As New Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()

        ' jako header
        ' https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/toast-headers
        oBldr.AddHeader(sIcon, sIcon, "")
        oBldr.AddText(sTitle)
        oBldr.AddButton(New Microsoft.Toolkit.Uwp.Notifications.ToastButtonDismiss)

        Dim oUri As Uri = New Uri("ms-appx:///Assets/icon-" & sIcon & ".png")
        oBldr.AddAppLogoOverride(oUri)
        Dim sTag As String = ""
        If sId <> "" Then sTag = sIcon & "-" & sId
        oBldr.AddArgument("OPEN" & sTag)

        Dim oToast As Windows.UI.Notifications.ToastNotification = New Windows.UI.Notifications.ToastNotification(oBldr.GetXml)

        If sTag <> "" Then
            ' The size of the notification tag is too large.
            ' tag: 16 chrs, od Creators (14971 ??) jest 64 chrs
            ' https://docs.microsoft.com/en-us/uwp/api/windows.ui.notifications.toastnotification.tag#Windows_UI_Notifications_ToastNotification_Tag
            If sTag.Length > 64 Then sTag = sTag.Substring(0, 64)
            oToast.Tag = sTag   ' żeby można było usunąć toast gdy sie usunie w aplikacji
        End If

        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub

    Protected Shared Sub MakeToast(oItem As JednoPowiadomienie, sSettName As String, bDefOneToast As Boolean)
        vb14.DumpCurrMethod()

        If Not vb14.GetSettingsBool(sSettName & "_Toast") Then Return

        If vb14.GetSettingsBool(sSettName & "_OneToast", bDefOneToast) Then
            mToastIcon = oItem.sIcon
            If mToastLines <> "" Then mToastLines += vbCrLf
            mToastLines += oItem.sTitle
        Else
            MakeToastMain(oItem.sIcon, oItem.sTitle, oItem.sId)
            Return
        End If

    End Sub

    Public Shared Async Function ReadDataOne(bMsg As Boolean, oZrodlo As VBlib.Source_Base) As Task
        vb14.DumpCurrMethod("(zrodlo: " & oZrodlo.GetFullName)

        mToastLines = ""
        mToastIcon = ""

        If Not vb14.GetSettingsBool(oZrodlo.GetSettingName, oZrodlo.GetDefEnable) Then
            vb14.DebugOut("ReadData for " & oZrodlo.GetSettingName & " - but not enabled")
            Return
        End If

        Dim oTempColl As Collection(Of JednoPowiadomienie) = Await oZrodlo.ReadData(bMsg)
        If oTempColl Is Nothing Then Return

        For Each oNew As JednoPowiadomienie In oTempColl
            VBlib.App.glItems.Add(oNew)
            MakeToast(oNew, oZrodlo.GetSettingName, oZrodlo.GetDefOneToast)
        Next

        If Not vb14.GetSettingsBool(oZrodlo.GetSettingName & "_OneToast", oZrodlo.GetDefOneToast) Then Return
        If mToastLines = "" Then Return

        ' jeden wspolny toast (wszystkie linie tego source razem)
        MakeToastMain(mToastIcon, mToastLines, "")

    End Function

    'Private Shared Sub InitVbLib()
    '    VBlib.pkarlibmodule.InitDump(GetSettingsInt("debugLogLevel", 0), Windows.Storage.ApplicationData.Current.TemporaryFolder.Path)
    '    VBlib.pkarlibmodule.InitSettings(AddressOf pkar.SetSettingsString, AddressOf pkar.SetSettingsInt, AddressOf pkar.SetSettingsBool, AddressOf pkar.GetSettingsString, AddressOf pkar.GetSettingsInt, AddressOf pkar.GetSettingsBool)
    '    VBlib.pkarlibmodule.InitDialogBox(AddressOf pkar.DialogBoxAsync, AddressOf pkar.DialogBoxYNAsync, AddressOf pkar.DialogBoxInputAllDirectAsync)
    'End Sub
    Public Shared Async Function SciagnijDane(oPage As Page, bMsg As Boolean) As Task
        vb14.DumpCurrMethod("(bMsg=" & bMsg)

        If VBlib.App.glItems.Count < 1 Then
            vb14.DebugOut(2, "nie ma cache - Loading")
            WczytajCache()
        End If

        For Each oZrodlo As VBlib.Source_Base In VBlib.App.gaSrc
            Try
                Await ReadDataOne(bMsg, oZrodlo)
                If bMsg Then oPage.ProgRingInc()
            Catch ex As Exception
                ' zeby jeden błędny nie powodował zniknięcia reszty
            End Try
        Next

        ZapiszCache()
    End Function

    Protected Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
        Select Case sCommand.ToLower
            Case "check now"
                Await SciagnijDane(Nothing, False)
                Return "Done"
        End Select
        Return ""
    End Function

    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        ' tile update / warnings
        moTaskDeferal = args.TaskInstance.GetDeferral() ' w pkarmodule.App

        InitLib(Nothing, True)

        Dim bNoComplete As Boolean = False
        Dim bObsluzone As Boolean = False
        Select Case args.TaskInstance.Task.Name
            Case "Wycofania_Timer"
                vb14.SetSettingsString("lastTimerRun", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                If NetIsIPavailable(False) Then Await SciagnijDane(Nothing, False)
                bObsluzone = True
        End Select

        Dim sLocalCmds As String = "check now" & vbTab & ": check data now"

        If Not bObsluzone Then bNoComplete = RemSysInit(args, sLocalCmds)

        If Not bNoComplete Then moTaskDeferal.Complete()

    End Sub


End Class
