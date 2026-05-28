using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using project.utils;

namespace project.ModelsDto;

public partial class binnacleBody : CommonsModel<long>
{
    public string field { get; set; } = null!;

    public string previousValue { get; set; } = null!;

    public string newValue { get; set; } = null!;

    public long binnacleHeaderId { get; set; }
    public binnacleHeader binnacleHeader { get; set; } = null!;
}
