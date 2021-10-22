

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
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed


            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

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
#End Region

    ' wedle https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/send-local-toast
    ' foreground activation
    Protected Overrides Async Sub OnActivated(e As IActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

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
                        If glItems.Count < 1 Then Await WczytajCache()
                        oMPage.GoDetailsToastId(sIcon, sId)
                    End If
            End Select
        End If

        Window.Current.Activate()
    End Sub



    Public Shared gaSrc As Source_Base() = {
        New Source_Gif,
        New Source_GIS,
        New Source_UOKIK,
        New Source_UOKIK_Reg,
        New Source_RASFF,
        New Source_Rapex
    }

    Public Shared glItems As Collection(Of JednoPowiadomienie) = New Collection(Of JednoPowiadomienie)


    Public Shared Async Function WczytajCache() As Task(Of Boolean)
        Dim sTxt As String = Await Windows.Storage.ApplicationData.Current.LocalCacheFolder.ReadAllTextFromFileAsync("items.json")
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            glItems.Clear()
            Return False
        End If

        glItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(Collection(Of JednoPowiadomienie)))
        Return True
    End Function

    Public Shared Async Function ZapiszCache() As Task(Of Boolean)
        If glItems Is Nothing Then
            DebugOut("ZapiszCache - glItems null")
            Return False
        End If
        If glItems.Count < 1 Then
            DebugOut("ZapiszCache - glItems.count<1")
            Return False
        End If

        RemoveOldCache()    ' usun zbyt stare entries
        If glItems.Count < 1 Then
            DebugOut("ZapiszCache - glItems.count<1 (po usunieciu starych)")
            Return False
        End If

        DebugOut("ZapiszCache - do zapisania jest " & glItems.Count & " entries")
        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(glItems, Newtonsoft.Json.Formatting.Indented)

        Await oFold.WriteAllTextToFileAsync("items.json", sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        Return True
    End Function

    Private Shared Function RemoveOldCache() As Integer
        Dim iRet As Integer = 0

        For Each oSource As Source_Base In gaSrc
            iRet += oSource.RemoveOldCache
        Next

        DebugOut("RemoveOldCache - removed " & iRet & " old items")
        Return iRet

    End Function

    Public Shared Async Function SciagnijDane(bMsg As Boolean) As Task
        DebugOut(1, "SciagnijDane(" & bMsg)

        If glItems.Count < 1 Then
            DebugOut(2, "nie ma cache - Loading")
            Await WczytajCache()
        End If

        For Each oZrodlo As Source_Base In App.gaSrc
            Try
                Await oZrodlo.ReadData(bMsg)
                If bMsg Then ProgRingInc()
            Catch ex As Exception
                ' zeby jeden błędny nie powodował zniknięcia reszty
            End Try
        Next

        Await ZapiszCache()
    End Function

    Protected Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
        Select Case sCommand.ToLower
            Case "check now"
                Await SciagnijDane(False)
                Return "Done"
        End Select
        Return ""
    End Function

    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        ' tile update / warnings
        moTaskDeferal = args.TaskInstance.GetDeferral() ' w pkarmodule.App

        Dim bNoComplete As Boolean = False
        Dim bObsluzone As Boolean = False
        Select Case args.TaskInstance.Task.Name
            Case "Wycofania_Timer"
                SetSettingsString("lastTimerRun", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                If NetIsIPavailable(False) Then Await SciagnijDane(False)
                bObsluzone = True
        End Select

        Dim sLocalCmds As String = "check now" & vbTab & ": check data now"

        If Not bObsluzone Then bNoComplete = RemSysInit(args, sLocalCmds)

        If Not bNoComplete Then moTaskDeferal.Complete()

    End Sub


End Class
