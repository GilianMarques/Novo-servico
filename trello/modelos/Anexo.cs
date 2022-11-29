using System;
using System.Collections.Generic;

namespace trello.modelos
{

    public partial class Anexo
    {
        public string? Id { get; set; }
        public long? Bytes { get; set; }
        public DateTimeOffset? Date { get; set; }
        public string EdgeColor { get; set; }
        public string? IdMember { get; set; }
        public bool? IsUpload { get; set; }
        public string MimeType { get; set; }
        public string? Name { get; set; }
        public List<Preview?>? Previews { get; set; }
        public Uri? Url { get; set; }
        public long? Pos { get; set; }
        public string? FileName { get; set; }
    }

    public partial class Preview
    {
        public string? PreviewId { get; set; }
        public string? Id { get; set; }
        public bool? Scaled { get; set; }
        public Uri? Url { get; set; }
        public long? Bytes { get; set; }
        public long?  Height { get; set; }
        public long? Width { get; set; }
    }
}
