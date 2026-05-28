using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace project.utils.Catalogues
{
    public class catalogueQueryDto
    {
        public long? id { get; set; }
        public string? name { get; set; }
        public long? catalogueParentId { get; set; }

    }
}