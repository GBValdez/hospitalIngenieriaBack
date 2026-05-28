using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace fletesProyect.catalogues
{
    [ApiController]
    [Route("nationality")]
    public class NationalityController:cataloguesController<Nationality>
    {
        public NationalityController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}