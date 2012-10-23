using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsAzureAdmin.Shared.Enums
{
	public enum FileType
	{
		Unknown,
		MP4,
		WMV,
		XML
	}

	public static class FileTypeHelper
	{
		public static string GetFileTypeExtension(FileType fileType)
		{
			switch (fileType)
			{
				case FileType.MP4:
					return ".mp4";
				case FileType.WMV:
					return "wmv";
				case FileType.XML:
					return "xml";
				case FileType.Unknown:
					return null;
				default:
					throw new ApplicationException("Unexpected FileType enum value: " + fileType.ToString());
			}
		}
		public static FileType GetFileType(string fileName)
		{
			var fileExtension = Path.GetExtension(fileName);
			if (string.IsNullOrEmpty(fileExtension))
				return FileType.Unknown;
			switch (fileExtension.ToLower())
			{
				case ".mp4":
					return FileType.MP4;
				case ".wmv":
					return FileType.WMV;
				case ".xml":
					return FileType.XML;
				default:
					throw new ApplicationException("Unexpected file extension: " + fileExtension);
			}
		}
	}
}
