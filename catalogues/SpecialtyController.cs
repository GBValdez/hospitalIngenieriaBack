using AutoMapper;
using fletesProyect.catalogues;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace back.catalogues
{
    [ApiController]
    [Route("specialty")]
    public class SpecialtyController : cataloguesController<Specialty>
    {
        public SpecialtyController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
