Public Class DownloadAdder

	Public ReadOnly Property URL As String
		Get
			Return txtURL.Text
		End Get
	End Property

	Public ReadOnly Property TargetFile As String
		Get
			Return txtSavePath.Text
		End Get
	End Property

	Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
		Me.DialogResult = False
	End Sub

	Private Sub Button_Click_1(sender As Object, e As RoutedEventArgs)
		Me.DialogResult = True
	End Sub

	Private Sub DownloadAdder_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		Try
			txtURL.Text = Clipboard.GetText()
			txtSavePath.Text = IO.Path.Combine(Settings.Instance.SavePath, IO.Path.GetFileName(txtURL.Text))
		Catch ex As Exception
			txtSavePath.Text = Settings.Instance.SavePath
		End Try
	End Sub

	Private Sub txtURL_TextChanged(sender As Object, e As TextChangedEventArgs)
		Try 
			txtSavePath.Text = IO.Path.Combine(Settings.Instance.SavePath, IO.Path.GetFileName(txtURL.Text))
		Catch ex As Exception
		End Try
	End Sub

End Class
