using SharpCompress.Common;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using static EasyUpdateFromGithub.ToolClass;

namespace EasyUpdateFromGithub
{
	/// <summary>
	/// 自动从Github获取更新
	/// </summary>
	public class UpdateFromGithub
	{
		string? repositoryUrl;
		/// <summary>
		/// Github仓库地址，设置后会自动匹配RepositoryURLApi的值
		/// </summary>
		public required string RepositoryURL
		{
			set {
				if (value[^1..] == "/" || value[^1..] == "\\") 
					repositoryUrl = value[..^1];
				else
					repositoryUrl = value;
				repositoryUrlApi = $"https://api.github.com/repos{repositoryUrl[(repositoryUrl.IndexOf("github.com") + 10)..]}";
				repositoryUrlApi_rel = $"{repositoryUrlApi}/releases";
				repositoryUrlApi_relLatest = $"{repositoryUrlApi_rel}/latest";
			}
			get => repositoryUrl!;
		}
		string? repositoryUrlApi;
		/// <summary>
		/// 只读属性
		/// Github仓库地址的Api
		/// </summary>
		public string RepositoryURLApi=> repositoryUrlApi!;

		/// <summary>
		/// release
		/// </summary>
		string? repositoryUrlApi_rel;
		/// <summary>
		/// release中的latest
		/// </summary>
		string? repositoryUrlApi_relLatest;

		string? programVersion;
		/// <summary>
		/// 当前程序的版本号
		/// </summary>
		public required string ProgramVersion
		{
			set
			{
				programVersion = value;
				programVersionNumber = long.Parse(Regex.Replace(programVersion, @"[^0-9]", ""));
			}
			get => programVersion!;
		}
		long programVersionNumber = -1;
		/// <summary>
		/// 当前程序的版本号，整数类型
		/// </summary>
		public long ProgramVersionNumber => programVersionNumber;

		string cacheDir = System.IO.Path.GetTempPath() + @"EasyUpdateFromGithub";
		/// <summary>
		/// 进行文件处理的临时文件夹<br/>
		/// 如果没有设置该项，则默认为"C:\Users\[user]\AppData\Local\Temp\EasyUpdateFromGithub\"
		/// </summary>
		public string CacheDir
		{
			set => cacheDir = value;
			get => cacheDir;
		}
		/// <summary>
		/// 更简单的设置文件处理的临时文件夹<br/>
		/// 只需将该值设置为主程序名或其它不带特殊字符的名字即可<br/>
		/// 它将会被自动匹配入临时文件夹路径内
		/// </summary>
		public string EasySetCacheDir
		{
			set=>cacheDir= $@"{System.IO.Path.GetTempPath()}{value}\EasyUpdateFromGithub";
		}

		/// <summary>
		/// 检查更新函数返回的信息
		/// </summary>
		public class CheckUpdateValue
		{
			internal bool haveUpdate;
			/// <summary>
			/// 是否包含更新
			/// </summary>
			public bool HaveUpdate => haveUpdate;

			internal string latestVersionStr="";
			/// <summary>
			/// 新版本的版本字符串
			/// </summary>
			public string LatestVersionStr=>latestVersionStr;

			internal long latestVersionNumber = -1;
			/// <summary>
			/// 新版本的版本号
			/// </summary>
			public long LatestVersionNumber => latestVersionNumber;

			internal DateTime publishedTime;
			/// <summary>
			/// 发布时间，UTC标准时间
			/// </summary>
			public DateTime PublishedTime_UTC => publishedTime;
			/// <summary>
			/// 发布时间，该属性将会自动将UTC时间转换为本地时间
			/// </summary>
			public DateTime PublishedTime_Local => 
				TimeZoneInfo.ConvertTimeFromUtc(PublishedTime_UTC, TimeZoneInfo.Local);

			internal int downloadCount = -1;
			/// <summary>
			/// 该发布页所有文件的下载次数
			/// </summary>
			public int DownloadCount => downloadCount;

			internal string releaseName = "";
			/// <summary>
			/// 发布页中的标题名
			/// </summary>
			public string ReleaseName => releaseName;

