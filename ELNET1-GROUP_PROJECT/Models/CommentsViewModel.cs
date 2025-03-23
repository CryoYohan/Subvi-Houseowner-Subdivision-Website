using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class CommentsViewModel
    {
        public dynamic Post { get; set; } 
        public List<ReplyViewModel> Replies { get; set; }
    }
}
