using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace fletesProyect.catalogues
{
    [ApiController]
    [Route("examtypes")]
    public class ExamTypeController : cataloguesController<ExamType>
    {
        public ExamTypeController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
