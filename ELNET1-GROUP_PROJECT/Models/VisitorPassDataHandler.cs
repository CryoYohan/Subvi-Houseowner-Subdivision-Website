using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class VisitorPassDataHandler
    {
        public int VisitorId { get; set; }
        public int UserId { get; set; }
        public string VisitorName { get; set; }
        public string Relationship { get; set; }
    }


}
