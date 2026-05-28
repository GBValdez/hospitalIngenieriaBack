using Microsoft.AspNetCore.Identity;
using project.utils.interfaces;

namespace project.users
{
    public class userEntity : IdentityUser, ICommonModel<string>
    {
        public string? userUpdateId { get; set; }
        public DateTime? deleteAt { get; set; }
        public userEntity? userUpdate { get; set; }
        public DateTime? createAt { get; set; }
        public DateTime? updateAt { get; set; }
    }
}