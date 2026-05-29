using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace fletesProyect.Medicine
{
    [ApiController]
    [Route("medicines")]
    public class MedicineController : cataloguesController<Medicine>
    {
        public MedicineController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
