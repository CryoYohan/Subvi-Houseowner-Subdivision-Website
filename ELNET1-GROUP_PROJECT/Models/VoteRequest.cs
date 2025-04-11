using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class VoteRequest
    {
        public int PollId { get; set; }
        public int ChoiceId { get; set; }
    }
}