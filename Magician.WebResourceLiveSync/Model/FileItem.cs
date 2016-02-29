using System;
using System.Text.RegularExpressions;

namespace Magician.WebResourceLiveSync.Model
{
    public class FileItem : DirectoryItem
    {
        public Guid? ResourceId { get; set; }

        public Uri RelativePath { get; set; }

        public DateTime LastWriteTime { get; set; }

        public int Type
        {
            get
            {
                switch (Extension)
                {
                    case ".css":
                        return (int)WebResourceType.Css;
                    case ".xml":
                        return (int)WebResourceType.Xml;
                    case ".gif":
                        return (int)WebResourceType.Gif;
                    case ".htm":
                        return (int)WebResourceType.Html;
                    case ".html":
                        return (int)WebResourceType.Html;
                    case ".ico":
                        return (int)WebResourceType.Png;
                    case ".jpg":
                        return (int)WebResourceType.Jpg;
                    case ".png":
                        return (int)WebResourceType.Png;
                    case ".js":
                        return (int)WebResourceType.JavaScript;
                    case ".xap":
                        return (int)WebResourceType.Silverlight;
                    case ".xsl":
                        return (int)WebResourceType.Stylesheet_XSL;
                    default:
                        return -1;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                if (Items == null || Items.Count > 0 || RelativePath == null || Type == -1)
                {
                    return false;
                }

                var nameRegex = new Regex("[^a-z0-9A-Z_\\./]|[/]{2,}",
                    (RegexOptions.Compiled | RegexOptions.CultureInvariant));

                // Test valid characters
                if (nameRegex.IsMatch(RelativePath.ToString()))
                {
                    return false;
                }

                // Test length
                if (RelativePath.ToString().Length > 100)
                {
                    return false;
                }

                return true;
            }
        }

        public FileItem()
        {
            IsFile = true;
        }
    }
}
