using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using project.users;
using project.utils;

namespace project.users.Models;

public partial class Client : CommonsModel<long>
{
    public string name { get; set; } = null!;
    public DateOnly birthdate { get; set; }
    public string email { get; set; } = null!;
    public string tel { get; set; } = null!;
    public string address { get; set; } = null!;
    public string userId { get; set; } = null!;
    public userEntity user { get; set; } = null!;
}
