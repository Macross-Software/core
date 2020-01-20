using System;
using System.IO;

namespace Macross.Logging.Files
{
	internal class LogFile
	{
		public DateTime DateUtc { get; }

		public string FileName { get; }

		public int Index { get; }

		public string FinalFileName { get; }

		public string FinalFullPath { get; }

		public Stream Stream { get; }

		public bool Toxic { get; set; }

		public LogFile(DateTime dateUtc, string fileName, int index, string finalFileName, string finalFullPath, Stream stream)
		{
			DateUtc = dateUtc;
			FileName = fileName;
			Index = index;
			FinalFileName = finalFileName;
			FinalFullPath = finalFullPath;
			Stream = stream;
		}
	}
}
