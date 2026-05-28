using Microsoft.AspNetCore.Identity;
using project.users;
using project.utils.interfaces;

namespace project.roles
{
    public class rolEntity : IdentityRole, ICommonModel<string>
    {
        public string? userUpdateId { get; set; }

        public DateTime? deleteAt { get; set; }
        public userEntity? userUpdate { get; set; }
        public DateTime? createAt { get; set; }
        public DateTime? updateAt { get; set; }

    }
}