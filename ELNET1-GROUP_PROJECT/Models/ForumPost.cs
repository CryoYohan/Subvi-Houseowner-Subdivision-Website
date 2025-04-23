using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ForumPost
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string? Hashtag { get; set; }
        public string Content { get; set; }
        public DateTime DatePosted { get; set; }
        public int UserId { get; set; }
        public string? Profile { get; set; }  
        public string Role { get; set; }  
        public string Firstname { get; set; }  
        public string Lastname { get; set; }  
        public string FullName { get; set; }  
        public int Likes { get; set; }  
        public int RepliesCount { get; set; }  

        // Indicates if the current user has liked this post
        [NotMapped]
        public bool IsLiked { get; set; }

        public List<Replies> Replies { get; set; }

        public string LikeDisplay => FormatCount(Likes);
        public string RepliesDisplay => FormatCount(RepliesCount);

        private string FormatCount(int count)
        {
            if (count >= 1000)
                return (count / 1000.0).ToString("0.#") + "K";
            return count.ToString();
        }
    }
}
