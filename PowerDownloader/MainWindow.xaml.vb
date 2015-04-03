Imports System.ComponentModel
Imports System.Runtime.Serialization
Imports System.Collections.ObjectModel
Imports System.Reflection
Imports System.Runtime.InteropServices
'Here is Code
Class MainWindow
	Implements INotifyPropertyChanged

	Private ForceClose As Boolean = False

	Private WithEvents TMR As New Timers.Timer With {.Enabled = True, .Interval = 400}

	Private WithEvents Saver As New Timers.Timer With {.Enabled = True, .Interval = 10000}

	

	Public ReadOnly Property Downloads As ObservableCollection(Of DownloadHandler)
		Get
			Return DownloadManager.Downloads
		End Get
	End Property

	Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
		Try

			Dim Downloader = CType(CType(sender, Button).Tag, DownloadHandler)
			If Downloader.IsWorking Then
				Downloader.Stop()
			End If
			If Downloader.Status = DownloadStatus.Idle Or Downloader.Status = DownloadStatus.Error Then
				Downloader.Start()
			End If
			If Downloader.Status = DownloadStatus.Completed Then
				Try
					Process.Start(Downloader.File)
				Catch ex As Exception
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
				End Try
			End If

		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
		Try
			SaveAll()
			If Not ForceClose Then
				e.Cancel = True
				Me.Hide()
			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub AddDownload()
		Try
			Dim Adder As New DownloadAdder
			If Adder.ShowDialog Then
				Dim Dh = DownloadManager.Add(Adder.TargetFile, Adder.URL)
				If Settings.Instance.ResumeDownload Then
					If IO.File.Exists(Adder.TargetFile) Then
						Dim fi = New IO.FileInfo(Adder.TargetFile)
						Dh.Offset = fi.Length
					End If
				End If
				RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Downloads"))
			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub RemoveDownload()
		Try
			If lst.SelectedItems.Count > 0 Then
				If MessageBox.Show("Do You Want to Delete Selected Item?", "Options", MessageBoxButton.YesNo) = MessageBoxResult.Yes Then
					Dim l As New List(Of DownloadHandler)
					For Each si As DownloadableObject In lst.SelectedItems
						si.Stop()
						l.Add(si)
					Next
					For Each x In l
						DownloadManager.Remove(x)
					Next
				End If

			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub RemoveAll()
		Try
			If MessageBox.Show("Do You Want to Delete All Items?", "Options", MessageBoxButton.YesNo) = MessageBoxResult.Yes Then
				Dim l As New List(Of DownloadHandler)
				For Each i In DownloadManager.Downloads
					l.Add(i)
				Next
				For Each i In l
					i.Stop()
					DownloadManager.Remove(i)
				Next
			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub StartDownload()
		Try
			For Each se In lst.SelectedItems
				CType(se, DownloadHandler).Start()
			Next
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub StopDownload()
		Try
			For Each se In lst.SelectedItems
				CType(se, DownloadHandler).Stop()
			Next
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub StartAll()
		Try
			For Each i In lst.Items
				CType(i, DownloadHandler).Start()
			Next
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub StopAll()
		Try
			For Each i In lst.Items
				CType(i, DownloadHandler).Stop()
			Next
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ExportAll()
		Try
			If SFD.ShowDialog Then
				DownloadManager.ExportTo(SFD.FileName)
			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ImportAll()
		Try
			If OFD.ShowDialog Then
				DownloadManager.ImportFrom(OFD.FileName)
			End If
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ShowSettings()
		Try
			Dim Sett As New SettingsDialog
			Sett.ShowDialog()
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ShowHelp()
		Try
			MessageBox.Show("Help Will Be Available Soon", "?")
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ShowAbout()
		Try
			MessageBox.Show("Copyright © Atosoft 2015", "Power Downloader")
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub ExitApp()
		Try
			ForceClose = True
			Me.Close()
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Sub SaveFrame(fn As String)
		Dim rtb As New RenderTargetBitmap(CInt(Me.ActualWidth), CInt(Me.ActualHeight), 96, 96, PixelFormats.Pbgra32)
		rtb.Render(Me)

		Dim png As New PngBitmapEncoder()
		png.Frames.Add(BitmapFrame.Create(rtb))
		Using f = IO.File.OpenWrite(fn)
			png.Save(f)
		End Using

	End Sub

	Public Sub Init() Handles Me.Initialized
		Try

			If Settings.Instance.SetAsStartup Then
			End If

			DownloadManager.Initialize()

			Dim Cmd = Environment.GetCommandLineArgs()
			If Cmd.Count > 1 AndAlso Cmd(1) = "-" Then
				Me.Visibility = Windows.Visibility.Hidden
			End If

			Notifier.LeftClickCommand = New ShowCommand
			Notifier.LeftClickCommandParameter = Me

			Notifier.DoubleClickCommand = Notifier.LeftClickCommand
			Notifier.DoubleClickCommandParameter = Me


			Me.DataContext = Me

			LoadAll()

			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Downloads"))

			If Settings.Instance.StartOnStart Then
				For Each dw As DownloadHandler In Downloads
					dw.Start()
				Next
			End If

			TMR.Interval = Settings.Instance.RefreshRate

		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub

	Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

	Private Sub TMR_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles TMR.Elapsed
		Try
			For Each dm As DownloadHandler In Downloads
				dm.Update()
			Next
		Catch ex As Exception
			CauseUnhandledError(ex)
		End Try
	End Sub


	Private Sub Saver_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles Saver.Elapsed
		SaveAll()
	End Sub

	Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		SaveFrame("1.png")
	End Sub
End Class
Public Class ShowCommand
	Implements ICommand

	Public Sub Execute(parameter As Object) Implements ICommand.Execute
		CType(parameter, Window).Show()
	End Sub

	Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
		Return True
	End Function

	Public Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged
End Class