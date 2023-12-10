using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DownloadImage.ViewModels
{

    public class PayloadInformationList: List<PayloadInformation>
    {
        public PayloadInformationList()
        { 
        }
    }
    public enum PayloadType
    {
        File,
        Folder,
        ZipFile,
    }

    public class PayloadInformation
    {
        [JsonPropertyName("payloadType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PayloadType PayloadType { get; set; } = PayloadType.File;

        [JsonPropertyName("fileURL")]
        public string FileURL { get; set; } = "";

        [JsonPropertyName("targetPath")]
        public string TargetPath { get; set; } = "";

        [JsonPropertyName("versionNumber")]
        public string VersionNumber { get; set; } = "";
    }
}
