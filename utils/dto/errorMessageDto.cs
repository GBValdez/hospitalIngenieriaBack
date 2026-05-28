using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace project.utils.dto
{
    public class errorMessageDto
    {
        public errorMessageDto(string message)
        {
            this.message = message;
        }
        public string message { get; set; }
    }
}