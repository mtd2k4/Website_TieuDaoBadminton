using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace tieudaobadminton.ViewModels
{
    public class AdminNewsFormVM
    {
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Title { get; set; } = "";

        public string? Summary { get; set; }
        public string? Content { get; set; }

        public string? ThumbnailUrl { get; set; } // nhập URL nếu không upload
        public IFormFile? ThumbnailFile { get; set; } // upload ảnh

        public bool IsPublished { get; set; } = true;
    }
}
