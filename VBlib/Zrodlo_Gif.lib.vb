'https://rdg.ezdrowie.gov.pl/
Public Class Source_Gif
    Inherits Source_Base

    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Główny Inspektor Farmaceutyczny"
    Protected Overrides Property SRC_SETTING_NAME As String = "GIF"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://www.gov.pl/web/gif/rodzaje-wydawanych-decyzji"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://rdg.ezdrowie.gov.pl/"

    Public Overrides Async Function ReadData(bMsg As Boolean) As Task(Of ObjectModel.Collection(Of JednoPowiadomienie))

        'DebugOut("  overridinig lastid")

        Dim oRetList As New ObjectModel.Collection(Of JednoPowiadomienie)

        Dim iLimit As Integer = 20 ' tu właściwie niepotrzebny - bo i tak strona ma tylko 25 pozycji
        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        ' https://rdg.ezdrowie.gov.pl/, 20 kB
        Dim sPage As String = Await HttpPageAsync(New Uri("https://rdg.ezdrowie.gov.pl/"))
        If sPage = "" Then Return Nothing

        Dim iInd As Integer = sPage.IndexOf("table-decisions")
        If iInd < 10 Then
            If bMsg Then DialogBox("ERROR parsing data (tabledec)")
            Return Nothing
        End If

        iInd = sPage.LastIndexOf("<table", iInd)
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</table>")
        sPage = sPage.Substring(0, iInd + 8)

        Dim oXml As System.Xml.XmlDocument = New Xml.XmlDocument
        oXml.LoadXml(sPage)
        DebugOut("  XML loaded")

        ' mogłoby być sprawdzenie kolumn (w THEAD, sprawdzanie tekstów, i zapisanie index kolumny)
        ' z tej strony moze byc: data, nazwa produktu -> tytul, link. Nie ma fulldata, ktora jest brana dopiero z linku. Ale za to łatwiej wyciągnąc dane :)
        Dim oElems As Xml.XmlNodeList = oXml.GetElementsByTagName("tbody")
        If oElems.Count <> 1 Then
            If bMsg Then DialogBox("ERROR parsing data (tbody)")
            Return Nothing
        End If

        Dim iMaxId As Integer = 0

        DebugOut("iterating ROWSy")
        ' kolejne decyzje - <tr
        For Each oRow As Xml.XmlNode In oElems.Item(0).ChildNodes

            Dim oNew As JednoPowiadomienie = NewPowiadomienie()

            oNew.sTitle = oRow.ChildNodes.Item(1).InnerText

            oNew.sData = oRow.ChildNodes.Item(2).InnerText.Replace("-", ".")
            If oNew.sData < sLimitDate Then
                DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                Return oRetList     ' koniec czytania - starsze niż...
            End If

            oNew.sId = oRow.ChildNodes.Item(6).InnerXml.ToString
            iInd = oNew.sId.IndexOf("?id=")
            oNew.sId = oNew.sId.Substring(iInd + 4)
            iInd = oNew.sId.IndexOf("""")
            oNew.sId = oNew.sId.Substring(0, iInd)

            oNew.sLink = "https://rdg.ezdrowie.gov.pl/Decision/Decision?id=" & oNew.sId

            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sLink = oNew.sLink Then
                    DebugOut("iteraing END because of ID")
                    Return oRetList
                End If
            Next

            DebugOut("  trying to get details for " & oNew.sTitle & "(@" & oNew.sData)
            'Public Property sHtmlInfo As String = ""
            sPage = Await HttpPageAsync(New Uri(oNew.sLink), "", False)
            DebugOut("  got details HTML page")

            iInd = sPage.IndexOf("<h1")
            If iInd > 10 Then
                ' iInd = sPage.LastIndexOf("<div", iInd - 1)
                sPage = sPage.Substring(iInd)
                iInd = sPage.IndexOf("<footer")
                If iInd > 10 Then
                    iInd = sPage.LastIndexOf("</div", iInd - 1)
                    iInd = sPage.LastIndexOf("</div", iInd - 1)
                    iInd = sPage.LastIndexOf("</div", iInd - 1)
                    iInd = sPage.LastIndexOf("</div", iInd - 1)
                    sPage = sPage.Substring(0, iInd + 6)

                    ' wyrzucenie linku powrot
                    ' <a onclick="window.history.back();" style="margin:10px;" class="btn btn-success">Powrót</a>
                    iInd = sPage.IndexOf("onclick")
                    If iInd > 10 Then
                        iInd = sPage.LastIndexOf("<")
                        Dim iInd1 As Integer = sPage.IndexOf("</a>", iInd)
                        sPage = sPage.Substring(0, iInd - 1) & sPage.Substring(iInd1 + 4)
                    End If

                    ' podmiana linkow
                    sPage = sPage.Replace("href=""/Decision", "href=""https://rdg.ezdrowie.gov.pl/Decision")
                End If
            End If

            oNew.sHtmlInfo = sPage

            oRetList.Add(oNew)

            iLimit -= 1
            If iLimit < 0 Then
                DebugOut("iteraing END because of INT limit")
                Return oRetList
            End If

        Next

        Return oRetList
        ' doszlismy do konca strony, i dalej nie ma tego co już widzieliśmy - pewnie init, nie wczytuję więcej

    End Function
End Class
