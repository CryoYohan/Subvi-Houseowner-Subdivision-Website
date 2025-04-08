using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class FeedbackRequest
    {
        public string FeedbackType { get; set; }
        public string Description { get; set; }
        public int? Rating { get; set; }
        public string? ComplaintStatus { get; set; }
    }

}
