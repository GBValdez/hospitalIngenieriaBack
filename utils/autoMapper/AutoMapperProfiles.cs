using AutoMapper;
using fletesProyect.Patient;
using project.roles;
using project.roles.dto;
using project.users;
using project.users.dto;
using project.users.Models;
using project.utils.catalogue;
using project.utils.catalogues.dto;
using project.utils.Catalogues.dto;

namespace project.utils.autoMapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<userEntity, userDto>()
            .ForMember(userDtoId => userDtoId.isActive, options => options.MapFrom(src => src.deleteAt == null));
            ;

            CreateMap<rolEntity, rolDto>();
            CreateMap<rolCreationDto, rolEntity>();
            CreateMap<Patient, clientDto>();
            CreateMap<clientCreationDto, Patient>();
            CreateMap<clientCreationDto, userCreationDto>();
            CreateMap<Catalogue, catalogueDto>();
            CreateMap<catalogueCreationDto, Catalogue>();

        }

    }
}