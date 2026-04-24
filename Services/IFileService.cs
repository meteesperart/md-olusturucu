using MDOlusturucu.Models;

namespace MDOlusturucu.Services;

public interface IFileService
{
    IEnumerable<FileModel> GetFiles(string directory);
    FileModel Read(string path);
    void Write(FileModel file);
    FileModel CopyToAppDirectory(string sourcePath, string appDirectory);
    void Rename(FileModel file, string newName);
}
