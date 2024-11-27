using SharpCompress.Common;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static EasyUpdateFromGithub.ToolClass;

namespace EasyUpdateFromGithub
{
	public class UpdateFromGithub
	{
		string? repositoryUrl;
		/// <summary>
		/// Github仓库地址，设置后会自动匹配RepositoryURLApi的值
		/// </summary>
		public string RepositoryURL
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
		/// Github仓库地址的Api
		/// </summary>
		public string RepositoryURLApi
		{
			get => repositoryUrlApi!;
		}
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
		public string ProgramVersion
		{
			set
			{
				programVersion = value;
				programVersionNumber = long.Parse(Regex.Replace(programVersion, @"[^0-9]", ""));
			}
			get => programVersion!;
		}
		long programVersionNumber = -1;
		public long ProgramVersionNumber
		{
			get => programVersionNumber;
		}

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
			/// <summary>
			/// 是否包含更新
			/// </summary>
			public bool HaveUpdate => haveUpdate;
			internal bool haveUpdate;
			/// <summary>
			/// 新版本的字符串
			/// </summary>
			public string NewVersionStr=>newVersionStr;
			internal string newVersionStr;
			/// <summary>
			/// 发布时间
			/// </summary>
			public string PublishedTime=>publishedTime;
			internal string publishedTime;
			/// <summary>
			/// 该发布页所有文件的下载次数
			/// </summary>
			public int DownloadCount => downloadCount;
			internal int downloadCount=-1;
		}
		/// <summary>
		/// 根据RepositoryURL检查当前程序是否有可用的更新和获取其它信息
		/// </summary>
		public CheckUpdateValue CheckUpdate()
		{
			Task<string> task = new(() => GetUrlResponseAsync(repositoryUrlApi_relLatest!).Result);
			task.Start();
			task.Wait();
			return CheckUpdateX(task.Result);
		}
		/// <summary>
		/// 根据RepositoryURL检查当前程序是否有可用的更新和获取其它信息
		/// </summary>
		public async Task<CheckUpdateValue> CheckUpdateAsync()
		{
			string ret = await GetUrlResponseAsync(repositoryUrlApi_relLatest!);
			return CheckUpdateX(ret);
		}
		CheckUpdateValue CheckUpdateX(string ret)
		{
			CheckUpdateValue cuv = new();
			{
				cuv.newVersionStr = JsonNode.Parse(ret)!["tag_name"]!.GetValue<string>();
				long latestVersionNumber = long.Parse(Regex.Replace(cuv.newVersionStr, @"[^0-9]", ""));
				long pvn = programVersionNumber;
				//将两个版本号的长度进行比较，将两个版本号的长度统一后再进行大小比较
				if (latestVersionNumber.ToString().Length > pvn.ToString().Length)
				{
					latestVersionNumber = long.Parse(latestVersionNumber.ToString()[..pvn.ToString().Length]);
				}
				else if (latestVersionNumber.ToString().Length < pvn.ToString().Length)
				{
					pvn = long.Parse(pvn.ToString()[..latestVersionNumber.ToString().Length]);
				}
				cuv.haveUpdate = (latestVersionNumber > pvn);
			}
			cuv.publishedTime = JsonNode.Parse(ret)!["published_at"]!.GetValue<string>().Replace('T', ' ').Replace("Z","");
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


		public class InfoOfInstall
		{
			internal string newFileDir = "";
			internal string oldFileDir = "";
			internal string installerFile = "";
			internal string exeFile = "";
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
			JsonNode json = JsonNode.Parse(await GetUrlResponseAsync(url!))!["assets"]![item]!;
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
		/// <paramref name="useAdmin">是否使用管理员权限运行</paramref>
		/// <paramref name="openOnOver">在执行完成后是否自动打开可执行文件</paramref>
		/// <paramref name="waitTime">安装进程等待程序退出的时间，单位: ms</paramref>
		/// <paramref name="exePath">可执行文件路径，如果openOnOver参数为true，则在安装结束后执行该路径的程序。<br/>
		/// 如果该参数为null，则自动获取当前可执行文件的路径。
		/// </paramref>
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
