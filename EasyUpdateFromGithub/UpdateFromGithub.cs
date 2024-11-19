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

		string cacheDir = System.IO.Path.GetTempPath() + @"EasyUpdateFromGithub\";
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
		/// 根据RepositoryURL检查当前程序是否有可用的更新
		/// </summary>
		/// <returns>有可用的更新将返回true，否则返回false</returns>
		public bool CheckUpdate()
		{
			Task<string> task = new(() => GetUrlResponseAsync(repositoryUrlApi_relLatest!).Result);
			task.Start();
			task.Wait();
			return CheckUpdateX(task.Result);
		}
		/// <summary>
		/// 根据RepositoryURL检查当前程序是否有可用的更新
		/// </summary>
		/// <returns>有可用的更新将返回true，否则返回false</returns>
		public async Task<bool> CheckUpdateAsync()
		{
			string ret = await GetUrlResponseAsync(repositoryUrlApi_relLatest!);
			return CheckUpdateX(ret);
		}
		bool CheckUpdateX(string ret)
		{
			long latestVersionNumber = long.Parse(Regex.Replace(JsonNode.Parse(ret)!["tag_name"]!.GetValue<string>(), @"[^0-9]", ""));
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
			if (latestVersionNumber > pvn)
				return true;
			else
				return false;
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
					newFileDir = $@"{dlFilePath}_AllFile\"
				};
				Directory.CreateDirectory(ioi.newFileDir);
				if (unPack)
					UnPack(dlFilePath, ioi.newFileDir);
				else
					File.Move(dlFilePath, $"{ ioi.newFileDir}{Path.GetFileName(dlFilePath)}");


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
		/// </param>
		public static void InstallFile(InfoOfInstall ioi,string? installDir=null,bool useAdmin=false,bool openOnOver=true)
		{
			if (installDir != null)
				ioi.oldFileDir = installDir;
			else
				ioi.oldFileDir = Environment.CurrentDirectory;
			
			Process process = new()
			{
				StartInfo = new ProcessStartInfo
				{
					UseShellExecute = true,
					CreateNoWindow = true,
					FileName = ioi.installerFile,
					Arguments = $" {ioi.newFileDir} {ioi.oldFileDir}"
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
