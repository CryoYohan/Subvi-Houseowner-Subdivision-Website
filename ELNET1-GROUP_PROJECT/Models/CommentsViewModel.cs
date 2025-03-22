using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class CommentsViewModel
    {
        public dynamic Post { get; set; } // Replace dynamic with the actual type of the Post if needed
        public List<ReplyViewModel> Replies { get; set; }
    }
}
