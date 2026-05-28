using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using project.users;
using project.utils;

namespace project.ModelsDto;

public partial class binnacleHeader : CommonsModel<long>
{
    public string table { get; set; } = null!;

    public string idRegister { get; set; } = null!;

    public string transactionType { get; set; } = null!;

    public string userId { get; set; } = null!;
    public ICollection<binnacleBody> binnacleBody { get; set; } = new List<binnacleBody>();
    [ForeignKey("userId")]
    public userEntity User { get; set; } = null!;
}
