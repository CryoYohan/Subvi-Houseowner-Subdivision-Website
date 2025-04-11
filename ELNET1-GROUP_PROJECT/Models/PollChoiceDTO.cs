using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class PollChoiceDTO
    {
        public int ChoiceId { get; set; }
        public string Choice { get; set; }
        public bool IsVoted { get; set; }
    }

}