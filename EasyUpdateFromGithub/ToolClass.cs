using SharpCompress.Archives;
using SharpCompress.Common;

namespace EasyUpdateFromGithub
{
	static class ToolClass
	{
		/// <summary>
		/// 获取github api请求的返回值
		/// </summary>
		internal static async Task<string> GetGithubApiResponseAsync(string url)
		{
			HttpResponseMessage response;
			using (HttpClient hc = new()) {
				HttpRequestMessage request;
				request =new(HttpMethod.Get, url);
				request.Headers.Add("User-Agent", "EasyUpdateFromGithub");

				 response = await hc.SendAsync(request);
			}
				//var request = await new HttpClient().GetAsync(url);
			return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 下载文件
		/// </summary>
		/// <param name="url"></param>
		/// <param name="filePath">写入的文件路径</param>
		internal static async Task<bool> DownloadFile(string url,string filePath)
		{
			try
			{
				Stream stream;
				using (HttpClient client = new())
				{
					stream = client.GetStreamAsync(url).Result;
				}
				using (Stream fileStream = File.Create(filePath))
				{
					await stream.CopyToAsync(fileStream);
				}
				stream.Close();
				return true;
			}
			catch { return  false; }
		}

		internal static void UnPack(string filePath,string dirPath)
		{
			using IArchive archive = ArchiveFactory.Open(filePath);
			foreach (var entry in archive.Entries)
			{
				if (!entry.IsDirectory)
				{
					//发现一个潜在的漏洞，应该是属于Nuget包SharpCompress的问题
					//漏洞：无法解压大小为0的文件
					entry.WriteToDirectory(dirPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
				}
			}
		}
	}
}
