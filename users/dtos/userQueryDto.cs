
namespace project.users.dto
{
    public class userQueryDto
    {
        public string? UserNameCont { get; set; }
        public string? EmailCont { get; set; }
        public List<string>? roles { get; set; }

        public Boolean? isActive { get; set; }

    }
}