using Magician.WebResourceLiveSync.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Helpers
{
    public static class Encoder
    {
        /// <summary>
        /// Base64 decode file contents, then write to disk and re-read to ensure
        /// comparisons are as accurate as possible. We seem to have some encoding issues
        /// without the additioanl write to disk read steps.
        /// </summary>
        /// <param name="base64text"></param>
        /// <returns>file contents</returns>
        public static string DecodeBas64(string base64text)
        {
            var tempFile = Path.GetTempFileName();

            var bytes = System.Convert.FromBase64String(base64text);

            File.WriteAllBytes(tempFile, bytes);

            var text = File.ReadAllText(tempFile);

            File.Delete(tempFile);

            return text;
        }

        public static string EncodeBase64(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            return System.Convert.ToBase64String(bytes);
        }

        public static string EncodeBase64(FileItem file)
        {
            var bytes = File.ReadAllBytes(file.FullName.OriginalString);

            return System.Convert.ToBase64String(bytes);
        }
    }
}
