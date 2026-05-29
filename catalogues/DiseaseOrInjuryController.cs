using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace fletesProyect.catalogues
{
    [ApiController]
    [Route("diseaseorinjuries")]
    public class DiseaseOrInjuryController : cataloguesController<DiseaseOrInjury>
    {
        public DiseaseOrInjuryController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
