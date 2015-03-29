Imports System.Net
Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Imports System.Text
Imports System.Security.Cryptography
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.ComponentModel
Imports System.Collections.ObjectModel
Imports Microsoft.Win32

Public Module APIs

	Friend SFD As New Microsoft.Win32.SaveFileDialog
	Friend OFD As New Microsoft.Win32.OpenFileDialog
	Friend FBD As New System.Windows.Forms.FolderBrowserDialog

	Public ReadOnly Property AppPath As String
		Get
			Return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atosoft\Downloader")
		End Get
	End Property

	Public Sub SaveAll()
		Settings.Save()
		DownloadManager.Save()
	End Sub
	Public Sub LoadAll()
		Settings.Load()
		DownloadManager.Load()
	End Sub

	Public Sub CauseUnhandledError(ex As Exception)
		MsgBox(ex.ToString, MsgBoxStyle.Critical, "Unhandled Error")
		End
	End Sub

	Public Sub PreStart()
		IO.Directory.SetCurrentDirectory(IO.Path.GetDirectoryName(Settings.ExePath))

		Dim di1 As New IO.DirectoryInfo(IO.Directory.GetCurrentDirectory)
		Dim di2 As New IO.DirectoryInfo(AppPath)

		If Not di2.Exists Then
			di2.Create()
		End If

		If di1.FullName.ToLower <> di2.FullName.ToLower Then
			Dim Target = IO.Path.Combine(AppPath, IO.Path.GetFileName(Settings.ExePath))
			IO.File.Copy(Settings.ExePath, Target, True)
			Process.Start(Target)
			End
		End If

		If Process.GetProcessesByName(IO.Path.GetFileNameWithoutExtension(Settings.ExePath)).Count > 1 Then
			End
		End If

		If Not IO.File.Exists("PresentationFramework.Aero.dll") Then
			IO.File.WriteAllBytes("PresentationFramework.Aero.dll", My.Resources.PresentationFramework_Aero)
		End If

		If Not IO.File.Exists("Hardcodet.Wpf.TaskbarNotification.dll") Then
			IO.File.WriteAllBytes("Hardcodet.Wpf.TaskbarNotification.dll", My.Resources.Hardcodet_Wpf_TaskbarNotification)
		End If
	End Sub

End Module
Public Class DownloadManager

	Private Shared BF As New BinaryFormatter
	Public Shared ReadOnly File As String = "Downlo.ads"
	Public Shared Property Downloads As New ObservableCollection(Of DownloadHandler)

	Public Shared Function Add(File As String, URL As String) As DownloadHandler
		Dim Dh = New DownloadHandler(File, URL)
		Downloads.Add(Dh)
		If Settings.Instance.StartOnAdded And Not Dh.IsCompleted Then
			Dh.Start()
		End If
		Return Dh
	End Function
	Public Shared Sub Remove(Dh As DownloadHandler)
		Downloads.Remove(Dh)
	End Sub

	Public Shared Sub Save()
		Using out = IO.File.OpenWrite(File)
			BF.Serialize(out, Downloads)
		End Using
	End Sub
	Public Shared Sub Load()
		If Not IO.File.Exists(File) Then Exit Sub
		Using inp = IO.File.OpenRead(File)
			Downloads = BF.Deserialize(inp)
			Refresh(Downloads)
		End Using
	End Sub

	Public Shared Sub ImportFrom(File As String)
		If Not IO.File.Exists(File) Then Exit Sub
		Using inp = IO.File.OpenRead(File)
			Dim Dws = BF.Deserialize(inp)
			Refresh(Dws)
			For Each obj In Dws
				Downloads.Add(obj)
			Next
		End Using
	End Sub
	Public Shared Sub ExportTo(File As String)
		Using out = IO.File.OpenWrite(File)
			BF.Serialize(out, Downloads)
		End Using
	End Sub

	Private Shared Sub Refresh(Downloads As ObservableCollection(Of DownloadHandler))
		For Each dm As DownloadHandler In Downloads
			If dm.Status = DownloadStatus.Working Or dm.Status = DownloadStatus.Starting Then
				dm._Status = DownloadStatus.Idle
			End If
			If dm.IsCompleted Then
				dm._Status = DownloadStatus.Completed
			End If
		Next
	End Sub

	Public Shared Sub Initialize()
		ServicePointManager.DefaultConnectionLimit = 100
		ServicePointManager.Expect100Continue = False
		ServicePointManager.UseNagleAlgorithm = False
		ServicePointManager.CheckCertificateRevocationList = True
	End Sub

