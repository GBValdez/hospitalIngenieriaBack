using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using project.utils.dto;

namespace project.utils.dto
{
    public class errClass<T>
    {
        public errorMessageDto? error { get; set; }
        public T? data { get; set; }
    }
}