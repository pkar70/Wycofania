' https://www.gov.pl/web/gis/ostrzezenia
Public Class Source_GIS
    Inherits Source_Base

    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Główny Inspektor Sanitarny"
    Protected Overrides Property SRC_SETTING_NAME As String = "GIS"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://www.gov.pl/web/gis/podstawowe-informacje"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://www.gov.pl/web/gis/ostrzezenia"
    Public Overrides Async Function ReadData(bMsg As Boolean) As Task(Of ObjectModel.Collection(Of JednoPowiadomienie))

        'DebugOut("  overridinig lastid")

        Dim oRetList As New ObjectModel.Collection(Of JednoPowiadomienie)

        Dim iLimit As Integer = 20 ' tu właściwie niepotrzebny - bo i tak strona ma tylko 10 pozycji
        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        ' https://rdg.ezdrowie.gov.pl/, 7 kB
        Dim sPage As String = Await HttpPageAsync("https://www.gov.pl/web/gis/ostrzezenia")
        If sPage = "" Then Return Nothing

        Dim iInd As Integer = sPage.IndexOf("<h2>Ostrzeżenia</h2>")
        If iInd < 10 Then
            If bMsg Then DialogBox("ERROR parsing data (h2)")
            Return Nothing
        End If

        iInd = sPage.IndexOf("<ul", iInd)
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</ul>")
        sPage = sPage.Substring(0, iInd + 5)

        Dim oXml As System.Xml.XmlDocument = New Xml.XmlDocument
        oXml.LoadXml(sPage)
        DebugOut("  XML loaded")

        ' mogłoby być sprawdzenie kolumn (w THEAD, sprawdzanie tekstów, i zapisanie index kolumny)
        ' z tej strony moze byc: data, nazwa produktu -> tytul, link. Nie ma fulldata, ktora jest brana dopiero z linku. Ale za to łatwiej wyciągnąc dane :)
        Dim oElems As Xml.XmlNodeList = oXml.GetElementsByTagName("li")
        If oElems.Count < 1 Then
            If bMsg Then DialogBox("ERROR parsing data (li)")
            Return Nothing
        End If

        Dim sFirstId As String = ""

        DebugOut("iterating ROWSy")
        ' kolejne produkty - <li
        For Each oRow As Xml.XmlNode In oElems

            Dim oNew As JednoPowiadomienie = NewPowiadomienie()

            Dim sItemText As String = oRow.InnerXml

            ' 1) data
            iInd = sItemText.IndexOf("class=""date")
            If iInd > 0 Then
                iInd = sItemText.IndexOf(">", iInd)
                oNew.sData = sItemText.Substring(iInd + 1)
                iInd = oNew.sData.IndexOf("<")
                oNew.sData = oNew.sData.Substring(0, iInd).Trim
                ' tu jest dd.mm.yyyy, obracamy
                oNew.sData = oNew.sData.Substring(6, 4) & oNew.sData.Substring(2, 4) & oNew.sData.Substring(0, 2)

                If oNew.sData < sLimitDate Then
                    DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                    Return oRetList
                End If

            End If

            ' 2) link - jak leci, jego sprawdzenie czy jeszcze nie występuje na liście (bez sLastId)
            iInd = sItemText.IndexOf("href")
            If iInd > 0 Then
                oNew.sLink = sItemText.Substring(iInd + 6)
                iInd = oNew.sLink.IndexOf("""")
                oNew.sLink = oNew.sLink.Substring(0, iInd)
                oNew.sLink = "https://www.gov.pl" & oNew.sLink
            End If

            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sLink = oNew.sLink Then
                    DebugOut("iteraing END because of ID")
                    Return oRetList
                End If
            Next

            ' 3) id - data + numerek (sprawdzanie w liście ile już jest)
            Dim iCnt As Integer = 0
            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sIcon = SRC_SETTING_NAME Then
                    If oItem.sData = oNew.sData Then iCnt += 1
                End If
            Next
            oNew.sId = oNew.sData & "." & iCnt


            ' 4) tytuł - wedle A, ale z wycieciem początku (dwukropek) do "ze względu", "z powodu", "produktu pn."
            iInd = sItemText.IndexOf("class=""title")
            If iInd > 0 Then
                iInd = sItemText.IndexOf(">", iInd)
                oNew.sTitle = sItemText.Substring(iInd + 1)
                iInd = oNew.sTitle.IndexOf("<")
                oNew.sTitle = oNew.sTitle.Substring(0, iInd)

                oNew.sTitle = oNew.sTitle.Replace("Ostrzeżenie publiczne dotyczące żywności: ", "")
                oNew.sTitle = oNew.sTitle.Replace("Ostrzeżenie publiczne dotyczące wyrobu do kontaktu z żywnością: ", "")

                oNew.sTitle = oNew.sTitle.Replace("Ostrzeżenie publiczne dotyczące ", "")

                iInd = oNew.sTitle.IndexOf("produktu pn.")
                If iInd > 0 Then
                    oNew.sTitle = oNew.sTitle.Substring(iInd + "produktu pn.".Length).Trim
                Else
                    iInd = oNew.sTitle.IndexOf("produktu ")
                    If iInd > 0 Then oNew.sTitle = oNew.sTitle.Substring(iInd + "produktu ".Length).Trim
                End If

                iInd = oNew.sTitle.IndexOf("ze względu")
                If iInd < 1 Then iInd = oNew.sTitle.IndexOf("z powodu")
                If iInd > 0 Then oNew.sTitle = oNew.sTitle.Substring(0, iInd).Trim
            End If


            ' 5) html - chyba z danych strony

            DebugOut("  trying to get details for " & oNew.sTitle & "(@" & oNew.sData)
            'Public Property sHtmlInfo As String = ""
            sPage = Await HttpPageAsync(oNew.sLink, "", False)
            DebugOut("  got details HTML page")

            iInd = sPage.IndexOf("<article")
            If iInd > 10 Then
                ' iInd = sPage.LastIndexOf("<div", iInd - 1)
                sPage = sPage.Substring(iInd)
                iInd = sPage.IndexOf("</article")
                sPage = sPage.Substring(0, iInd + 10)

                ' podmiana linkow do obrazków
                sPage = sPage.Replace("""/photo", """https://www.gov.pl/photo")
            End If

            oNew.sHtmlInfo = sPage

            DebugOut("Adding " & oNew.sTitle)
            oRetList.Add(oNew)

            iLimit -= 1
            If iLimit < 0 Then
                DebugOut("iteraing END because of INT limit")
                Return oRetList
            End If

        Next

        DebugOut("doszlismy do konca strony, i dalej nie ma tego co już widzieliśmy - pewnie init, nie wczytuję więcej")

        Return oRetList

    End Function

End Class