			internal string releaseBody = "";
			/// <summary>
			/// 发布页中的说明内容
			/// </summary>
			public string ReleaseBody=>releaseBody;
		}
		/// <summary>
		/// 根据RepositoryURL检查当前程序是否有可用的更新和获取其它信息
		/// </summary>
		public CheckUpdateValue CheckUpdate()
		{
			Task<string> task = new(() => GetGithubApiResponseAsync(repositoryUrlApi_relLatest!).Result);
			task.Start();
			task.Wait();
			return CheckUpdateX(task.Result);
		}
		/// <summary>
		/// 根据RepositoryURL检查当前程序是否有可用的更新和获取其它信息
		/// </summary>
		public async Task<CheckUpdateValue> CheckUpdateAsync()
		{
			string ret = await GetGithubApiResponseAsync(repositoryUrlApi_relLatest!);
			return CheckUpdateX(ret);
		}
		CheckUpdateValue CheckUpdateX(string ret)
		{
			CheckUpdateValue cuv = new();
			{
				cuv.latestVersionStr = JsonNode.Parse(ret)!["tag_name"]!.GetValue<string>();
				cuv.latestVersionNumber = long.Parse(Regex.Replace(cuv.latestVersionStr, @"[^0-9]", ""));
				long pvn = programVersionNumber;
				//将两个版本号的长度进行比较，将两个版本号的长度统一后再进行大小比较
				if (cuv.LatestVersionNumber.ToString().Length > pvn.ToString().Length)
				{
					cuv.latestVersionNumber = long.Parse(cuv.LatestVersionNumber.ToString()[..pvn.ToString().Length]);
				}
				else if (cuv.LatestVersionNumber.ToString().Length < pvn.ToString().Length)
				{
					pvn = long.Parse(pvn.ToString()[..cuv.LatestVersionNumber.ToString().Length]);
				}
				cuv.haveUpdate = (cuv.LatestVersionNumber > pvn);
				cuv.releaseName= JsonNode.Parse(ret)!["name"]!.GetValue<string>();
				cuv.releaseBody= JsonNode.Parse(ret)!["body"]!.GetValue<string>();
			}
			cuv.publishedTime =
				DateTime.SpecifyKind(
					DateTime.Parse( 
							JsonNode.Parse(ret)!["published_at"]!.GetValue<string>().Replace('T', ' ').Replace("Z","")
							)
				,DateTimeKind.Utc);//将时间Kind属性设置为UTC
			{
				int num = 0;
				for (int i = 0; true; i++)
				{
					try
					{
						JsonNode jn = JsonNode.Parse(ret)!["assets"]![i]!["download_count"]!;
						if (jn != null)
							num += jn.GetValue<int>();
						else break;
					}
					catch { break; }
				} 
				cuv.downloadCount = num;
			}
			return cuv;
		}
		/// <summary>
		/// 将被下载的目标文件的信息
		/// </summary>
		public class InfoOfDownloadFile {
			/// <summary>
			/// 文件名
			/// </summary>
			public required string Name {  get; set; }
			/// <summary>
			/// 文件大小
			/// </summary>
			public required ulong? Size { get;set; }
			/// <summary>
			/// 下载地址
			/// </summary>
			public required string DownloadUrl { get; set; }
			/// <summary>
			/// 该文件的发布者名称
			/// </summary>
			public required string UploaderName { get;set; }
		}
		/// <summary>
		/// 获取需要下载的文件的信息
		/// </summary>
		/// <param name="fileRegex">
		/// 用于选择文件的正则表达式<br/>
		/// 名称符合所给正则表达式的文件将被选中，如果包含多个符合正则表达式的文件，则使用fileIndex参数选择指定文件<br/>
		/// 如果为null，则使用文件编号来选择Release中的文件
		/// </param>
		/// <param name="fileIndex">
		/// 文件编号<br/>
		/// 如果fileRegex不为null，则代表符合条件的文件选择序号。否则则代表Release中所有文件的选择序号
		/// </param>
		/// <param name="tag">版本名，默认为latest</param>
		/// <returns></returns>
		public async Task<InfoOfDownloadFile> GetDownloadFileInfoAsync(Regex? fileRegex = null, int fileIndex = 0, string tag = "latest") {
			string url = $"{repositoryUrlApi_rel}/{tag}";
			JsonNode json;
			if (fileRegex != null) {
				JsonNode tmpj = JsonNode.Parse(await GetGithubApiResponseAsync(url!))!["assets"]!;
				List<JsonNode> trueRegex = [];
				for (int i = 0; true; i++) {
					JsonNode tmpj2 = tmpj[i]!;
					if (tmpj2 != null) {
						if (fileRegex.IsMatch(tmpj2["name"]!.GetValue<string>())) {
							trueRegex.Add(tmpj2);
							break;
						}
					}
					else break;
				}
				if (trueRegex.Count > 1) {
					if (fileIndex < trueRegex.Count)
						json = trueRegex[fileIndex];
					else
						json = trueRegex[^1];
				}
				else
					json = trueRegex[0];
			}
			else {
				json = JsonNode.Parse(await GetGithubApiResponseAsync(url!))!["assets"]![fileIndex]!;
			}

			return new() {
				Name= json["name"]!.GetValue<string>(),
				Size= json["size"]!.GetValue<ulong>(),
				DownloadUrl= json["browser_download_url"]!.GetValue<string>(),
				UploaderName= json["uploader"]!["login"]!.GetValue<string>(),
			};
		}
		/// <summary>
		/// github中源代码包的格式
		/// </summary>
		public enum GithubSourceCodeFile {
			/// <summary>
			/// zip格式的源码文件
			/// </summary>
			zip,
			/// <summary>
			/// tar.gz格式的源码文件<br/>
			/// </summary>
			[Obsolete("目前tar.gz格式不支持自动解压，请使用zip格式",true)]
			targz,
		}
		/// <summary>
		/// 获取需要下载的文件的信息
		/// </summary>
		/// <param name="gscf">源代码文件类型</param>
		/// <param name="tag">版本名，默认为latest</param>
		/// <returns></returns>
		public async Task<InfoOfDownloadFile> GetDownloadFileInfoAsync(GithubSourceCodeFile gscf, string tag = "latest") {
			string url = $"{repositoryUrlApi_rel}/{tag}";
			JsonNode json = JsonNode.Parse(await GetGithubApiResponseAsync(url!))!;
			string sourceFileType="";
			switch (gscf) { 
				case GithubSourceCodeFile.zip:
					sourceFileType = ".zip";
					break;
#if false
				case GithubSourceCodeFile.targz:
					sourceFileType = ".tar.gz";
					break;
#endif
			}
			return new() {
				Name = $"Source code{sourceFileType}",
				Size = null,
				DownloadUrl = $"{RepositoryURL}/archive/refs/tags/{json["tag_name"]!.GetValue<string>()}{sourceFileType}",
				UploaderName = json["author"]!["login"]!.GetValue<string>(),
			};
		}
		/// <summary>
		/// 执行安装的信息
		/// </summary>
		public class InfoOfInstall
		{
			internal string newFileDir = "";
			internal string oldFileDir = "";
			internal string installerFile = "";
			internal string exeFile = "";

