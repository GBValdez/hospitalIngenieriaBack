using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace fletesProyect.catalogues
{
    [ApiController]
    [Route("sex")]
    public class sexController:cataloguesController<Sex>
    {
        public sexController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
