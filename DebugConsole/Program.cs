using EasyUpdateFromGithub;
using System.Text.RegularExpressions;

namespace DebugConsole
{
	internal class Program
	{
		static void Main(string[] args)
		{
			UpdateFromGithub ufg = new()
			{
				RepositoryURL = "https://github.com/Hgnim/Test/",
				ProgramVersion = "1.0",				
			};
			Console.WriteLine($"缓存位置: {ufg.CacheDir}");
			Console.WriteLine($"目标存储库地址: {ufg.RepositoryURL}");
			Console.WriteLine($"目标存储库的API地址: {ufg.RepositoryURLApi}");
			UpdateFromGithub.CheckUpdateValue cuv = ufg.CheckUpdate();
			Console.WriteLine(
$@"是否有可用的更新: {cuv.HaveUpdate}
最新版本: {cuv.LatestVersionStr}({cuv.LatestVersionNumber}) 
发布时间(本地): {cuv.PublishedTime_Local}
发布时间(UTC时间): {cuv.PublishedTime_UTC}
下载次数: {cuv.DownloadCount}
发布页标题: {cuv.ReleaseName}
发布页详情: {cuv.ReleaseBody}");
			UpdateFromGithub.InfoOfDownloadFile iodf;
			{
				Console.WriteLine("获取新版本文件信息...");
				Task<UpdateFromGithub.InfoOfDownloadFile> iodfTask=ufg.GetDownloadFileInfoAsync(new Regex(@"^.+(1).+"));
				//ufg.GetDownloadFileInfoAsync(UpdateFromGithub.GithubSourceCodeFile.zip);//下载源码
				iodfTask.Wait();
				iodf= iodfTask.Result;
				Console.WriteLine(
$@"文件名: {iodf.Name}
文件大小: {iodf.Size}
发布者: {iodf.UploaderName}
下载链接: {iodf.DownloadUrl}");
			}
			Console.WriteLine("开始下载更新...");
			Task<UpdateFromGithub.InfoOfInstall> ioiTask= ufg.DownloadReleaseAsync(iodf, unPack:true)!;
			ioiTask.Wait();			
			Console.WriteLine("更新下载完成，开始模拟安装");
			ufg.InstallFile(ioiTask.Result, @"D:\Test",waitTime:1000,openOnOver:false /*exePath: @"D:\Test\定时电源.exe"*/);

			Console.WriteLine("模拟安装完成");			
		}


	}
}
