using System;
using System.Collections.Generic;
using project.utils;

namespace project.utils.catalogue;

public partial class Catalogue : CommonsModel<long>
{
    public string name { get; set; } = null!;
    public string? description { get; set; }
}
