using SharpCompress.Archives;
using SharpCompress.Common;

namespace EasyUpdateFromGithub
{
	static class ToolClass
	{
		/// <summary>
		/// 获取指定url的返回值
		/// </summary>
		internal static async Task<string> GetUrlResponseAsync(string url)
		{
			var response = await new HttpClient().GetAsync(url);
			return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// 下载文件
		/// </summary>
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
					//Console.WriteLine(entry.Key);
					entry.WriteToDirectory(dirPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
				}
			}
		}
	}
}
