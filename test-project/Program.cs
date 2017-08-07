
namespace test_project
{
	class Program
	{
		static void Main(string[] args)
		{
			Archivator archivator = new Archivator();
			archivator.Run(@"zip-files\mock-zip.zip").Wait();
		}
	}
}
