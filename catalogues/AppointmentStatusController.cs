using AutoMapper;
using fletesProyect.catalogues;
using Microsoft.AspNetCore.Mvc;
using project.Models;
using project.utils.catalogues;

namespace back.catalogues
{
    [ApiController]
    [Route("appointment-status")]
    public class AppointmentStatusController : cataloguesController<AppointmentStatus>
    {
        public AppointmentStatusController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
