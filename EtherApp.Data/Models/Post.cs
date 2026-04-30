using EtherApp.Data.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace EtherApp.Data.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public int NrOfReports { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool IsDeleted { get; set; }
        
        // Moderation fields
        public bool RequiresModeration { get; set; }
        public string? ModerationReason { get; set; }
        public bool IsHidden { get; set; } // True if post isn't shown to users pending moderation
        public DateTime? ModeratedAt { get; set; }
        public int? ModeratorId { get; set; }

        //foreign key
        public int UserId { get; set; }

        //Navigation properties
        public User User { get; set; }
        public ICollection<Like> Like { get; set; } = new List<Like>();
        public ICollection<Comment> Comment { get; set; } = new List<Comment>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<PostInterest> Interests { get; set; } = new List<PostInterest>();

    }

}
