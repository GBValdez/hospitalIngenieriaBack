
namespace project.utils.dto
{
    public class emailSendDto
    {
        public string email { get; set; }
        public string subject { get; set; }
        public string message { get; set; }
        public List<emailAttachmentDto> attachments { get; set; } = new List<emailAttachmentDto>();

    }

    public class emailAttachmentDto
    {
        public string fileName { get; set; }
        public string contentType { get; set; }
        public byte[] content { get; set; }
    }
}
