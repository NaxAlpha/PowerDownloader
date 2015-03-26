Public Class SettingsDialog

	Sub OnLoaded() Handles Me.Loaded
		Me.DataContext = Me
	End Sub

	Public ReadOnly Property Settings As Settings
		Get
			Return Global.Atosoft.Net.DownloadEngine.Settings.Instance
		End Get
	End Property

	Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
		If FBD.ShowDialog = Forms.DialogResult.OK Then
			txtSavePath.Text = FBD.SelectedPath
		End If
	End Sub

	Private Sub txtSavePath_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtSavePath.TextChanged
		
	End Sub

End Class
