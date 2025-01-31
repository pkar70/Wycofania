
' VBLibek wersja

'https://ec.europa.eu/consumers/consumers_safety/safety_products/rapex/alerts/?event=main.immediatlyPublishedNotifications - niezdrowotny

'https://ec.europa.eu/consumers/consumers_safety/safety_products/rapex/alerts/?event=main.immediatlyPublishedNotifications - niezdrowotny

Public Class Source_Rapex
    Inherits Source_Base
    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Rapid Exchange of Information System (EU)"
    Protected Overrides Property SRC_SETTING_NAME As String = "RAPEX"
    Protected Overrides Property SRC_DEFAULT_ONETOAST As Boolean = True
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://ec.europa.eu/safety-gate"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://ec.europa.eu/safety-gate-alerts/screen/webReport"

    ' ale początek i koniec funkcji (maketoast) jest do zrobienia w UWP
    Public Overrides Async Function ReadData(bMsg As Boolean) As Task(Of ObjectModel.Collection(Of JednoPowiadomienie))

        Dim iWeeks As Integer = GetSettingsInt(SRC_SETTING_NAME & "_Slider", SRC_MAXWEEKS)

        Dim oLimitDate As Date = Date.Now.AddDays(-7 * iWeeks)
        Dim iLimit As Integer = 20

        Dim oResponse As JSONrapexPage
        Dim iPageNo As Integer = 0

        Dim oRetList As New ObjectModel.Collection(Of JednoPowiadomienie)

        ' Return ' ***********************************************************************


        Do
            oResponse = Await RapexGetResultPageAsync(iPageNo, bMsg)
            If oResponse Is Nothing Then Return oRetList
            For Each oAlert As JSONrapexContent In oResponse.content

                Dim oNew As JednoPowiadomienie = NewPowiadomienie()


                oNew.sId = oAlert.id
                oNew.sLink = "https://ec.europa.eu/safety-gate-alerts/screen/webReport/alertDetail/" & oNew.sId
                For Each oItem As JednoPowiadomienie In App.glItems
                    If oItem.sLink = oNew.sLink Then
                        DebugOut("iteraing END because of ID")
                        Exit Do
                    End If
                Next

                If oAlert.publicationDate < oLimitDate Then
                    DebugOut("iteraing END because of date")
                    Exit Do
                End If
                oNew.sData = oAlert.publicationDate.ToString("yyyy.MM.dd HH:mm:ss")

                oNew.sTitle = ""
                If oAlert.product.brandKnown Then
                    For Each oBrand As JSONrapexBrand In oAlert.product.brands
                        If oNew.sTitle <> "" Then oNew.sTitle += " / "
                        oNew.sTitle += oBrand.brand
                    Next
                End If

                If oAlert.product.nameSpecificKnown Then
                    If oNew.sTitle <> "" Then oNew.sTitle += " - "
                    oNew.sTitle += oAlert.product.nameSpecific
                End If

                If oNew.sTitle = "" Then
                    oNew.sTitle = "(brak danych marki i modelu)"
                End If

                '' zakładam że pierwsza sekcja mówi o danych produktu
                'Dim iInd As Integer = sPage.IndexOf("<section")
                'If iInd > 0 Then
                '    sPage = sPage.Substring(iInd)
                '    iInd = sPage.IndexOf("</section")
                '    sPage = sPage.Substring(0, iInd + "</section>".Length)
                '    oNew.sHtmlInfo = sPage
                'End If
                ' https://ec.europa.eu/safety-gate-alerts/public/api/notification/10003080?language=pl
                Dim oDetails As JSONdetail = Await RapexGetDetailsPageAsync(oNew.sId, bMsg)
                Dim sTxt = oDetails.ToString.Replace(vbCrLf, "<br/>")

                If oAlert.product.photos IsNot Nothing Then
                    For Each oFoto As JSONrapexPhoto In oAlert.product.photos
                        If oFoto.mainPicture Then
                            sTxt = sTxt & "<p><img src='https://ec.europa.eu/safety-gate-alerts/public/api/notification/image/" & oFoto.id & "'>"
                        End If
                    Next
                End If

                oNew.sHtmlInfo = sTxt

                oRetList.Add(oNew)

                iLimit -= 1
                If iLimit < 0 Then
                    DebugOut("iteraing END because of INT limit")
                    Return oRetList
                End If

            Next

            iPageNo += 1
        Loop Until oResponse.last

        Return oRetList

    End Function
    Private Shared Async Function RapexGetDetailsPageAsync(sId As String, bMsg As Boolean) As Task(Of JSONdetail)

        Dim sUrl As String = "https://ec.europa.eu/safety-gate-alerts/public/api/notification/" & sId & "?language=pl"

        Dim moHttp As New Net.Http.HttpClient
        moHttp.DefaultRequestHeaders.Accept.Clear()
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"))
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))

        Dim sError As String = ""
        Dim oResp As Net.Http.HttpResponseMessage = Nothing

        Try
            oResp = Await moHttp.GetAsync(New Uri(sUrl))
        Catch ex As Exception
            EwentualnieToastError("@RapexGetDetailsPageAsync GetAsync " & sUrl, ex)
            Return Nothing
        End Try

        'If sError <> "" Then
        '    sError = "error " & sError & " at RAPEX get details page"
        '    If bMsg Then
        '        Await DialogBoxAsync(sError)
        '    Else
        '        CrashMessageAdd("@HttpPageAsync1", sError)
        '    End If
        '    Return Nothing
        'End If

        If oResp.StatusCode > 290 Then
            sError = "ERROR " & oResp.StatusCode & " at RAPEX get details page"
            If bMsg Then
                Await DialogBoxAsync(sError)
            Else
                EwentualnieToastError("@RapexGetDetailsPageAsync2", sError)
            End If
            Return Nothing
        End If


        Dim sResp As String = ""
        Try
            sResp = Await oResp.Content.ReadAsStringAsync
        Catch ex As Exception
            EwentualnieToastError("@RapexGetDetailsPageAsync ReadAsStringAsync " & sUrl, ex)
            ' sError = ex.Message
            Return Nothing
        End Try

        'If sError <> "" Then
        '    sError = "error " & sError & " at ReadAsStringAsync in RAPEX get details page"
        '    If bMsg Then
        '        Await DialogBoxAsync(sError)
        '    Else
        '        CrashMessageAdd("@HttpPageAsync3", sError)
        '    End If
        '    Return Nothing
        'End If

        Dim oResponse As JSONdetail = Newtonsoft.Json.JsonConvert.DeserializeObject(sResp, GetType(JSONdetail))
        Return oResponse

    End Function

    Private Shared _rapexErrorCount As Integer

    Private Shared Sub EwentualnieToastError(sCheckpoint As String, sMessage As String)

        _rapexErrorCount += 1
        If _rapexErrorCount < 3 Then Return

        CrashMessageAdd(sCheckpoint, sMessage)
        _rapexErrorCount = 0

    End Sub

    Private Shared Sub EwentualnieToastError(sCheckpoint As String, ex As Exception)

        _rapexErrorCount += 1
        If _rapexErrorCount < 3 Then Return

        CrashMessageAdd(sCheckpoint, ex?.Message)
        _rapexErrorCount = 0

    End Sub


    Private Shared Async Function RapexGetResultPageAsync(iPageNo As Integer, bMsg As Boolean) As Task(Of JSONrapexPage)

        Dim sUrl As String = "https://ec.europa.eu/safety-gate-alerts/public/api/notification/carousel/?language=pl"
        Dim sData As String = "{""language"":""pl"",""page"":""" & iPageNo & """}"

        Dim moHttp As New Net.Http.HttpClient
        moHttp.DefaultRequestHeaders.Accept.Clear()
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"))
        moHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
        Dim oHttpCont = New Net.Http.StringContent(sData, Text.Encoding.UTF8, "application/json")

        Dim sError As String = ""
        Dim oResp As Net.Http.HttpResponseMessage = Nothing
        Try
            oResp = Await moHttp.PostAsync(New Uri(sUrl), oHttpCont)
        Catch ex As Exception
            sError = ex.Message
        End Try

        If sError <> "" Then
            sError = $"error {sError} at RAPEX get page {iPageNo}"
            If bMsg Then
                Await DialogBoxAsync(sError)
            Else
                EwentualnieToastError("@HttpPageAsync1", sError)
            End If
            Return Nothing
        End If

        If oResp.StatusCode > 290 Then
            sError = $"ERROR {oResp.StatusCode} at RAPEX get page"
            If bMsg Then
                Await DialogBoxAsync(sError)
            Else
                EwentualnieToastError("@HttpPageAsync2", sError)
            End If
            Return Nothing
        End If


        Dim sResp As String = ""
        Try
            sResp = Await oResp.Content.ReadAsStringAsync
        Catch ex As Exception
            sError = ex.Message
        End Try

        If sError <> "" Then
            sError = "error ${sError} at ReadAsStringAsync in RAPEX get page"
            If bMsg Then
                Await DialogBoxAsync(sError)
            Else
                EwentualnieToastError("@HttpPageAsync3", sError)
            End If
            Return Nothing
        End If

        Dim oResponse As JSONrapexPage = Newtonsoft.Json.JsonConvert.DeserializeObject(sResp, GetType(JSONrapexPage))
        Return oResponse

    End Function

End Class

Namespace Global

#Region "JSON dla listy produktów"
    Public Class JSONrapexPage
        Public Property content As List(Of JSONrapexContent)
        Public Property last As Boolean
        Public Property totalPages As Integer
        Public Property first As Boolean
    End Class

    Public Class JSONrapexContent
        Public Property id As Integer
        Public Property publicationDate As Date ' ewentualnie string, 2021-03-17T15:35:11.686+0000
        Public Property product As JSONrapexProduct
    End Class
    Public Class JSONrapexProduct
        Public Property nameSpecific As String
        Public Property nameSpecificKnown As Boolean
        Public Property brandKnown As Boolean
        Public Property brands As List(Of JSONrapexBrand)
        Public Property photos As List(Of JSONrapexPhoto)
    End Class
    Public Class JSONrapexBrand
        Public Property brand As String
    End Class
    Public Class JSONrapexPhoto
        Public Property id As Integer
        Public Property mainPicture As Boolean
    End Class
#End Region

#Region "JSON dla details produktu"
    Public Class JSONdetail
        Public Property id As Integer
        Public Property reference As String
        Public Property country As JSONdetailCountry
        Public Property product As JSONdetailProduct
        Public Property risk As JSONdetailRisk
        Public Property measureTaken As JSONdetailMeasure
        Public Property versions As List(Of JSONdetailVersion)

        Public Overrides Function ToString() As String
            Dim sTxt As String = ""

            sTxt = product.ToString & risk.ToString

            If versions IsNot Nothing Then
                For Each oItem As JSONdetailVersion In versions
                    If oItem.language.key = "PL" Then
                        sTxt = sTxt & vbCrLf & "Corrigendum: " & oItem.corrigendum & vbCrLf
                        Exit For
                    End If
                Next
            End If

            Return sTxt
        End Function

    End Class
    Public Class JSONdetailCountry
        Public Property name As String
    End Class
    Public Class JSONdetailProduct
        Public Property nameSpecific As String
        Public Property nameSpecificKnown As Boolean
        Public Property brandKnown As Boolean
        Public Property brands As List(Of JSONrapexBrand)
        Public Property photos As List(Of JSONrapexPhoto)

        Public Property barcodeKnown As Boolean
        Public Property versions As List(Of JSONdetailProdVersion)
        Public Property barcodes As List(Of JSONdetailBarCode)
        Public Property batchNumbers As List(Of JSONdetailBatchNumber)

        Public Overrides Function ToString() As String
            Dim sTxt As String = ""
            If brandKnown Then
                sTxt = sTxt & "Marka: "
                For Each oItem As JSONrapexBrand In brands
                    sTxt = sTxt & oItem.brand & ", "
                Next
                sTxt &= vbCrLf
            End If

            If nameSpecificKnown Then
                sTxt = sTxt & "Model: " & nameSpecific & vbCrLf
            End If

            Return sTxt
        End Function
    End Class
    Public Class JSONdetailRisk
        Public Property riskType As List(Of JSONkeyName)
        Public Property versions As List(Of JSONriskVersion)

        Public Overrides Function ToString() As String
            Dim sTxt As String = ""
            For Each oItem As JSONkeyName In riskType
                sTxt = sTxt & oItem.name & ", "
            Next
            If sTxt <> "" Then sTxt = "Typ ryzyka: " & sTxt & vbCrLf

            For Each oItem As JSONriskVersion In versions
                sTxt = sTxt & "Opis: " & oItem.riskDescription & vbCrLf
                sTxt = sTxt & "Przepisy: " & oItem.legalProvision & vbCrLf
            Next

            Return sTxt
        End Function
    End Class

    Public Class JSONriskVersion
        Public Property riskDescription As String
        Public Property legalProvision As String
    End Class

    Public Class JSONdetailMeasure
        Public Property hasPublishedRecallOnline As JSONkeyName
        Public Property measures As List(Of JSONdetailOneMeasure)
    End Class

    Public Class JSONdetailOneMeasure
        Public Property measureCategory As JSONkeyName
        Public Property measureType As JSONkeyName
        Public Property measureCompulsoryEconomicOperator As JSONkeyName

    End Class
    Public Class JSONdetailVersion
        Public Property language As JSONkeyName
        Public Property corrigendum As String   ' może byc null (najczesciej JEST null)
    End Class
    Public Class JSONdetailProdVersion
        Public Property name As String
        Public Property description As String
        Public Property packageDescription As String
        Public Property productCategoryOther As String
    End Class
    Public Class JSONdetailBarCode
        Public Property versions As List(Of JSONdetailBarCodeVersion)
    End Class
    Public Class JSONdetailBatchNumber
        Public Property versions As List(Of JSONdetailBatchVersion)
    End Class
    Public Class JSONdetailBarCodeVersion
        Public Property barcode As String
    End Class
    Public Class JSONdetailBatchVersion
        Public Property batchNumber As String
    End Class

    Public Class JSONkeyName
        Public Property key As String
        Public Property name As String
    End Class
#End Region

End Namespace
