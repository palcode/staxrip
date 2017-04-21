Imports System.ComponentModel
Imports StaxRip.UI

Public Class AviSynthListView
    Inherits ListViewEx

    Private BlockItemCheck As Boolean
    WithEvents Menu As New ContextMenuStripEx
    Property IsLoading As Boolean

    Event Changed()

    Sub New()
        AllowDrop = True
        Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        CheckBoxes = True
        View = View.Details
        HideSelection = False
        FullRowSelect = True
        MultiSelect = False
        HeaderStyle = ColumnHeaderStyle.None
        Columns.Add("")
        Columns.Add("Type")
        Columns.Add("Name")
        Menu.Font = New Font("Segoe UI", 9 * s.UIScaleFactor)
        ContextMenuStrip = Menu
        SendMessageHideFocus()
        AddHandler VideoScript.Changed, Sub(script As VideoScript)
                                            If Not ProfileFunc Is Nothing AndAlso script Is ProfileFunc.Invoke Then
                                                OnChanged()
                                            End If
                                        End Sub
    End Sub

    <Browsable(False),
    DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Property ProfileFunc As Func(Of VideoScript)

    Sub Load()
        BlockItemCheck = True
        Items.Clear()
        BeginUpdate()

        For Each i In ProfileFunc.Invoke.Filters
            Dim item As New ListViewItem
            item.Tag = i
            item.Checked = i.Active
            item.SubItems.Add(i.Category)
            If i.Name = "" Then item.SubItems.Add(i.Script) Else item.SubItems.Add(i.Name)
            item.SubItems.Add(i.Script)
            Items.Add(item)
        Next

        AutoResizeColumns(True)
        EndUpdate()
        BlockItemCheck = False
    End Sub

    Sub UpdateMenu()
        Dim filterProfiles As List(Of FilterCategory)

        If p.Script.Engine = ScriptEngine.AviSynth Then
            filterProfiles = s.AviSynthProfiles
        Else
            filterProfiles = s.VapourSynthProfiles
        End If

        Menu.Items.Clear()

        If SelectedItems.Count > 0 Then
            Dim selectedFilter = DirectCast(SelectedItems(0).Tag, VideoFilter)

            For Each i In filterProfiles
                If i.Name = selectedFilter.Category Then
                    For Each i2 In i.Filters
                        Dim tip = i2.Script
                        ActionMenuItem.Add(Menu.Items, i2.Category + " | " + i2.Path, AddressOf ReplaceClick, i2.GetCopy, tip)
                    Next
                End If
            Next

            If Menu.Items.Count > 0 Then Menu.Items.Add(New ToolStripSeparator)

            Dim replace = Menu.Add("Replace")
            Dim replaceFirst = Menu.Add("Replace | a")

            AddHandler replace.DropDownOpened, Sub()
                                                   If replace.DropDownItems.Count > 1 Then Exit Sub
                                                   replace.DropDownItems.RemoveAt(0)

                                                   For Each i In filterProfiles
                                                       For Each i2 In i.Filters
                                                           Dim tip = i2.Script
                                                           ActionMenuItem.Add(replace.DropDownItems, i.Name + " | " + i2.Path, AddressOf ReplaceClick, i2.GetCopy, tip)
                                                           Application.DoEvents()
                                                       Next
                                                   Next
                                               End Sub
            Dim insert = Menu.Add("Insert")
            Dim insertFirst = Menu.Add("Insert | a")

            AddHandler insert.DropDownOpened, Sub()
                                                  If insert.DropDownItems.Count > 1 Then Exit Sub
                                                  insert.DropDownItems.RemoveAt(0)

                                                  For Each i In filterProfiles
                                                      For Each i2 In i.Filters
                                                          Dim tip = i2.Script
                                                          ActionMenuItem.Add(insert.DropDownItems, i.Name + " | " + i2.Path, AddressOf InsertClick, i2.GetCopy, tip)
                                                          Application.DoEvents()
                                                      Next
                                                  Next
                                              End Sub
        End If

        Dim add = Menu.Add("Add")
        add.SetImage(Symbol.Add)
        Dim addFirst = Menu.Add("Add | a")

        AddHandler add.DropDownOpened, Sub()
                                           If add.DropDownItems.Count > 1 Then Exit Sub
                                           add.DropDownItems.RemoveAt(0)

                                           For Each i In filterProfiles
                                               For Each i2 In i.Filters
                                                   Dim tip = i2.Script
                                                   ActionMenuItem.Add(add.DropDownItems, i.Name + " | " + i2.Path, AddressOf AddClick, i2.GetCopy, tip)
                                                   Application.DoEvents()
                                               Next
                                           Next
                                       End Sub

        If SelectedItems.Count > 0 Then
            Menu.Add("-")
            Menu.Add("Remove", AddressOf RemoveClick, "Removes the selected filter.").SetImage(Symbol.Remove)
        End If

        Menu.Add("Edit Code...", AddressOf ShowEditor, "Dialog to edit filters.").SetImage(Symbol.Code)
        Menu.Add("Preview Code...", Sub() g.CodePreview(p.Script.GetFullScript), "Script code preview.")
        Menu.Add("Play", Sub() g.PlayScript(p.Script), "Plays the script with the AVI player.").SetImage(Symbol.Play)
        Menu.Add("Profiles...", AddressOf g.MainForm.ShowFilterProfilesDialog, "Dialog to edit profiles.")

        Dim setup = Menu.Add("Filter Setup")
        setup.SetImage(Symbol.MultiSelect)
        g.PopulateProfileMenu(setup.DropDownItems, s.FilterSetupProfiles, AddressOf g.MainForm.ShowFilterSetupProfilesDialog, AddressOf g.MainForm.LoadScriptProfile)
    End Sub

    Sub ShowEditor()
        If ProfileFunc().Invoke.Edit = DialogResult.OK Then OnChanged()
    End Sub

    Sub ReplaceClick(filter As VideoFilter)
        Dim tup = Macro.ExpandGUI(filter.Script)
        If tup.cancel Then Exit Sub
        If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then filter.Path += " " + tup.Caption
        filter.Script = tup.value
        Dim index = SelectedItems(0).Index
        ProfileFunc.Invoke.SetFilter(index, filter)
        Items(index).Selected = True
    End Sub

    Private Sub InsertClick(filter As VideoFilter)
        Dim tup = Macro.ExpandGUI(filter.Script)
        If tup.cancel Then Exit Sub
        If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then filter.Path += " " + tup.Caption
        filter.Script = tup.value
        Dim index = SelectedItems(0).Index
        ProfileFunc.Invoke.InsertFilter(index, filter)
        Items(index).Selected = True
    End Sub

    Private Sub AddClick(filter As VideoFilter)
        Dim tup = Macro.ExpandGUI(filter.Script)
        If tup.cancel Then Exit Sub
        If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then filter.Path += " " + tup.Caption
        filter.Script = tup.value
        ProfileFunc.Invoke.AddFilter(filter)
        Items(Items.Count - 1).Selected = True
    End Sub

    Sub OnChanged()
        If IsLoading Then Exit Sub
        Load()
        RaiseEvent Changed()
    End Sub

    Sub RaiseChangedAsync()
        Dim async = Sub()
                        Application.DoEvents()
                        OnChanged()
                    End Sub

        g.MainForm.BeginInvoke(async)
    End Sub

    Private Sub RemoveClick()
        If Items.Count > 1 Then ProfileFunc.Invoke.RemoveFilterAt(SelectedItems(0).Index)
    End Sub

    Sub UpdateDocument()
        ProfileFunc.Invoke.Filters.Clear()

        For Each i As ListViewItem In Items
            ProfileFunc.Invoke.Filters.Add(DirectCast(i.Tag, VideoFilter))
        Next

        OnChanged()
    End Sub

    Protected Overrides Sub OnCreateControl()
        MyBase.OnCreateControl()
        AutoResizeColumns(True)
    End Sub

    Protected Overrides Sub OnLayout(e As LayoutEventArgs)
        MyBase.OnLayout(e)
        AutoResizeColumns(True)
    End Sub

    Protected Overrides Sub OnDragDrop(e As DragEventArgs)
        BlockItemCheck = True
        MyBase.OnDragDrop(e)
        BlockItemCheck = False
        UpdateDocument()
    End Sub

    Protected Overrides Sub OnItemCheck(e As ItemCheckEventArgs)
        MyBase.OnItemCheck(e)

        If Not BlockItemCheck AndAlso Focused Then
            Dim filter = DirectCast(Items(e.Index).Tag, VideoFilter)

            If e.NewValue = CheckState.Checked AndAlso filter.Category = "Resize" Then
                Dim f = FindForm()

                If Not f Is Nothing AndAlso TypeOf f Is MainForm Then
                    g.MainForm.SetTargetImageSize(p.TargetWidth, 0)
                End If
            End If

            filter.Active = e.NewValue = CheckState.Checked
            RaiseChangedAsync()
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then UpdateMenu()
        MyBase.OnMouseUp(e)
    End Sub
End Class