			/// <summary>
			/// 新版本的文件目录
			/// </summary>
			public string NewFileDir => newFileDir;
			/// <summary>
			/// 旧版本的文件目录
			/// </summary>
			public string OldFileDir => oldFileDir; 
			/// <summary>
			/// 安装程序的文件位置
			/// </summary>
			public string InstallerFile => installerFile; 
			/// <summary>
			/// 安装完后执行的可执行文件位置
			/// </summary>
			public string ExeFile => exeFile; 
		}
		/// <summary>
		/// 下载发布文件
		/// </summary>
		/// <param name="iodf">目标文件信息</param>
		/// <param name="readyToInstall">下载完成后，是否进行安装准备。如果为false，则返回null</param>
		/// <param name="unPack">是否解压下载的文件，如果为true，会将下载的文件解压缩，不会判断其是否是压缩文件</param>
		/// <returns>当参数readyToInstall为true时，会返回处理过的安装信息。如果该参数为false，则返回null</returns>
		public async Task<InfoOfInstall?> DownloadReleaseAsync(InfoOfDownloadFile iodf, bool readyToInstall = true,bool unPack=true)
		{
			string dlFilePath = $@"{cacheDir}\{iodf.Name}";
			Directory.CreateDirectory(cacheDir);
			await DownloadFile(iodf.DownloadUrl, dlFilePath);

			if (readyToInstall)
			{
				InfoOfInstall ioi = new()
				{
					newFileDir = $@"{dlFilePath}_AllFile"
				};
				Directory.CreateDirectory(ioi.newFileDir);
				if (unPack)
					UnPack(dlFilePath, ioi.newFileDir);
				else
					File.Move(dlFilePath, $@"{ ioi.newFileDir}\{Path.GetFileName(dlFilePath)}",true);


				ioi.installerFile = $@"{cacheDir}\EasyUpdateFromGithub_RunInstall.exe";
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"EasyUpdateFromGithub.EasyUpdateFromGithub_RunInstall.exe")!)
				{
					using Stream fileStream = File.Create(ioi.installerFile);
					stream.CopyTo(fileStream);
				}
				ioi.exeFile = Environment.ProcessPath!;
				return ioi;
			}
			return null;
		}
		/// <summary>
		/// 安装已下载的文件<br/>
		/// 将已下载的文件(或是已解压的文件)移动(或覆盖)到指定文件夹
		/// </summary>
		/// <param name="ioi">安装信息</param>
		/// <param name="installDir">
		/// 选择安装目录，将会把下载的文件(或是解压的文件)移动到指定的目录<br/>
		/// 如果为空，则使用当前程序所在的目录 
		/// <param name="useAdmin">是否使用管理员权限运行</param>
		/// <param name="openOnOver">在执行完成后是否自动打开可执行文件</param>
		/// <param name="waitTime">安装进程等待程序退出的时间，单位: ms</param>
		/// <param name="exePath">可执行文件路径，如果openOnOver参数为true，则在安装结束后执行该路径的程序。<br/>
		/// 如果该参数为null，则自动获取当前可执行文件的路径。
		/// </param>
		/// </param>
		public void InstallFile(InfoOfInstall ioi,string? installDir=null,bool useAdmin=false,bool openOnOver=true,int waitTime=0,string? exePath=null)
		{
			if (installDir != null)
				ioi.oldFileDir = installDir;
			else
				ioi.oldFileDir = Environment.CurrentDirectory;
			if(exePath != null)
				ioi.exeFile = exePath;
			
			Process process = new()
			{
				StartInfo = new ProcessStartInfo
				{
					UseShellExecute = true,
					CreateNoWindow = true,
					FileName = ioi.installerFile,
					Arguments = $" {waitTime} \"{ioi.newFileDir}\" \"{ioi.oldFileDir}\""
				}
			};
			if (useAdmin)
				process.StartInfo.Verb = "RunAs";
			if (openOnOver)
				process.StartInfo.Arguments += $" \"{ioi.exeFile}\"";
			else
				process.StartInfo.Arguments += " NULL";
			process.Start();
		}
	}
}
