using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using Encoder = Magician.WebResourceLiveSync.Helpers.Encoder;

namespace Magician.WebResourceLiveSync.Model
{
    public static class FileItemExtensions
    {
        public static FileItem ConvertToFile(this FileInfo file, Uri localDirectory)
        {
            var fullName = new Uri(file.FullName);

            return new FileItem
            {
                Name = file.Name,
                FullName = fullName,
                RelativePath = localDirectory.MakeRelativeUri(fullName),
                LastWriteTime = file.LastWriteTimeUtc
            };
        }

        public static Entity ConvertToEntity(this FileItem file, string customizationPrefix)
        {
            var entity = new Entity("webresource");

            entity["displayname"] = entity["name"] = customizationPrefix + "/" + file.RelativePath.OriginalString;
            
            if (file.ResourceId != null)
            {
                entity["webresourceid"] = entity.Id = file.ResourceId.Value;
            }

            entity["content"] = Encoder.EncodeBase64(file);

            return entity;
        }
    }
}
