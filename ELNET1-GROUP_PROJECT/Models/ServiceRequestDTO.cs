using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ServiceRequestDTO
    {
        public string ServiceName { get; set; }
        public string Notes { get; set; }
    }
}
