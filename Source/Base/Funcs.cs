using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SaaedBackup.Logic
{
    public static class Funcs
    {
        public static string RenameDuplicatedFile(string fullPath)
        {
            var dst = Path.GetDirectoryName(fullPath) + '\\';
            var filename = Path.GetFileNameWithoutExtension(fullPath);
            var ext = Path.GetExtension(fullPath);
            var targetWithoutExtention = Path.Combine(dst, filename);
            while (File.Exists(targetWithoutExtention + ext))
            {
                targetWithoutExtention = ProductNextName(targetWithoutExtention);
            }
            return targetWithoutExtention + ext;
        }
        public static string ProductNextName(string currentName)
        {
            bool hasNum;

            hasNum = currentName.Last() == ')';
            if (hasNum && !char.IsDigit(currentName[currentName.Length - 2]))
                hasNum = false;
            if (!hasNum) return currentName + "(2)";

            var currentNumStr = "";
            int i;
            for (i = currentName.Length - 2; i > 0 && char.IsDigit(currentName[i]); i--)
            {
                currentNumStr = currentName[i] + currentNumStr;
            }

            if (currentName[i] != '(') return currentName + "(2)";
            var currentNum = int.Parse(currentNumStr);
            var pureName = currentName.Substring(0, i);
            return $"{pureName}({++currentNum})";
        }

        public static string FileMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static string StreamMd5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        
        //public static string RenameDuplicatedDirectory(string fullPath)
        //{

        //    while (Directory.Exists(fullPath))
        //    {
        //        fullPath = ProductNextName(fullPath);
        //    }
        //    return fullPath;
        //}

        //to use in ftp

        /*public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
               
*/



    }
}
