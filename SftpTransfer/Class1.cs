using Renci.SshNet;
using System;
using System.IO;
using System.Linq;

namespace SftpTransfer
{
    public class SftpTransfer()
    {
        public static void Start(string folderName, string sourcePath, string sftpPath ,string host, string username, string password, string unzip)
        {
            string fileName = sourcePath.Split('\\').Last();
            string targetDir = $"{sftpPath}/{folderName}";
            string sourceDir = sourcePath.Replace($"\\{fileName}", "");
            Console.WriteLine("Prepare to upload file to SFTP");
            UploadFileToSftp(fileName, targetDir, sourceDir, host, username, password);

            string extractDir = $"{targetDir}";
            if (unzip == "true")
            {
                UnzipFileOnSftp(fileName, targetDir, extractDir, host, username, password);
            }
        }

        private static void UploadFileToSftp(string fileName, string targetDir, string sourceDir, string host, string username, string password)
        {
            string targetPath = $"{targetDir}/{fileName}";
            string sourcePath = $"{sourceDir}\\{fileName}";

            using SftpClient client = new(host, username, password);

            client.Connect();

            Console.WriteLine("SFTP client is connected");

            if (client.Exists(targetDir))
            {
                Console.WriteLine($"Target SFTP path \"{targetDir}\" already exists !");
                DeleteDirectory(targetDir, host, username, password);
            }
            client.CreateDirectory(targetDir);
            Console.WriteLine($"Target SFTP path \"{targetDir}\" is created.");

            Console.WriteLine($"Upload file \"{sourcePath}\"");
            using FileStream file = System.IO.File.OpenRead($"{sourcePath}");

            client.UploadFile(file, targetPath);
            Console.WriteLine($"The upload has been finished.");

            client.Disconnect();
            Console.WriteLine($"SFTP client is disconnected.");
        }

        private static void DeleteDirectory(string targetDir, string host, string username, string password)
        {
            using SshClient ssh = new(host, username, password);

            ssh.Connect();

            string command = $"rm -rf {targetDir}";
            SshCommand result = ssh.RunCommand(command);
            Console.WriteLine($"Target SFTP path \"{targetDir}\" has been deleted.");

            ssh.Disconnect();
        }

        private static void UnzipFileOnSftp(string fileName, string targetDir, string extractDir, string host, string username, string password)
        {
            string targetFile = $"{targetDir}/{fileName}";

            using SshClient ssh = new(host, username, password);

            ssh.Connect();
            Console.WriteLine("SSH is connected");

            string fileExt = fileName.Split('.').Last();

            string unzipCommand = "";
            if (fileExt == "zip")
            {
                unzipCommand = $"unzip {targetFile} -d {extractDir}";
            }
            else
            {
                unzipCommand = $"tar -xzvf {targetFile} -C {extractDir}";
            }
            Console.WriteLine($"Run unzip command: {unzipCommand}");
            SshCommand unzipResult = ssh.RunCommand(unzipCommand);

            if (!string.IsNullOrEmpty(unzipResult.Error))
            {
                Console.WriteLine($"Error: {unzipResult.Error}");
            }
            else
            {
                Console.WriteLine("File unzipped successfully.");
            }

            string removeZipCommand = $"rm -rf {targetFile}";
            Console.WriteLine($"Run remove zip command: {removeZipCommand}");
            SshCommand removeResult = ssh.RunCommand(removeZipCommand);

            if (!string.IsNullOrEmpty(removeResult.Error))
            {
                Console.WriteLine($"Error: {removeResult.Error}");
            }
            else
            {
                Console.WriteLine("Zip has been removed.");
            }

            ssh.Disconnect();
        }
    }
}
