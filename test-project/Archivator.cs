using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace test_project
{
	class Archivator
	{
		//Extracting files via SharpZipLib
		//I use SharpZipLib because SharpZipLib more faster than System.IO.Compression.ZipFile
		private void ExtractZipFiles(string zipPath, string outFolder, string password = null)
		{
			ZipFile zipFile = null;
			try
			{
				FileStream fs = File.OpenRead(zipPath);
				zipFile = new ZipFile(fs);

				if (!String.IsNullOrEmpty(password)) //If archive is encrypted
					zipFile.Password = password;

				foreach (ZipEntry zipEntry in zipFile)
				{
					if (!zipEntry.IsFile || Path.GetExtension(zipEntry.Name) != ".csv")
						continue;

					var entryFileName = zipEntry.Name;

					var buffer = new byte[4096];
					Stream zipStream = zipFile.GetInputStream(zipEntry);

					string fullZipToPath = Path.Combine(outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName(fullZipToPath);
					if (directoryName.Length > 0)
						Directory.CreateDirectory(directoryName);

					using (FileStream streamWriter = File.Create(fullZipToPath))
					{
						StreamUtils.Copy(zipStream, streamWriter, buffer);
						Console.WriteLine($"File extracted '{Path.GetFileName(zipEntry.Name)}'");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"***Exception***\n '{ex.Message}'.");
			}
			finally
			{
				if (zipFile != null)
				{
					zipFile.IsStreamOwner = true; // Makes close also shut the underlying stream
					zipFile.Close();
				}
			}
		}

		//Get temporary directory for current user
		private string GetTemporaryDirectory()
		{
			var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			DirectoryInfo tempDirectory = Directory.CreateDirectory(tempPath);
			tempDirectory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
			Console.WriteLine($"Directory created: '{tempPath}'");
			return tempPath;
		}

		//Method A
		public async Task Run(string zipPath, string zipPassword = null)
		{
			if (String.IsNullOrEmpty(zipPath)) 
				 throw new ArgumentNullException($"The argument '{nameof(zipPath)}' can't be null or empty");
			if (!File.Exists(zipPath))
				throw new FileNotFoundException($"The file '{nameof(zipPath)}' was not be found");

			var tempPath = GetTemporaryDirectory();

			try
			{
				await Task.Factory
					.StartNew(() => ExtractZipFiles(zipPath, tempPath, zipPassword))
					.ContinueWith((t) =>
					{
						if (!Directory.Exists(tempPath))
							throw new DirectoryNotFoundException($"The directory '{nameof(tempPath)}' was not be found");

						ProcessDirectory(tempPath);

					}, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.PreferFairness)
					.ContinueWith((t) =>
					{
						Directory.Delete(tempPath, true);
						Console.WriteLine($"Directory deleted '{tempPath}'");
					}, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.PreferFairness);
			}
			catch (Exception ex)
			{
				//Something went wrong
				Console.WriteLine("Something went wrong:");
				Console.WriteLine(ex.Message);
			}
		}

		//If necessary, makes a recursive pass in the subdirectories
		private void ProcessDirectory(string targetDirectory)
		{
			var fileEntries = Directory.GetFiles(targetDirectory);
			foreach (var fileName in fileEntries)
				ProcessFile(fileName);

			var subdirectoryEntries = Directory.GetDirectories(targetDirectory);
			foreach (var subdirectory in subdirectoryEntries)
				ProcessDirectory(subdirectory);
		}

		//Method B
		private static void ProcessFile(string path) => Console.WriteLine($"Processed file '{path}'");
		

	}
}
