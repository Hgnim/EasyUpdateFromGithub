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
		/// <param name="item">文件编号</param>
		/// <param name="tag">版本名，默认为latest</param>
		/// <param name="readyToInstall">下载完成后，是否进行安装准备。如果为false，则返回null</param>
		/// <param name="unPack">是否解压下载的文件，如果为true，会将下载的文件解压缩，不会判断其是否是压缩文件</param>
		/// <returns>当参数readyToInstall为true时，会返回处理过的安装信息。如果该参数为false，则返回null</returns>
		public async Task<InfoOfInstall?> DownloadReleaseAsync(int item, string tag = "latest", bool readyToInstall = true,bool unPack=true)
		{
			string url = $"{repositoryUrlApi_rel}/{tag}";
			JsonNode json = JsonNode.Parse(await GetGithubApiResponseAsync(url!))!["assets"]![item]!;
			string dlFilePath = $@"{cacheDir}\{json["name"]}";
			Directory.CreateDirectory(cacheDir);
			await DownloadFile($"{json["browser_download_url"]}", dlFilePath);

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
					Arguments = $" {waitTime} {ioi.newFileDir} {ioi.oldFileDir}"
				}
			};
			if (useAdmin)
				process.StartInfo.Verb = "RunAs";
			if (openOnOver)
				process.StartInfo.Arguments += $" {ioi.exeFile}";
			else
				process.StartInfo.Arguments += " NULL";
			process.Start();
		}
	}
}
