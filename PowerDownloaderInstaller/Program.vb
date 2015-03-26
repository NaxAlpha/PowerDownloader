Imports System.IO

Module Program

	Sub Main()

		Dim Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atosoft\Downloader")

		If Not Directory.Exists(Dir) Then
			Directory.CreateDirectory(Dir)
		End If

		Dim Exe = Path.Combine(Dir, "PowerDownloader.exe")

		File.WriteAllBytes(Exe, My.Resources.PowerDownloader)
		File.WriteAllBytes(Path.Combine(Dir, "Hardcodet.Wpf.TaskbarNotification.dll"), My.Resources.Hardcodet_Wpf_TaskbarNotification)
		File.WriteAllBytes(Path.Combine(Dir, "PresentationFramework.Aero.dll"), My.Resources.PresentationFramework_Aero)

		Directory.SetCurrentDirectory(Dir)

		Process.Start("PowerDownloader.exe")



	End Sub

End Module
