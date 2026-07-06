namespace FileService.Entities
{
	public class AudioFile
	{
		public Guid Id { get; private set; } = Guid.NewGuid();
		public string FileName { get; private set; }
		public Uri Url { get; private set; }
		public long FileSize { get; private set; }
		public string FileSHA256Hash { get; private set; }
		public DateTime CreateDatetime { get; private set; }

		public static AudioFile Create(Guid id,long fileSize,string fileName,string hash,Uri url)
		{
			AudioFile audioFile = new AudioFile()
			{
				Id = id,
				CreateDatetime = DateTime.UtcNow,
				FileSize = fileSize,
				FileName = fileName,
				FileSHA256Hash = hash,
				Url = url
			};
			return audioFile;
		}
	}
}
