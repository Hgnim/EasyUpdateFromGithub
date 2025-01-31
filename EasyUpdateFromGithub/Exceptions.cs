using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyUpdateFromGithub {
	/// <summary>
	/// 特殊异常
	/// </summary>
	public struct Exceptions {
		/// <summary>
		/// 文件下载失败异常
		/// </summary>
		public class FileDownloadFailed : Exception {
			/// <summary>
			/// </summary>
			public FileDownloadFailed():base("文件下载失败") {
			}
			/// <summary>
			/// </summary>
			public FileDownloadFailed(string message):base(message) {
			}
		}
	}
}
