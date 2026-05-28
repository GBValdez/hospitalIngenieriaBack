using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;
using project.utils.interfaces;

namespace project.utils
{
    public class controllerCommons<TEntity, TDtoCreation, TDto, TQuery, TQueryCreation, idClass> : ControllerBase
    where TEntity : class
    where TDto : class
    where TDtoCreation : class
    where TQuery : class
    {
        protected readonly DBProyContext context;
        protected readonly IMapper mapper;
        // protected readonly ILogger<AuthorsController> logger;
        protected virtual Boolean showDeleted { get; set; } = false;
        public controllerCommons(DBProyContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
            // this.logger=logger;
        }

        [HttpGet()]
        public virtual async Task<ActionResult<resPag<TDto>>> get([FromQuery] pagQueryDto infoQuery, [FromQuery] TQuery queryParams)
        {
            IQueryable<TEntity> query = context.Set<TEntity>();
            if (!showDeleted)
                query = query.Where(db => ((ICommonModel<idClass>)db).deleteAt == null);

            query = await this.modifyGet(query, queryParams);

            int total = await query.CountAsync();

            if (total == 0)
            {

                return new resPag<TDto>
                {
                    items = new List<TDto>(),
                    total = 0,
                    index = 0,
                    totalPages = 0
                };
            }

            int totalPages = (int)Math.Ceiling((double)total / infoQuery.pageSize);

            if (infoQuery.pageNumber > totalPages && !infoQuery.all.Value)
                return BadRequest(new errorMessageDto("El indice de la pagina es mayor que el numero de paginas total"));

            if (infoQuery.pageNumber < 0 && !infoQuery.all.Value)
                return BadRequest(new errorMessageDto("El indice de la pagina no puede ser menor que 0"));

            if (infoQuery.all == false)
                query = query
                .Skip((infoQuery.pageNumber - 1) * infoQuery.pageSize)
                .Take(infoQuery.pageSize);

            List<TEntity> listDb = await
                query
                .ToListAsync();
            modifyGetResult(listDb);
            List<TDto> listDto = mapper.Map<List<TDto>>(listDb);
            return new resPag<TDto>
            {
                items = listDto,
                total = total,
                index = infoQuery.pageNumber,
                totalPages = totalPages
            };
        }
        protected async virtual Task<IQueryable<TEntity>> modifyGet(IQueryable<TEntity> query, TQuery queryParams)
        {
            return query;
        }
        protected virtual void modifyGetResult(List<TEntity> list) { }

        [HttpPost]
        public virtual async Task<ActionResult<TDto>> post(TDtoCreation newRegister, [FromQuery] TQueryCreation queryParams)
        {
            TEntity newRegisterEntity = this.mapper.Map<TEntity>(newRegister);
            errorMessageDto error = await this.validPost(newRegisterEntity, newRegister, queryParams);
            if (error != null)
                return BadRequest(error);
            context.Add(newRegisterEntity);
            await context.SaveChangesAsync();
            await this.finallyPost(newRegisterEntity, newRegister, queryParams);
            return this.mapper.Map<TDto>(newRegisterEntity);
        }

        protected async virtual Task<errorMessageDto> validPost(TEntity entity, TDtoCreation newRegister, TQueryCreation queryParams)
        {
            return null;
        }
        protected async virtual Task finallyPost(TEntity entity, TDtoCreation dtoCreation, TQueryCreation queryParams)
        {
            return;
        }

        [HttpDelete("{id}")]

        public virtual async Task<ActionResult> delete(idClass id)
        {
            TEntity exits = await context.Set<TEntity>()
                .FirstOrDefaultAsync(Db => ((ICommonModel<idClass>)Db).Id.Equals(id) && ((ICommonModel<idClass>)Db).deleteAt == null);
            if (exits == null)
            {
                return NotFound();
            }
            errorMessageDto error = await this.validDelete(exits);
            if (error != null)
                return BadRequest(error);
            ((ICommonModel<idClass>)exits).deleteAt = DateTime.Now.ToUniversalTime();
            await context.SaveChangesAsync();
            return Ok();
        }

        protected virtual async Task<errorMessageDto> validDelete(TEntity entity)
        {
            return null;
        }

        [HttpPut("{id}")]

        public virtual async Task<ActionResult> put(TDtoCreation entityCurrent, [FromRoute] idClass id, [FromQuery] TQueryCreation queryCreation)
        {

            TEntity exits = await context.Set<TEntity>().FirstOrDefaultAsync(db => ((ICommonModel<idClass>)db).Id.Equals(id) && ((ICommonModel<idClass>)db).deleteAt == null);
            if (exits == null)
                return NotFound();
            errorMessageDto error = await this.validPut(entityCurrent, exits, queryCreation);
            if (error != null)
                return BadRequest(error);
            await modifyPut(exits, entityCurrent, queryCreation);
            exits = mapper.Map(entityCurrent, exits);
            await context.SaveChangesAsync();
            await finallyPut(exits, entityCurrent, queryCreation);
            return NoContent();
        }

        protected virtual async Task<errorMessageDto> validPut(TDtoCreation dtoNew, TEntity entity, TQueryCreation queryParams)
        {
            return null;
        }
        protected virtual async Task modifyPut(TEntity entity, TDtoCreation dtoNew, TQueryCreation queryParams)
        {
            return;
        }
        protected virtual async Task finallyPut(TEntity entity, TDtoCreation dtoNew, TQueryCreation queryParams)
        {
            return;
        }
    }
}