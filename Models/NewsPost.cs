using System;

namespace tieudaobadminton.Models
{
    public class NewsPost
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";          // URL thân thiện
        public string? Summary { get; set; }            // mô tả ngắn
        public string? Content { get; set; }            // nội dung HTML/text
        public string? ThumbnailUrl { get; set; }       // ảnh đại diện

        public bool IsPublished { get; set; } = true;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
