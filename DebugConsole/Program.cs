using EasyUpdateFromGithub;

namespace DebugConsole
{
	internal class Program
	{
		static void Main(string[] args)
		{
			UpdateFromGithub ufg = new()
			{
				RepositoryURL = "https://github.com/Hgnim/Test",
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
下载次数: {cuv.DownloadCount}");

			Console.WriteLine("开始下载更新...");
			Task<UpdateFromGithub.InfoOfInstall> task= ufg.DownloadReleaseAsync(0,unPack:true)!;
			task.Wait();			
			Console.WriteLine("更新下载完成，开始模拟安装");
			ufg.InstallFile(task.Result, @"D:\Test",waitTime:1000,openOnOver:false /*exePath: @"D:\Test\定时电源.exe"*/);

			Console.WriteLine("模拟安装完成");			
		}


	}
}