End Class
Public Class Settings

	Dim Startup As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)

	Public Shared ReadOnly Property ExePath As String
		Get
			Return System.Environment.GetCommandLineArgs()(0)
		End Get
	End Property

	Friend Sub RunAtStartup()
		Startup.SetValue("Power Downloader", String.Format("""{0}"" -", ExePath, "-"))
	End Sub

	Private Sub RemoveFromStartup()
		Startup.DeleteValue("Power Downloader", False)
	End Sub

	Private Sub New()

	End Sub

	Private Shared BF As New BinaryFormatter
	Private Settings As New Hashtable
	Public Shared ReadOnly File As String = "Setti.ngs"
	Private Shared _Instance As Settings

	Public Shared ReadOnly Property Instance As Settings
		Get
			If _Instance Is Nothing Then _Instance = New Settings
			Return _Instance
		End Get
	End Property

	Public Property SavePath As String
		Get
			If Settings("SavePath") = "" Then Settings("SavePath") =
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
			Return Settings("SavePath")
		End Get
		Set(value As String)
			Settings("SavePath") = value
		End Set
	End Property
	Public Property SetAsStartup As Boolean
		Get
			If Not Settings.ContainsKey("SetAsStartup") Then
				Settings("SetAsStartup") = True
				RunAtStartup()
			End If
			Return Settings("SetAsStartup")
		End Get
		Set(value As Boolean)
			If value Then RunAtStartup() Else RemoveFromStartup()
			Settings("SetAsStartup") = value
		End Set
	End Property
	Public Property ResumeDownload As Boolean
		Get
			If Not Settings.ContainsKey("ResumeDownload") Then
				Settings("ResumeDownload") = True
			End If
			Return Settings("ResumeDownload")
		End Get
		Set(value As Boolean)
			Settings("ResumeDownload") = value
		End Set
	End Property
	Public Property OnErrorRetry As Boolean
		Get
			If Not Settings.ContainsKey("OnErrorRetry") Then
				Settings("OnErrorRetry") = True
			End If
			Return Settings("OnErrorRetry")
		End Get
		Set(value As Boolean)
			DownloadableObject.RetryOnError = value
			Settings("OnErrorRetry") = value
		End Set
	End Property
	Public Property StartOnAdded As Boolean
		Get
			If Not Settings.ContainsKey("StartOnAdded") Then
				Settings("StartOnAdded") = True
			End If
			Return Settings("StartOnAdded")
		End Get
		Set(value As Boolean)
			Settings("StartOnAdded") = value
		End Set
	End Property
	Public Property RefreshRate As Integer
		Get
			If Settings("RefreshRate") < 400 Or Settings("RefreshRate") > 5000 Then Settings("RefreshRate") = 400
			Return Settings("RefreshRate")
		End Get
		Set(value As Integer)
			Settings("RefreshRate") = value
		End Set
	End Property
	Public Property StartOnStart As Boolean
		Get
			If Not Settings.ContainsKey("StartOnStart") Then
				Settings("StartOnStart") = True
			End If
			Return Settings("StartOnStart")
		End Get
		Set(value As Boolean)
			Settings("StartOnStart") = value
		End Set
	End Property

	Public Shared Sub Save()
		Using out = IO.File.OpenWrite(File)
			BF.Serialize(out, Instance.Settings)
		End Using
	End Sub
	Public Shared Sub Load()
		If Not IO.File.Exists(File) Then Exit Sub
		Using inp = IO.File.OpenRead(File)
			Instance.Settings = BF.Deserialize(inp)
		End Using
	End Sub

End Class

''' <summary>
''' Represents a downloadable entity
''' </summary>
''' <remarks></remarks>
<Serializable>
Public Class DownloadableObject

#Region "Fields"

	Private ReadOnly BufferSize As Integer = 8192

	<NonSerialized>
	Private _Request As WebRequest
	<NonSerialized>
	Private _Source As Stream
	<NonSerialized>
	Private _Destination As Stream
	<NonSerialized>
	Private _Response As WebResponse

	Private _URL As String
	Private _File As String

	Private _Offset As Long
	Private _Length As Long = -1

	Private _Error As Exception
	Friend _Status As DownloadStatus = DownloadStatus.Idle

#End Region

#Region "Constructors"

	''Represents a Default constructor for Serialization/Deserialization
	Protected Sub New()

	End Sub

	''' <summary>
	''' Creates an instance of Downloadable Object from given output file and url
	''' </summary>
	''' <param name="File">Target File</param>
	''' <param name="URL">Input File URL</param>
	''' <remarks></remarks>
	Public Sub New(File As String, URL As String)
		_File = File
		_URL = URL
	End Sub

#End Region

	Public Shared Property RetryOnError As Boolean = True

	''' <summary>
	''' Gets the length of file to be downloaded
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Length As Long
		Get
			Return _Length
		End Get
	End Property

	''' <summary>
	''' Gets or Sets the offset from where file will be started to download
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Offset As Long
		Get
			Return _Offset
		End Get
		Set(value As Long)
			_Offset = value
		End Set
	End Property

	''' <summary>
	''' Gets or Sets weather Request is to be stopped or not
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Stopped As Boolean

	Public ReadOnly Property Status As DownloadStatus
		Get
			Return _Status
		End Get
	End Property

	Public ReadOnly Property File As String
		Get
			Return _File
		End Get
	End Property

	Public ReadOnly Property [Error] As Exception
		Get
			Return _Error
		End Get
	End Property

	''' <summary>
	''' Gets the progress of current downloading
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Progress As Double
		Get
			If Length <> -1 Then
				Return 100 * Offset / Length
			Else
				Return 0.0F
			End If
		End Get
	End Property

	''' <summary>
	''' Gets Length From Server
	''' </summary>
	''' <remarks></remarks>
	Public Sub GetLength()
		Dim Req As WebRequest = WebRequest.Create(_URL)
		Req.Method = "HEAD"
		Using Resp = Req.GetResponse
			_Length = Resp.ContentLength
		End Using
		_Status = DownloadStatus.Idle
	End Sub

	''' <summary>
	''' Initializes download
	''' </summary>
	''' <remarks></remarks>
	Public Sub Initialize()
		Try
			_Request = WebRequest.Create(_URL)
			_Destination = IO.File.OpenWrite(_File)

			Try
				If Length = -1 Then
					CType(_Request, HttpWebRequest).AddRange(Offset)
				Else
					CType(_Request, HttpWebRequest).AddRange(Offset, Length)
				End If
				_Destination.Position = Offset
			Catch ex As Exception
			End Try

			_Response = _Request.GetResponse
			_Source = _Response.GetResponseStream
			_Status = DownloadStatus.Idle

		Catch ex As Exception
			_Status = DownloadStatus.Error
			_Error = ex
		End Try

	End Sub

	''' <summary>
	''' Finalizes Download
	''' </summary>
	''' <remarks></remarks>
	Public Shadows Sub Finalize()
		If IsCompleted Then
			_Status = DownloadStatus.Completed
		Else
			If Status <> DownloadStatus.Error Then
				_Status = DownloadStatus.Idle
			End If
		End If

		_Request = Nothing
		If _Source IsNot Nothing Then _
			_Source.Dispose()
		If _Response IsNot Nothing Then _
			_Response.Dispose()
		If _Destination IsNot Nothing Then _
			_Destination.Dispose()

	End Sub

	''' <summary>
	''' Transfers a packet and returns number of bytes of this packet
	''' </summary>
	''' <remarks></remarks>
	Public Function TransferPacket() As Long
		TransferPacket = 0 
		Dim Data(BufferSize - 1) As Byte
		TransferPacket = _Source.Read(Data, 0, Data.Length)
		_Destination.Write(Data, 0, TransferPacket)
	End Function

	''' <summary>
	''' Does Each and every step to complete download
	''' </summary>
	''' <remarks></remarks>
	Public Sub Download()
		Stopped = False

		Do
			If Stopped Then Exit Do
			Try
				_Status = DownloadStatus.Starting
				GetLength()
				Initialize()
				_Status = DownloadStatus.Working
				Do
					If IsCompleted Then
						Exit Do
					End If
					Dim Packet = TransferPacket()
					_Offset += Packet
					If Stopped Or Packet = 0 Then
						Exit Do
					End If
				Loop
				Finalize()
				Exit Do
			Catch ex As Exception
				_Status = DownloadStatus.Error
				_Error = ex
				Finalize()
				If RetryOnError Then
					Continue Do
				Else
					Exit Do
				End If
			End Try
		Loop

		Stopped = True
	End Sub

	''' <summary>
	''' Starts downloading asyncly
	''' </summary>
	''' <remarks></remarks>
	Public Sub Start()
		If Status = DownloadStatus.Error Or Status = DownloadStatus.Idle Then
			Task.Run(AddressOf Download)
		End If
	End Sub

	''' <summary>
	''' Sends Stop Request
	''' </summary>
	''' <remarks></remarks>
	Public Sub [Stop]()
		Stopped = True
	End Sub

	Public ReadOnly Property IsCompleted As Boolean
		Get
			Return _Offset = _Length
		End Get
	End Property

	Public ReadOnly Property IsWorking As Boolean
		Get
			Return Status = DownloadStatus.Working
		End Get
	End Property

	Public ReadOnly Property HasError As Boolean
		Get
			Return Status = DownloadStatus.Error
		End Get
	End Property

	'IsCompleted
	'IsWorking
	'HasError
	'HasStopped

End Class
Public Enum DownloadStatus
	Idle
	Starting
	Working
	[Error]
	Completed
End Enum
<Serializable>
Public Class DownloadHandler
	Inherits DownloadableObject
	Implements INotifyPropertyChanged

	<NonSerialized>
	Private Sw As Stopwatch

	Private OFs As Long = 0

	Public Sub New(File As String, URL As String)
		MyBase.New(File, URL)
	End Sub

	Public ReadOnly Property StatusColor As Brush
		Get
			Select Case Status
				Case DownloadStatus.Completed
					Return Brushes.Gray
				Case DownloadStatus.Error
					Return Brushes.Red
				Case DownloadStatus.Idle
					Return Brushes.Black
				Case DownloadStatus.Starting
					Return Brushes.Yellow
				Case DownloadStatus.Working
					Return Brushes.LightGreen
			End Select
			Return Brushes.White
		End Get
	End Property
	Public ReadOnly Property FileName As String
		Get
			Return IO.Path.GetFileName(File)
		End Get
	End Property
	Public ReadOnly Property SizeText As String
		Get
			If Length <> -1 Then
				If Length < 1024 Then
					Return (Length).ToString + " B"
				ElseIf Length < 1024 * 1024 Then
					Return CInt(Length / 1024).ToString + " kB"
				ElseIf Length < 1024 * 1024 * 1024 Then
					Return CInt(Length / (1024 * 1024)).ToString + " MB"
				Else
					Return CInt(Length / (1024 * 1024 * 1024)).ToString + " GB"
				End If
			Else
				Return "... B"
			End If
		End Get
	End Property
	Public ReadOnly Property SpeedText As String
		Get
			If Sw Is Nothing Then
				Sw = New Stopwatch
			End If
			Sw.Stop()
			If Sw.ElapsedMilliseconds <> 0 Then
				SpeedText = CInt((Offset - OFs) / Sw.ElapsedMilliseconds).ToString + " kB/s"
			Else
				SpeedText = "0 kB/s"
			End If
			OFs = Offset
			Sw.Restart()
		End Get
	End Property
	Public ReadOnly Property StatusText As String
		Get
			Select Case Status
				Case DownloadStatus.Completed
					Return "Completed"
				Case DownloadStatus.Error
					Return "Error:" + Me.Error.Message
				Case DownloadStatus.Idle
					Return "Idle"
				Case DownloadStatus.Starting
					Return "Starting..."
				Case DownloadStatus.Working
					Return "Working"
			End Select
			Return "Unknown"
		End Get
	End Property
	Public Property ProgressText As Double
		Get
			Return Progress
		End Get
		Set(value As Double)

		End Set
	End Property
	Public ReadOnly Property ActionEnabled As Boolean
		Get
			If Status = DownloadStatus.Starting Or (Stopped And IsWorking) Then
				Return False
			Else
				Return True
			End If
		End Get
	End Property
	Public ReadOnly Property ActionText As String
		Get
			If Status = DownloadStatus.Idle Or Status = DownloadStatus.Error Then
				Return "Start"
			ElseIf Status = DownloadStatus.Completed Then
				Return "Open"
			Else
				Return "Stop"
			End If
		End Get
	End Property
	Public ReadOnly Property Instance As Object
		Get
			Return Me
		End Get
	End Property

	<NonSerialized>
	Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

	Friend Sub Update()
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("StatusColor"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("SizeText"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("SpeedText"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("StatusText"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ProgressText"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ActionEnabled"))
		RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("ActionText"))
	End Sub

End Class

'Public Class Utils
'	Public Shared Function GetHash(filename As String, cryptoAlgo As CryptoAlgoEnum) As String
'		Dim algo As HashAlgorithm = Nothing
'		Dim sb As New StringBuilder()

'		Select Case cryptoAlgo
'			Case CryptoAlgoEnum.MD5
'				algo = New MD5CryptoServiceProvider()
'				Exit Select
'			Case CryptoAlgoEnum.SHA256
'				algo = New SHA256CryptoServiceProvider()
'				Exit Select
'			Case CryptoAlgoEnum.SHA512
'				algo = New SHA512CryptoServiceProvider()
'				Exit Select
'		End Select


'		Using fs As New FileStream(filename, FileMode.Open, FileAccess.Read)
'			For Each b As [Byte] In algo.ComputeHash(fs)
'				sb.Append(b.ToString("x2").ToLower())
'			Next
'		End Using

'		Return sb.ToString()
'	End Function

'	Public Shared Sub SerializeToBinary(o As Object, fs As Stream)
'		Dim binfmt As New BinaryFormatter()
'		binfmt.Serialize(fs, o)
'	End Sub

'	Public Shared Function DeserializeFromBinary(fs As Stream) As Object
'		Dim binfmt As New BinaryFormatter()
'		Return binfmt.Deserialize(fs)
'	End Function

'	Public Shared Sub WriteLog(message As String)
'		WriteLog(message, True)
'	End Sub

'	Public Shared Sub WriteLog(message As String, maintenance As Boolean)
'		Try
'			Dim f As New System.Diagnostics.StackFrame(2)
'			Dim methodname As String = f.GetMethod().Name
'			Dim fname As String = AppDomain.CurrentDomain.BaseDirectory + "scavenger.log"
'			Using sw As New StreamWriter(fname, True)
'				'message=String.Format ("{0:dd-MMM-yyyy HH:mm:ss}",DateTime.Now) + message;
'				sw.WriteLine(Convert.ToString((Convert.ToString(String.Format("{0:dd-MMM-yyyy HH:mm:ss}", DateTime.Now) + ": ") & methodname) + ": ") & message)
'			End Using

'			If maintenance Then
'				Dim fi As New FileInfo(fname)
'				If fi.Length > 1000000000 Then
'					'TODO: Clear initial n lines instead of deleting the entire file
'					File.Delete(fname)
'				End If
'				fi = Nothing
'			End If
'		Catch
'		End Try
'	End Sub
'End Class

'Public Enum CryptoAlgoEnum
'	None
'	MD5
'	SHA256
'	SHA512
'End Enum

'Public Enum DownloadStatusEnum
'	Preparing
'	Prepared
'	Running
'	Pausing
'	Paused
'	Stopping
'	Stopped
'	Completing
'	Completed
'	[Error]
'End Enum

'Public Delegate Sub ProgressEventHandler(sender As Object, e As ProgressEventArgs)

'Public Class ProgressEventArgs
'	Inherits System.EventArgs
'	Public BytesPending As Integer = 0
'	Public BytesTotal As Integer = 0
'	Public Status As DownloadStatusEnum
'	Public Key As String

'	Public Sub New(pending As Integer, total As Integer, status As DownloadStatusEnum, key As String)
'		BytesPending = pending
'		BytesTotal = total
'	End Sub
'End Class

'<Serializable> _
'Public Class Download
'	'public event System.EventHandler OnProgress;
'	Public CryptoAlgo As CryptoAlgoEnum = CryptoAlgoEnum.None
'	Public CryptoKey As String = ""
'	Public Status As DownloadStatusEnum = DownloadStatusEnum.Preparing
'	Public Size As Integer = 0
'	Public SizeInKB As Integer = 0
'	Public BytesRead As Integer = 0
'	Public FullFileName As String = Nothing
'	Public FileName As String = Nothing
'	Public Url As Uri
'	Public UseProxy As Boolean = False
'	Public ProxyServer As String = ""
'	Public ProxyPort As Integer = 0
'	Public ProxyUsername As String = ""
'	Public ProxyPassword As String = ""

'	'public DateTime Started= Convert.ToDateTime("16-august-1981");

'	''' <summary>
'	''' Download speed in kilobytes/sec
'	''' </summary>
'	Public Speed As Double = 0
'	'in KBPS

'	'private Uri location=null;
'	'private int segments=1;
'	<NonSerialized> _
'	Private ns As Stream = Nothing
'	<NonSerialized> _
'	Private fs As Stream = Nothing
'	Private acceptRanges As Boolean = False
'	<NonSerialized> _
'	Private thStart As Thread
'	<NonSerialized> _
'	Private thPrepare As Thread
'	Private ScheduledTime As DateTime = DateTime.MinValue
'	'time at which download is scheduled to start
'	<NonSerialized> _
'	Private sw As New Stopwatch()

'	Public Sub OnDeserialize()
'		thStart = New Thread(AddressOf StartThread)
'		sw = New Stopwatch()
'		'fs = new FileStream(FullFileName, FileMode.Append, FileAccess.Write);
'	End Sub

'	Public Function IsRunning() As Boolean
'		Return (thStart.ThreadState = System.Threading.ThreadState.Running)
'	End Function

'	Private Sub CreateStreams(resuming As Boolean)
'		'create file stream
'		Utils.WriteLog("Creating local file stream.")

'		If Not resuming Then
'			fs = New FileStream(Me.FullFileName, FileMode.Create, FileAccess.Write)
'			fs.SetLength(Size)
'		Else
'			'if (fs != null) fs.Close();
'			fs = New FileStream(Me.FullFileName, FileMode.Append, FileAccess.Write)
'			fs.Position = BytesRead
'		End If

'		Utils.WriteLog("Created local file stream.")

'		Utils.WriteLog("Creating network stream.")
'		Dim request As HttpWebRequest = DirectCast(WebRequest.Create(Url), HttpWebRequest)
'		If UseProxy Then
'			request.Proxy = New WebProxy((ProxyServer & Convert.ToString(":")) + ProxyPort.ToString())
'			If ProxyUsername.Length > 0 Then
'				request.Proxy.Credentials = New NetworkCredential(ProxyUsername, ProxyPassword)
'			End If
'		End If
'		'HttpWebRequest hrequest = (HttpWebRequest)request;
'		'hrequest.AddRange(BytesRead); ::TODO: Work on this
'		If BytesRead > 0 Then
'			request.AddRange(BytesRead)
'		End If

'		Dim response As WebResponse = request.GetResponse()
'		'result.MimeType = res.ContentType;
'		'result.LastModified = response.LastModified;
'		If Not resuming Then
'			'(Size == 0)
'			'resuming = false;
'			Size = CInt(response.ContentLength)
'			SizeInKB = CInt(Size) / 1024
'		End If
'		acceptRanges = [String].Compare(response.Headers("Accept-Ranges"), "bytes", True) = 0

'		'create network stream
'		ns = response.GetResponseStream()
'		Utils.WriteLog("Created network stream.")
'	End Sub



'	Public Sub New(url As Uri, localfilename As String)
'		Me.Url = url
'		Me.FullFileName = localfilename
'		Status = DownloadStatusEnum.Preparing

'		thPrepare = New Thread(AddressOf PrepareThread)
'		thPrepare.Start()
'	End Sub

'	Public Sub Schedule(time As DateTime)
'		ScheduledTime = time
'		Start()
'	End Sub

'	Public Sub Start()
'		Utils.WriteLog("Waiting for prepare thread to end.")
'		If thPrepare IsNot Nothing AndAlso thPrepare.IsAlive Then
'			thPrepare.Join()
'		End If
'		Utils.WriteLog("Starting the main thread.")
'		If thStart IsNot Nothing AndAlso thStart.IsAlive Then
'			thStart.Abort()
'		End If
'		thStart = New Thread(AddressOf StartThread)
'		thStart.Start()
'		Utils.WriteLog("Started the main thread.")
'	End Sub

'	Private Sub PrepareThread()
'		Try
'			Status = DownloadStatusEnum.Preparing
'			Utils.WriteLog(Convert.ToString("Preparing download for url " + Url.OriginalString + ". localpath=") & FullFileName)

'			'this.location = url;
'			'this.segments=segments;

'			'First, create the local file:
'			Dim fi As New FileInfo(FullFileName)
'			If Not Directory.Exists(fi.DirectoryName) Then
'				Directory.CreateDirectory(fi.DirectoryName)
'			End If

'			Dim fext As String = Path.GetExtension(FullFileName)
'			If File.Exists(FullFileName) Then
'				Dim c As Integer = 0
'				Dim fname_woe As String = Path.GetFileNameWithoutExtension(FullFileName)
'				Dim ffname As String = ""
'				Do
'					ffname = Convert.ToString(fi.Directory.FullName + Path.DirectorySeparatorChar + fname_woe & String.Format("({0})", System.Math.Max(System.Threading.Interlocked.Increment(c), c - 1))) & fext
'				Loop While File.Exists(ffname)

'				FullFileName = ffname
'			End If

'			Me.FileName = Path.GetFileNameWithoutExtension(FullFileName) & fext

'			CreateStreams(False)

'			Status = DownloadStatusEnum.Prepared
'			Utils.WriteLog("Prepared download.")
'		Catch e As Exception
'			Status = DownloadStatusEnum.[Error]
'			Utils.WriteLog("Error occured: " + e.Message)
'		End Try
'	End Sub

'	Private Sub StartThread()
'		Try
'			Status = DownloadStatusEnum.Paused
'			While ScheduledTime > DateTime.Now
'			End While


'			Me.Status = DownloadStatusEnum.Running
'			'TODO: Check for thread-safety and lock/sync variables accordingly.
'			Dim buffer As Byte() = New Byte(4095) {}
'			Dim bytesToRead As Integer = Size
'			'if (Started.Year==1981) Started = DateTime.Now;
'			sw.Start()
'			While True
'				'(bytesToRead>0)
'				Dim n As Integer = ns.Read(buffer, 0, buffer.Length)
'				'sw.Stop();
'				'Speed = (n == 0) ? 0 : ((float)n / 1000) / sw.Elapsed.TotalSeconds;
'				Speed = If((n = 0), 0, (CSng(BytesRead) / 1000) / sw.Elapsed.TotalSeconds)

'				If n = 0 Then
'					Status = DownloadStatusEnum.Completing
'					Exit While
'				End If
'				fs.Write(buffer, 0, n)
'				fs.Flush()
'				BytesRead += n
'				bytesToRead -= n
'				'OnProgress(this, new System.EventArgs());
'				If Status = DownloadStatusEnum.Pausing OrElse Status = DownloadStatusEnum.Stopping Then
'					Exit While
'				End If
'#If DEBUG Then
'#End If
'				Thread.Sleep(50)
'			End While
'			sw.[Stop]()

'			ns.Close()
'			ns = Nothing
'			fs.Close()

'			If Status = DownloadStatusEnum.Pausing Then
'				Status = DownloadStatusEnum.Paused
'			ElseIf Status = DownloadStatusEnum.Completing Then
'				'verify before completing
'				If CryptoAlgo <> CryptoAlgoEnum.None Then
'					'TODO: Indicate in some manner that verfication has failed.
'					If Utils.GetHash(FullFileName, CryptoAlgo) <> Me.CryptoKey Then
'					End If
'				End If
'				Status = DownloadStatusEnum.Completed
'			ElseIf Status = DownloadStatusEnum.Stopping Then
'				File.Delete(FullFileName)
'				Status = DownloadStatusEnum.Stopped
'				'OnProgress(this, new System.EventArgs());
'			End If
'		Catch generatedExceptionName As Exception
'			'OnProgress(this, new EventArgs());
'			Status = DownloadStatusEnum.[Error]
'		End Try
'	End Sub

'	Public Sub [Resume]()
'		'TODO: Test this code thoroughly.

'		'ns.Position = BytesRead; No need as this will be handled by request.AddRange() in CreateNetworkStream()
'		CreateStreams(True)
'		Start()
'	End Sub

'	Public Sub Pause()
'		Status = DownloadStatusEnum.Pausing
'		If thStart.IsAlive Then
'			thStart.Join()
'		End If
'	End Sub

'	Public Sub [Stop]()
'		If thStart Is Nothing OrElse thStart.ThreadState = System.Threading.ThreadState.Unstarted OrElse thStart.ThreadState = System.Threading.ThreadState.Stopped Then
'			Status = DownloadStatusEnum.Stopped
'			Return
'		End If
'		Status = DownloadStatusEnum.Stopping

'		thStart.Abort()
'		thStart.Join(5 * 1000)

'		Status = DownloadStatusEnum.Stopped
'	End Sub
'End Class