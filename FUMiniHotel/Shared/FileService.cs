namespace FUMiniHotel.Shared
{
    public interface IFileService
    {
        void DeleteFile(string fileName, string directory);
        Task<string> SaveFile(IFormFile file, string directory, string[] allowedExtension);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<string> SaveFile(IFormFile file, string directory, string[] allowedExtension)
        {
            // wwwpath
            var wwwPath = _webHostEnvironment.WebRootPath;
            var path = Path.Combine(wwwPath, directory); // wwwroot/images
            //if it not exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            //check the extension is allowd or not
            var extension = Path.GetExtension(file.FileName);
            if (!allowedExtension.Contains(extension))
            {
                throw new InvalidOperationException($"Only {string.Join
                    (",", allowedExtension)} extensions are allowed");
            }
            var newFileName = $"{Guid.NewGuid()}{extension}"; //asdfasd-sasdasdfa-hoang.jpeg
            //create fullpath = path+newfileName
            var fullPath = Path.Combine(path, newFileName);
            // create a file stream
            using var fileStream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            return newFileName;
        }
        public void DeleteFile(string fileName, string directory)
        {
            var fullPath = Path.Combine
                    (_webHostEnvironment.WebRootPath, directory, fileName);
            if (!Path.Exists(fullPath))
            {
                throw new FileNotFoundException($"File {fileName} does not exist.");
            }
            File.Delete(fullPath);
        }
    }
}
