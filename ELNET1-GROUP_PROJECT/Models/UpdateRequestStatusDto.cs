namespace ELNET1_GROUP_PROJECT.Models
{
    public class UpdateRequestStatusDto
    {
        public int RequestId { get; set; }
        public string Status { get; set; }
        public string RejectedReason { get; set; }
    }
}
